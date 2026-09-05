using System.Collections.Generic;
using UnityEngine;

// Watches the game for the things achievements are made of, and asks Achievements to award them.
// Nothing else in the game knows an achievement exists: every hook here is an event that was
// already being raised for some other reason, so removing this component removes the whole feature.
//
// It lives on GameSession, which is DontDestroyOnLoad - so Awake runs once per launch and the run
// state below survives going back to the menu and returning. Every event it takes is static and
// belongs to objects that die with the Gameplay scene, so all of them are dropped in OnDestroy.
public class AchievementTracker : MonoBehaviour
{
    [Header("Data")]
    public ScenarioManager scenarioManager;
    public DiffcultyManager diffcultyManager;

    [Tooltip("currentLvl - zero means the player is at the start of a scenario.")]
    public TypeDistinguisher currentLevel;

    [Tooltip("dropsCaught - the lifetime tally, kept in the save.")]
    public TypeDistinguisher dropsCaught;

    [Tooltip("ChosenAvatar - which cat is in play, as an index into AvatarSwitcher.avatars.")]
    public TypeDistinguisher chosenAvatar;

    [Tooltip("Which difficulties count as 'hard or above'. Listed rather than matched by name, so renaming an asset cannot quietly break an achievement.")]
    public DifficultySettings[] countsAsHard;

    [Header("Tuning")]
    public int dropsWanted = 500;

    [Tooltip("Seconds of unbroken speed-up, on hard or above, in real time rather than game time.")]
    public float speedStreakWanted = 180f;

    public int heartsWanted = 9;

    [Header("Endless")]
    [Tooltip("Any cat, any difficulty - the one for simply getting somewhere.")]
    public EndlessMilestone endlessForAnyone = new EndlessMilestone { wave = 5 };

    [Tooltip("Ziggy, and only on METAL.")]
    public EndlessMilestone endlessForZiggy = new EndlessMilestone { wave = 10, cat = 2 };

    [Tooltip("Simba Bimba, and only on Easy.")]
    public EndlessMilestone endlessForSimbaBimba = new EndlessMilestone { wave = 15, cat = 1 };

    // What one endless achievement asks for. Kept as data rather than as three pairs of loose
    // numbers so that the difficulty is an asset reference - the same reason countsAsHard is one -
    // and so adding a fourth is a field rather than another branch.
    [System.Serializable]
    public class EndlessMilestone
    {
        public const int AnyCat = -1;

        [Tooltip("The wave that has to be reached, counted from one the way the card on screen says it.")]
        public int wave = 5;

        [Tooltip("Left empty this counts on any difficulty. Otherwise only on this one, held as a reference so that renaming the asset cannot quietly stop the achievement being earned.")]
        public DifficultySettings difficulty;

        [Tooltip("-1 counts for any cat. Otherwise an index into AvatarSwitcher.avatars: 0 Liquid Darkness, 1 Simba Bimba, 2 Ziggy, 3 the tutorial dummy.")]
        public int cat = AnyCat;

        public bool IsReachedBy(int reachedWave, DifficultySettings playedOn, int playedAs)
        {
            if (reachedWave < wave)
            {
                return false;
            }

            if (difficulty != null && playedOn != difficulty)
            {
                return false;
            }

            return cat == AnyCat || playedAs == cat;
        }
    }

    // Which scenario earns which achievement, by asset name. A new scenario adds a line here and a
    // line in Achievements, and nothing else changes.
    private static readonly Dictionary<string, string> ScenarioAchievements = new Dictionary<string, string>
    {
        { TutorialAsset, Achievements.TutorialFinished },
        { "LiquidDarkness", Achievements.LiquidDarknessFinished },
        { "SimbaBimba", Achievements.SimbaBimbaFinished },
        { ZiggiesMundaAsset, Achievements.ZiggiesMundaFinished },
    };

    private const string TutorialAsset = "Tutorial";
    private const string ZiggiesMundaAsset = "ZiggiesMunda";

    private GameSpeedManager gameSpeed;

    // --- Run state, in memory only ---------------------------------------------------------------
    // A scenario spans many levels and can be put down and picked up again, so this is deliberately
    // conservative: see HandleGameplayLoaded.
    private bool runTracked;
    private bool lostLifeThisRun;
    private bool lostLifeThisLevel;
    private bool hardAllRun;
    private float speedStreak;
    private bool speedStreakAwarded;

    private void Awake()
    {
        gameSpeed = GetComponent<GameSpeedManager>();

        StoryManager.OnScenarioFinished += HandleScenarioFinished;
        PlayerHealth.OnDeath += HandleDeath;
        PlayerHealth.OnHealthLost += HandleHealthLost;
        ShopDropHandler.OnPurchase += HandlePurchase;
        DropReceiver.OnDropCollected += HandleDropCollected;
        SceneLoader.OnGameplayLoaded += HandleGameplayLoaded;
        MainManager.OnLevelLoaded += HandleLevelLoaded;
        Level.OnLevelCompleted += HandleLevelCompleted;
        ContinuePurchase.OnContinued += HandleContinued;
        DiffcultyManager.OnSettingsChanged += HandleDifficultyChanged;
        StoryManager.OnBeatShown += HandleBeatShown;

        if (gameSpeed != null)
        {
            gameSpeed.OnGameSpeedModified += HandleSpeedModified;
            gameSpeed.OnGameSpeedChanged += HandleSpeedActive;
        }
    }

    private void OnDestroy()
    {
        StoryManager.OnScenarioFinished -= HandleScenarioFinished;
        PlayerHealth.OnDeath -= HandleDeath;
        PlayerHealth.OnHealthLost -= HandleHealthLost;
        ShopDropHandler.OnPurchase -= HandlePurchase;
        DropReceiver.OnDropCollected -= HandleDropCollected;
        SceneLoader.OnGameplayLoaded -= HandleGameplayLoaded;
        MainManager.OnLevelLoaded -= HandleLevelLoaded;
        Level.OnLevelCompleted -= HandleLevelCompleted;
        ContinuePurchase.OnContinued -= HandleContinued;
        DiffcultyManager.OnSettingsChanged -= HandleDifficultyChanged;
        StoryManager.OnBeatShown -= HandleBeatShown;

        if (gameSpeed != null)
        {
            gameSpeed.OnGameSpeedModified -= HandleSpeedModified;
            gameSpeed.OnGameSpeedChanged -= HandleSpeedActive;
        }
    }

    // --- The run -----------------------------------------------------------------------------

    private void HandleGameplayLoaded()
    {
        bool atTheStart = currentLevel != null && currentLevel.IntValue == 0;

        if (atTheStart)
        {
            BeginRun(watched: true);
            return;
        }

        if (!runTracked)
        {
            // Dropped into the middle of a scenario that began before this component was watching -
            // a save picked up in a later session. Nothing that asks about the whole run can be
            // awarded honestly, so the run counts as already spoiled rather than handing out
            // something unearned.
            BeginRun(watched: false);
        }
    }

    private void BeginRun(bool watched)
    {
        runTracked = watched;
        lostLifeThisRun = !watched;
        hardAllRun = watched && IsHardOrAbove();
        speedStreak = 0f;
    }

    private void HandleHealthLost()
    {
        lostLifeThisRun = true;
        lostLifeThisLevel = true;
    }

    // Every level arrives through MainManager.LoadLevel, so each one starts its own clean slate -
    // including a level replayed after the whole run was already spoiled.
    private void HandleLevelLoaded()
    {
        lostLifeThisLevel = false;
    }

    private void HandleContinued()
    {
        Achievements.Unlock(Achievements.BoughtAContinue);
    }

    private void HandleDeath()
    {
        if (CurrentScenario() == TutorialAsset)
        {
            Achievements.Unlock(Achievements.TutorialGameOver);
        }
    }

    private void HandleScenarioFinished()
    {
        string scenario = CurrentScenario();

        if (ScenarioAchievements.TryGetValue(scenario, out string finished))
        {
            Achievements.Unlock(finished);
        }
        else
        {
            Debug.LogWarning($"[{nameof(AchievementTracker)}] scenario '{scenario}' has no achievement listed - add it here and in Achievements.");
        }

        if (hardAllRun)
        {
            Achievements.Unlock(Achievements.ScenarioOnHard);
        }

        // The tutorial is where a player is meant to be learning, so getting through it untouched
        // is not the same feat.
        if (!lostLifeThisRun && scenario != TutorialAsset)
        {
            Achievements.Unlock(Achievements.ScenarioUnscathed);
        }

        if (PlayerHealth.healthTD != null && PlayerHealth.Health >= heartsWanted)
        {
            Achievements.Unlock(Achievements.NineHearts);
        }

        // Whatever comes next is its own attempt.
        runTracked = false;
    }

    // The lifetime drop tally lives in PlayerPrefs, which only reaches the save file - and so the
    // cloud - when something writes it out. A level is a natural, cheap moment for that; writing on
    // every drop would put the whole settings file on disk several times a second.
    private void HandleLevelCompleted()
    {
        if (!lostLifeThisLevel)
        {
            Achievements.Unlock(Achievements.LevelUnscathed);
        }

        SaveManager.Save();
    }

    private void HandleDifficultyChanged(DifficultySettings settings)
    {
        if (!IsHardOrAbove())
        {
            hardAllRun = false;
            speedStreak = 0f;
        }
    }

    // --- Endless -----------------------------------------------------------------------------

    // Every beat of every scenario comes through here, and only the endless one is answered. There
    // is nothing to remember between waves: the wave number is the whole of the progress and it is
    // saved, so a run picked up in a later session is asked the same question and answers it the
    // same way. The difficulty and the cat are read as they stand, which is the whole story - both
    // are chosen on the way into a run and there is no road from the game back to either without
    // starting a different run, which zeroes the wave.
    private void HandleBeatShown(int beat)
    {
        if (scenarioManager == null || !(scenarioManager.CurrentScenarioSettings is EndlessScenario))
        {
            return;
        }

        int wave = EndlessScenario.WaveOf(beat);
        DifficultySettings difficulty = diffcultyManager != null ? diffcultyManager.CurrentSettings : null;
        int cat = chosenAvatar != null ? chosenAvatar.IntValue : EndlessMilestone.AnyCat;

        Award(endlessForAnyone, Achievements.EndlessWaveFive, wave, difficulty, cat);
        Award(endlessForZiggy, Achievements.EndlessZiggyOnMetal, wave, difficulty, cat);
        Award(endlessForSimbaBimba, Achievements.EndlessSimbaOnEasy, wave, difficulty, cat);
    }

    private static void Award(EndlessMilestone milestone, string apiName, int wave, DifficultySettings difficulty, int cat)
    {
        if (milestone != null && milestone.IsReachedBy(wave, difficulty, cat))
        {
            Achievements.Unlock(apiName);
        }
    }

    // --- Counters ----------------------------------------------------------------------------

    private void HandleDropCollected()
    {
        if (dropsCaught == null)
        {
            return;
        }

        int total = dropsCaught.IntValue + 1;
        dropsCaught.SetIntValue(total);

        if (total >= dropsWanted)
        {
            Achievements.Unlock(Achievements.FiveHundredDrops);
        }
    }

    private void HandlePurchase(MonoBehaviour bought)
    {
        Achievements.Unlock(Achievements.FirstPurchase);

        // gameSpeedInfluence is what tells the two speed offers apart: the shop sells a +1 and a
        // -0.5 of the same component.
        if (bought is SpeedDropData speedDrop
            && speedDrop.gameSpeedInfluence > 0f
            && CurrentScenario() == ZiggiesMundaAsset)
        {
            Achievements.Unlock(Achievements.SpeedBoughtInZiggiesMunda);
        }
    }

    // Raised every frame while an effect is running, and not at all while the game is paused - so
    // this measures exactly the time the effect was actually up.
    private void HandleSpeedModified(float timeLeft, bool isSpedUp)
    {
        if (!isSpedUp || !IsHardOrAbove())
        {
            speedStreak = 0f;
            return;
        }

        // Real seconds, not game seconds. The whole point of a speed-up is that the game clock runs
        // faster, so Time.deltaTime would count a minute of it as two.
        speedStreak += Time.unscaledDeltaTime;

        if (!speedStreakAwarded && speedStreak >= speedStreakWanted)
        {
            speedStreakAwarded = true;
            Achievements.Unlock(Achievements.SpeedStreakOnHard);
        }
    }

    // Catching another speed-up extends the effect that is running rather than starting a new one,
    // so this only arrives when the speed really has gone back to normal - which is the break in
    // the streak.
    private void HandleSpeedActive(bool isActive)
    {
        if (!isActive)
        {
            speedStreak = 0f;
        }
    }

    // --- Reading the game --------------------------------------------------------------------

    private string CurrentScenario()
    {
        StoryContainer scenario = scenarioManager != null ? scenarioManager.CurrentScenarioSettings : null;
        return scenario == null ? string.Empty : scenario.name;
    }

    private bool IsHardOrAbove()
    {
        if (diffcultyManager == null || countsAsHard == null)
        {
            return false;
        }

        DifficultySettings current = diffcultyManager.CurrentSettings;

        foreach (DifficultySettings candidate in countsAsHard)
        {
            if (candidate == current)
            {
                return true;
            }
        }

        return false;
    }
}
