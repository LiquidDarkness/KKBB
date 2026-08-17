#if AUTOPLAY
using UnityEngine;
using UnityEngine.SceneManagement;

// Development aid: plays the game on its own, so a run does not have to be sat through by hand.
// Compiled in only when the AUTOPLAY scripting define symbol is set (Debug -> Autoplay compiled
// into the build), so a shipped build without that symbol contains none of this - not even the
// key check.
//
// It needs nothing wired in any scene or prefab: it makes its own DontDestroyOnLoad host at
// startup and finds everything it drives by itself. F9 turns it on and off at any time; it says
// what it is doing in the console and draws nothing on screen.
//
// In the Gameplay scene it:
//  - clicks through each story beat once it has been on screen for StoryReadTime,
//  - launches the ball once a level has settled,
//  - steers the paddle under the ball while the ball is on its way down,
//  - puts back every life lost, so a run can never end on the Game Over screen,
//  - goes back to the menu when the ending shows up.
//
// In the Menu scene it starts a scenario: the one currently selected if autoplay was just
// switched on, the next one down the ScenarioManager's list if it has just finished one. Score,
// story position and lives are cleared first, exactly as the New Game button clears them. It
// stops itself after the last scenario in the list (in a demo build, after the last one flagged
// availableInDemo - locked ones are skipped rather than started into a lock).
public class AutoPlayer : MonoBehaviour
{
    public const KeyCode ToggleKey = KeyCode.F9;

    private const string GameplaySceneName = "Gameplay";
    private const string MenuSceneName = "Menu";
    // Same index GameStarterHelper uses for the same purpose: the first enabled scene in the
    // build settings, which is where CommonInit lives.
    private const int InitSceneBuildIndex = 0;

    // How long a story beat stays on screen before it is clicked through.
    private const float StoryReadTime = 2f;
    // Breathing room after a level appears: LevelLoader takes spawnTime to reveal the whole
    // formation (3s on Levelmanager), and a ball already flying through that area can end up
    // with a block materialising on top of it. Waiting the board out costs a couple of seconds
    // per level and removes the whole class of oddity.
    private const float LaunchDelay = 3.5f;
    // ProgressToLevel fades to black first (2s at the LoadingScreen's current fadeoutSpeed of
    // 0.5) and only then hides the story text, so the request has to be given that long to take
    // effect before it may be issued again. This is the fallback for a request that got lost
    // entirely, not the normal path - normally the wait ends when the story text goes away.
    private const float ProgressRetryTimeout = 8f;
    // How long the finale is left on screen before heading back to the menu.
    private const float EndingDwellTime = 4f;
    // How long the menu is given to fade in and to have its CoreReferences injected before a
    // scenario is started from it.
    private const float MenuSettleTime = 2.5f;
    // How long the run may go without a single block being broken before it counts as stuck. A
    // long rally on a sparse board is the only innocent way to reach it, and the recovery costs
    // that rally and nothing else - no life, no progress.
    private const float StuckTimeout = 30f;
    // Clearance above the ball's resting height on the paddle within which the paddle is left
    // alone. By the time the ball is that close the shot is already decided.
    private const float ContactMargin = 0.75f;

    private static AutoPlayer instance;

    private bool isEnabled;
    private bool healthHooked;

    // Both survive scene changes on purpose. advanceScenarioInMenu is set when a scenario ends
    // and read on the menu visit that follows, which is what makes the chain move on instead of
    // replaying the same one; initSceneBounced makes the trip through the init scene one-shot.
    private bool advanceScenarioInMenu;
    private bool initSceneBounced;

    private string lastSceneName = string.Empty;
    private bool leavingGameplay;
    private bool scenarioStartRequested;

    private StoryManager storyManager;
    private PaddleMovement paddle;
    private BallMovement ball;
    private Rigidbody2D ballBody;
    private Camera gameCamera;
    // An asset, so unlike everything above it stays valid across scene loads - which is the point,
    // since the menu has no StoryManager to ask for it.
    private ScenarioManager scenarioManager;

    private float storyTimer;
    private float launchTimer;
    private float endingTimer;
    private float menuTimer;
    private bool progressRequested;
    private float progressWaitTimer;
    private float idleTimer;
    private float pausedTimer;
    private bool longPauseReported;
    private int recoveryCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject(nameof(AutoPlayer));
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AutoPlayer>();
    }

    private void OnDestroy()
    {
        SetEnabled(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            SetEnabled(!isEnabled);
        }

        if (!isEnabled)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != lastSceneName)
        {
            lastSceneName = sceneName;
            ForgetSceneState();
        }

        switch (sceneName)
        {
            case GameplaySceneName:
                UpdateGameplay();
                break;
            case MenuSceneName:
                UpdateMenu();
                break;
            default:
                break;
        }
    }

    private void UpdateGameplay()
    {
        if (leavingGameplay)
        {
            return;
        }

        AcquireSceneObjects();

        if (storyManager == null)
        {
            return;
        }

        // Checked before anything is asked of the scene rather than at the point of use: both the
        // story page and the return to the menu go through the loading screen, and finding out
        // it is missing halfway through either of them is an exception, not a diagnosis.
        if (!IsCoreReady(storyManager.coreReferences))
        {
            HandleMissingCoreReferences();
            return;
        }

        // The ending's placeholder level has no blocks and is never meant to be loaded, so there
        // is nothing left to play here - the run moves on from the menu instead.
        if (storyManager.ending != null && storyManager.ending.activeInHierarchy)
        {
            HandleEnding();
            return;
        }

        if (StoryManager.isStoryActive)
        {
            idleTimer = 0f;
            HandleStoryBeat();
            return;
        }

        progressRequested = false;
        progressWaitTimer = 0f;
        storyTimer = 0f;

        if (PauseManager.IsPaused)
        {
            ReportLongPause();
            return;
        }

        pausedTimer = 0f;
        longPauseReported = false;

        HandleLaunch();
        WatchForStuckRun();
    }

    // A pause is not treated as being stuck - it may well be the one watching the run who opened
    // the shop - so nothing is forced here. It is worth one line in the console all the same: a
    // pause lock nobody gave back is otherwise indistinguishable from a frozen game.
    private void ReportLongPause()
    {
        pausedTimer += Time.unscaledDeltaTime;

        if (longPauseReported || pausedTimer < StuckTimeout)
        {
            return;
        }

        longPauseReported = true;
        Debug.LogWarning($"[{nameof(AutoPlayer)}] paused for {StuckTimeout:0}s and autoplay is waiting it out. If nothing is open on screen, a pause lock was taken and never given back.");
    }

    // The whole run hangs on blocks being broken, so that is what is timed. Every way the ball can
    // stop mattering ends up here: wedged in a corner, stopped dead (AdjustVelocity multiplies a
    // zero vector by minVelocity and gets zero back, so a motionless ball stays motionless), or
    // sitting on the paddle unable to launch. The state is dumped before the fix, so the next hang
    // is a log line instead of a screenshot to squint at, and the fix is the one thing that
    // resolves all three: put the ball back on the paddle and serve again.
    private void WatchForStuckRun()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer < StuckTimeout)
        {
            return;
        }

        idleTimer = 0f;
        recoveryCount++;

        Debug.LogWarning($"[{nameof(AutoPlayer)}] nothing broken for {StuckTimeout:0}s - re-serving (recovery #{recoveryCount}). {DescribeState()}");

        if (PlayerRig.instance != null)
        {
            PlayerRig.instance.LockToPaddle();
        }

        launchTimer = 0f;
    }

    private string DescribeState()
    {
        Level level = FindObjectOfType<Level>();
        string ballState = ball == null
            ? "ball: missing"
            : $"ball: pos {ball.transform.position}, speed {ball.currentVelocity:0.00}, launched {ball.hasBeenLaunched}, canLaunch {ball.canBeLaunched}";
        string paddleState = paddle == null ? "paddle: missing" : $"paddle x: {paddle.transform.position.x:0.00}";
        string blocks = level == null ? "blocks left: no Level" : $"blocks left: {level.breakableBlocks}";

        return $"{ballState}; {paddleState}; {blocks}; paused {PauseManager.IsPaused}; story {StoryManager.isStoryActive}";
    }

    private void HandleStoryBeat()
    {
        launchTimer = 0f;

        if (progressRequested)
        {
            // The fade is running; isStoryActive drops on its callback. Only step in if that
            // never happens, which would otherwise leave the run stuck on a story page forever.
            progressWaitTimer += Time.unscaledDeltaTime;
            if (progressWaitTimer < ProgressRetryTimeout)
            {
                return;
            }

            Debug.LogWarning($"[{nameof(AutoPlayer)}] the story page did not go away - asking again.");
            progressRequested = false;
            progressWaitTimer = 0f;
        }

        storyTimer += Time.unscaledDeltaTime;

        if (storyTimer < StoryReadTime)
        {
            return;
        }

        storyTimer = 0f;
        progressRequested = true;
        progressWaitTimer = 0f;
        storyManager.ProgressToLevel();
    }

    private void HandleLaunch()
    {
        if (ball == null || ball.hasBeenLaunched)
        {
            launchTimer = 0f;
            return;
        }

        launchTimer += Time.deltaTime;

        if (launchTimer < LaunchDelay || !ball.canBeLaunched || PlayerRig.instance == null)
        {
            return;
        }

        launchTimer = 0f;
        idleTimer = 0f;
        PlayerRig.instance.LaunchBall();
    }

    private void HandleEnding()
    {
        endingTimer += Time.unscaledDeltaTime;
        if (endingTimer < EndingDwellTime)
        {
            return;
        }

        leavingGameplay = true;
        advanceScenarioInMenu = true;
        Debug.Log($"[{nameof(AutoPlayer)}] ending reached - back to the menu for the next scenario.");

        if (!TryLoadScene(storyManager.coreReferences, MenuSceneName))
        {
            leavingGameplay = false;
        }
    }

    private void UpdateMenu()
    {
        if (scenarioStartRequested)
        {
            return;
        }

        menuTimer += Time.unscaledDeltaTime;
        if (menuTimer < MenuSettleTime)
        {
            return;
        }

        scenarioStartRequested = true;
        StartScenario(advanceScenarioInMenu);
        advanceScenarioInMenu = false;
    }

    private void StartScenario(bool advance)
    {
        GameController controller = FindGameController();
        if (controller == null)
        {
            Debug.LogWarning($"[{nameof(AutoPlayer)}] no GameController in the menu - nothing to read the new-game values off, stopping.");
            SetEnabled(false);
            return;
        }

        if (!IsCoreReady(controller.coreReferences))
        {
            HandleMissingCoreReferences();
            return;
        }

        ScenarioManager manager = ResolveScenarioManager();
        if (manager == null)
        {
            Debug.LogWarning($"[{nameof(AutoPlayer)}] no ScenarioManager asset in memory - cannot pick a scenario, stopping.");
            SetEnabled(false);
            return;
        }

        int index = Mathf.Max(manager.chosenScenario.IntValue, 0);
        if (advance)
        {
            index++;
        }

        while (index < manager.scenarios.Count && !IsPlayable(manager.scenarios[index]))
        {
            Debug.Log($"[{nameof(AutoPlayer)}] skipping {manager.scenarios[index].name} - locked in this build.");
            index++;
        }

        if (index >= manager.scenarios.Count)
        {
            Debug.Log($"[{nameof(AutoPlayer)}] every scenario has been played - autoplay off.");
            SetEnabled(false);
            return;
        }

        StoryContainer scenario = manager.scenarios[index];
        Debug.Log($"[{nameof(AutoPlayer)}] starting {scenario.name} (scenario {index}) as a new game.");
        manager.SetSettings(scenario);
        ResetRunProgress(controller);
        TryLoadScene(controller.coreReferences, GameplaySceneName);
    }

    // GameController.NewGame is deliberately not used, even though this is exactly its job: the
    // persistent GameSession carries a second GameController whose sceneName is Menu (it is the
    // one behind the pause menu), so NewGame on the wrong one of the two reloads the menu over
    // itself. Which one FindObjectsOfType hands back first is not defined, so the run reset is
    // done here and the scene is named explicitly. The values themselves are the same assets on
    // both, and clearing them is what stops a new game inheriting the last run's score, story
    // position and lives.
    private static void ResetRunProgress(GameController controller)
    {
        ClearValue(controller.savedScore, nameof(controller.savedScore));
        ClearValue(controller.currentLevel, nameof(controller.currentLevel));
        ClearValue(controller.health, nameof(controller.health));
        SaveManager.Save();
    }

    private static void ClearValue(TypeDistinguisher value, string fieldName)
    {
        if (value == null)
        {
            Debug.LogWarning($"[{nameof(AutoPlayer)}] GameController.{fieldName} is not assigned - it will carry over from the previous run.");
            return;
        }

        // Health treats 0 as uninitialised and refills from the difficulty on the next Initialize.
        value.SetIntValue(0);
    }

    private static GameController FindGameController()
    {
        GameController[] controllers = FindObjectsOfType<GameController>();
        if (controllers.Length == 0)
        {
            return null;
        }

        foreach (GameController controller in controllers)
        {
            if (controller.currentLevel != null && controller.coreReferences != null)
            {
                return controller;
            }
        }

        return controllers[0];
    }

    private static bool IsPlayable(StoryContainer scenario)
    {
        if (scenario == null)
        {
            return false;
        }

#if DEMO_BUILD
        return scenario.availableInDemo;
#else
        return true;
#endif
    }

    private ScenarioManager ResolveScenarioManager()
    {
        if (scenarioManager != null)
        {
            return scenarioManager;
        }

        if (storyManager != null && storyManager.scenarioManager != null)
        {
            scenarioManager = storyManager.scenarioManager;
            return scenarioManager;
        }

        // Nothing in the menu holds a ScenarioManager in a field this can reach, but the asset is
        // loaded all the same - the scenario buttons reference it - so it can be picked out of
        // what Unity already has in memory. There is only ever one of them in this project.
        ScenarioManager[] loaded = Resources.FindObjectsOfTypeAll<ScenarioManager>();
        if (loaded.Length > 0)
        {
            scenarioManager = loaded[0];
        }

        return scenarioManager;
    }

    private static bool IsCoreReady(CoreReferences coreReferences)
    {
        return coreReferences != null && coreReferences.loadingScreen != null;
    }

    // CoreReferences.loadingScreen is filled in at runtime by the ReferenceProvider on the
    // LoadingScreen inside CommonInit, and CommonInit only sits in the init scenes. Press Play on
    // Menu or Gameplay directly and the field stays null for the whole session, which is a
    // NullReferenceException the moment anything asks for a scene change - the menu's own New
    // Game button included. The project's own answer to being started in the wrong scene is
    // GameStarterHelper's: go through the init scene first, which is what this does. A build
    // always starts there, so this can only ever fire in the editor.
    private void HandleMissingCoreReferences()
    {
        if (initSceneBounced)
        {
            Debug.LogWarning($"[{nameof(AutoPlayer)}] CoreReferences still has no LoadingScreen after the init scene - stopping.");
            SetEnabled(false);
            return;
        }

        initSceneBounced = true;
        Debug.LogWarning($"[{nameof(AutoPlayer)}] this session was not started from the init scene, so CoreReferences was never injected - going through scene {InitSceneBuildIndex} first. (Pressing Play on Menu or Gameplay directly breaks the New Game button the same way.)");
        SceneManager.LoadScene(InitSceneBuildIndex, LoadSceneMode.Single);
    }

    private bool TryLoadScene(CoreReferences coreReferences, string sceneName)
    {
        SceneLoader loader = ResolveSceneLoader(coreReferences);
        if (loader == null)
        {
            Debug.LogWarning($"[{nameof(AutoPlayer)}] no SceneLoader to reach {sceneName} with - stopping.");
            SetEnabled(false);
            return false;
        }

        loader.LoadScene(sceneName);
        return true;
    }

    // The route the menu buttons take. Whichever instance this is, LoadScene runs its transition
    // on the persistent LoadingScreen rather than on itself, so it does not matter that
    // CoreReferences points at the one living on a prefab.
    private static SceneLoader ResolveSceneLoader(CoreReferences coreReferences)
    {
        if (coreReferences != null && coreReferences.sceneLoader != null)
        {
            return coreReferences.sceneLoader;
        }

        GameSession session = FindObjectOfType<GameSession>();
        return session != null ? session.GetComponent<SceneLoader>() : null;
    }

    // The paddle is steered after its own Update, not before it: ReadKeyboardInput pulls
    // desiredPositionX back to where the paddle already is whenever no key is held, so a target
    // set any earlier in the frame would be thrown away before Move() ever saw it. Move() is
    // called here for the same reason - the one in Update has already run, against the old
    // target. That still leaves exactly one step per frame, at the paddle's own speed.
    private void LateUpdate()
    {
        if (!isEnabled || paddle == null || ball == null || gameCamera == null)
        {
            return;
        }

        if (PauseManager.IsPaused || StoryManager.isStoryActive)
        {
            return;
        }

        // Chasing the ball every single frame is what makes this paddle worse than a human one,
        // not better. The paddle is moved by writing transform.position - a teleport, with no
        // sweep - so a paddle that is still sliding sideways while the ball rests on it collides
        // with it again and again: each depenetration eats the ball's climb, AdjustVelocity puts
        // the speed back along the same flat heading, and the ball dribbles along the top of the
        // paddle at minVelocity until something outside the game gives up. It is held still in
        // the two cases where moving it can only do harm: while the ball is close enough to be
        // touching, and while the ball is on its way up, which is exactly when following it
        // keeps the paddle glued underneath. Both leave the paddle where the last descent put
        // it, so the interception itself is unaffected.
        float restHeight = paddle.mountPoint != null
            ? paddle.mountPoint.position.y - paddle.transform.position.y
            : 2f;

        if (ball.transform.position.y - paddle.transform.position.y < restHeight + ContactMargin)
        {
            return;
        }

        if (ballBody != null && ballBody.velocity.y > 0f)
        {
            return;
        }

        // PaddleMovement measures from the left edge of the view and assumes the camera sits at
        // x = 0, so the same arithmetic is repeated here rather than going through
        // WorldToViewportPoint, which would disagree the moment the camera is moved.
        float screenWidthInUnits = gameCamera.orthographicSize * 2f * gameCamera.aspect;
        if (screenWidthInUnits <= Mathf.Epsilon)
        {
            return;
        }

        float targetX = ball.transform.position.x;
        paddle.SetPosition((targetX + (screenWidthInUnits / 2f)) / screenWidthInUnits);
        paddle.Move();
    }

    private void AcquireSceneObjects()
    {
        if (storyManager == null)
        {
            storyManager = FindObjectOfType<StoryManager>();
        }

        if (ball == null)
        {
            ball = FindObjectOfType<BallMovement>();
            ballBody = ball != null ? ball.GetComponent<Rigidbody2D>() : null;
        }

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (scenarioManager == null && storyManager != null)
        {
            scenarioManager = storyManager.scenarioManager;
        }

        // A paddle swap deactivates the old paddle and parents the new one under the rig, so the
        // active one among the rig's children is always the current one. Looked up through the
        // rig rather than the whole scene, which would also turn up the shop's preview paddles.
        if ((paddle == null || !paddle.gameObject.activeInHierarchy) && PlayerRig.instance != null)
        {
            paddle = PlayerRig.instance.GetComponentInChildren<PaddleMovement>();
        }
    }

    // Everything tied to the scene that is being left. advanceScenarioInMenu and initSceneBounced
    // are deliberately not in here: their whole job is to cross a scene boundary.
    private void ForgetSceneState()
    {
        storyManager = null;
        ball = null;
        ballBody = null;
        paddle = null;
        gameCamera = null;

        leavingGameplay = false;
        scenarioStartRequested = false;
        progressRequested = false;
        progressWaitTimer = 0f;
        storyTimer = 0f;
        launchTimer = 0f;
        endingTimer = 0f;
        menuTimer = 0f;
        idleTimer = 0f;
        pausedTimer = 0f;
        longPauseReported = false;
        recoveryCount = 0;
    }

    private void SetEnabled(bool value)
    {
        if (isEnabled == value)
        {
            return;
        }

        isEnabled = value;
        ForgetSceneState();
        lastSceneName = string.Empty;
        advanceScenarioInMenu = false;
        initSceneBounced = false;

        if (isEnabled)
        {
            HookHealth();
            TopUpHealth();
            Debug.Log($"[{nameof(AutoPlayer)}] on, in scene '{SceneManager.GetActiveScene().name}'");
        }
        else
        {
            UnhookHealth();
            Debug.Log($"[{nameof(AutoPlayer)}] off");
        }
    }

    private void HookHealth()
    {
        if (healthHooked)
        {
            return;
        }

        PlayerHealth.OnHealthLost += RestoreLostLife;
        PlayerHealth.OnDeath += RecoverFromDeath;
        Block.OnBlockBroken += HandleBlockBroken;
        healthHooked = true;
    }

    private void UnhookHealth()
    {
        if (!healthHooked)
        {
            return;
        }

        PlayerHealth.OnHealthLost -= RestoreLostLife;
        PlayerHealth.OnDeath -= RecoverFromDeath;
        Block.OnBlockBroken -= HandleBlockBroken;
        healthHooked = false;
    }

    // The one signal that says the run is actually getting somewhere.
    private void HandleBlockBroken(Vector3 _)
    {
        idleTimer = 0f;
        recoveryCount = 0;
    }

    // Giving the life straight back is what keeps the run going; the displays redraw off
    // OnHealthChanged, so the hearts follow along on their own.
    private void RestoreLostLife()
    {
        if (PlayerHealth.healthTD == null)
        {
            return;
        }

        PlayerHealth.GainHealth();
    }

    // Only reachable if the last life was already gone when autoplay was switched on: LoseHealth
    // decides on death from the value it computed before this handler could put the life back,
    // so the Game Over screen still comes up and still takes a pause lock. Undo all of it rather
    // than leave the run frozen behind a screen nobody is going to dismiss.
    private void RecoverFromDeath()
    {
        if (PlayerHealth.healthTD == null)
        {
            return;
        }

        PlayerHealth.ResetHealth();

        GameOver gameOver = FindObjectOfType<GameOver>();
        if (gameOver != null && gameOver.gameOverScreen != null)
        {
            gameOver.gameOverScreen.SetActive(false);
        }

        PauseManager.ReleaseAll();

        if (PlayerRig.instance != null)
        {
            PlayerRig.instance.LockToPaddle();
        }
    }

    private void TopUpHealth()
    {
        if (PlayerHealth.healthTD == null || PlayerHealth.Health > 0)
        {
            return;
        }

        PlayerHealth.ResetHealth();
    }
}
#endif
