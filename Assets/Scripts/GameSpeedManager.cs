using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameSpeedManager : MonoBehaviour
{
    [Range(0.0001f, 20)] public float gameSpeed = 1f;
    public static UnityEvent<float, float> speedChangeSemaphore = new();
    public float effectCountDown;
    public float originalGameSpeed;
    public float minGameSpeed;
    private float elapsedTime = 0.0f;

    [Tooltip("Time scale while paused. Not quite zero, so scaled-time animations do not hard-freeze.")]
    public float pausedTimeScale = 0.0001f;

    private const float MinimumGameSpeed = 0.0001f;

    // Pausing used to be expressed by writing gameSpeed itself, which made this manager mistake
    // the pause for a running slow-down effect: buying a speed-up while the shop was open was
    // read as "reverse the current effect", and the unpause then overwrote the bought speed with
    // the pre-pause one - leaving a speed-up purchase displayed and behaving as a slow-down.
    // The pause is now a flag of its own and gameSpeed only ever means the speed of play.
    private bool isPaused;

    public event Action<float, bool> OnGameSpeedModified;
    public event Action<bool> OnGameSpeedChanged;

    public DiffcultyManager diffcultyManager;

    [Tooltip("Read to find out whether the scenario being played sets its own pace. Endless does: every wave is a little quicker than the one before, up from the chosen difficulty and part of the way towards its ceiling.")]
    public ScenarioManager scenarioManager;

    [Tooltip("Which beat the player is on, which for endless is which wave. Left empty, an endless run simply never speeds up.")]
    public TypeDistinguisher currentLvl;

    private Coroutine routine;

    [Tooltip("The scene an effect belongs to. Leaving it puts the speed back and takes the border away.")]
    public string gameplaySceneName = "Gameplay";

    public void Awake()
    {
        DiffcultyManager.OnSettingsChanged += HandleDifficultySettingsChanged;
        PauseManager.OnPause += HandlePause;
        PauseManager.OnUnpause += HandleUnpause;
        Level.OnLevelCompleted += HandleLevelCompleted;
        SceneLoader.OnSceneChanged += HandleSceneChanged;
        // A new beat can mean a new pace, which is what makes endless tighten as it goes. For a
        // written scenario this recomputes the same number it already held.
        StoryManager.OnBeatShown += HandleBeatShown;
    }

    private void OnDestroy()
    {
        DiffcultyManager.OnSettingsChanged -= HandleDifficultySettingsChanged;
        PauseManager.OnPause -= HandlePause;
        PauseManager.OnUnpause -= HandleUnpause;
        Level.OnLevelCompleted -= HandleLevelCompleted;
        SceneLoader.OnSceneChanged -= HandleSceneChanged;
        StoryManager.OnBeatShown -= HandleBeatShown;
    }

    private void HandleBeatShown(int _)
    {
        SetGameSpeed();
    }

    private void HandleLevelCompleted()
    {
        EndEffect();
    }

    // This manager and the border it drives both live on GameSession, which outlives every scene
    // load. Nothing used to end a running effect on the way out of gameplay, so a ramp caught in
    // the last rally went on counting down over the main menu - where there was nothing left for it
    // to speed up and no way to be rid of it.
    private void HandleSceneChanged(string sceneName)
    {
        if (sceneName == gameplaySceneName)
        {
            return;
        }

        EndEffect();
    }

    private void EndEffect()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        gameSpeed = originalGameSpeed;
        // Left standing, this keeps the finished effect's remaining time around, and
        // StartValueChange reads it to decide whether an opposing drop can cancel what is
        // running - with nothing running it must read as zero.
        effectCountDown = 0;
        ApplyTimeScale();
        OnGameSpeedChanged?.Invoke(false);
    }

    private void HandleDifficultySettingsChanged(DifficultySettings _)
    {
        SetGameSpeed();
    }

    public void Start()
    {
        speedChangeSemaphore.RemoveAllListeners();
        speedChangeSemaphore.AddListener(StartValueChange);
        SetGameSpeed();
        originalGameSpeed = gameSpeed;
    }

    public void SetGameSpeed()
    {
        originalGameSpeed = ClampToDifficulty(BaseSpeed());
        gameSpeed = originalGameSpeed;
        ApplyTimeScale();
    }

    // The pace a level starts at. Normally the chosen difficulty says it outright; an endless run
    // says it per wave instead, and asks for it in terms of the very same difficulty - so picking
    // METAL still means starting at METAL speed, only with somewhere left to climb.
    private float BaseSpeed()
    {
        DifficultySettings difficulty = diffcultyManager.CurrentSettings;

        if (scenarioManager != null && currentLvl != null
            && scenarioManager.CurrentScenarioSettings is EndlessScenario endless)
        {
            return endless.SpeedForWave(difficulty, EndlessScenario.WaveOf(currentLvl.IntValue));
        }

        return difficulty.baseGameSpeed;
    }

    // baseGameSpeed is the pace a level runs at, maxSpeed the ceiling no drop may push Kitty
    // past. The lower bound only mirrors the Range on gameSpeed - a time scale of zero or less
    // would stop the game outright rather than slow it down.
    private float ClampToDifficulty(float speed)
    {
        return Mathf.Clamp(speed, MinimumGameSpeed, diffcultyManager.CurrentSettings.maxSpeed);
    }

    public void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        StopAllCoroutines();
        ApplyTimeScale();
    }

    public IEnumerator ChangeGameSpeed(float dropInfluence, float duration)
    {
        elapsedTime = 0;
        gameSpeed = ClampToDifficulty(gameSpeed + dropInfluence);
        effectCountDown = duration;

        OnGameSpeedChanged?.Invoke(true);

        // Show the new effect straight away. OnGameSpeedChanged only switches the indicator on;
        // its colour and its countdown come from OnGameSpeedModified, which the loop below keeps
        // silent while the game is paused. Without this first call a drop bought in the shop lit
        // the indicator up still wearing the previous drop's colour and its run-out reading -
        // which is why buying a speed-up showed a slow-down sitting at 0.0s until the shop was
        // closed.
        OnGameSpeedModified?.Invoke(effectCountDown, gameSpeed > originalGameSpeed);

        while (elapsedTime < duration)
        {
            // An effect bought in the shop must not start running down while the shop is still
            // on screen: the countdown, the speed and the border all wait for the unpause.
            if (!isPaused)
            {
                elapsedTime += Time.deltaTime;
                effectCountDown = duration - elapsedTime;
                ApplyTimeScale();
                bool isGameSpedUp = gameSpeed > originalGameSpeed;
                OnGameSpeedModified?.Invoke(effectCountDown, isGameSpedUp);
            }

            yield return null;
        }

        gameSpeed = originalGameSpeed;
        ApplyTimeScale();
        OnGameSpeedChanged?.Invoke(false);
    }

    public void StartValueChange(float dropInfluence, float duration)
    {
        bool isSpedUp = gameSpeed > originalGameSpeed;
        bool isSlowedDown = gameSpeed < originalGameSpeed;

        if ((isSpedUp && dropInfluence > 0) || (isSlowedDown && dropInfluence < 0))
        {
            // Same direction as the currently running effect: extend the total duration by the
            // full duration of the newly caught drop.
            elapsedTime -= duration;
            return;
        }

        if ((isSpedUp && dropInfluence < 0) || (isSlowedDown && dropInfluence > 0))
        {
            if (effectCountDown > duration)
            {
                // Opposite direction, but shorter than what is left of the current effect: it just
                // eats into the remaining time, current effect keeps running as-is.
                elapsedTime += duration;
                return;
            }
            // Opposite direction and at least as long as what is left: current effect is fully
            // cancelled out, fall through and catch this drop as if nothing was running.
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }
        gameSpeed = originalGameSpeed;
        routine = StartCoroutine(ChangeGameSpeed(dropInfluence, duration));
    }

    private void HandlePause()
    {
        isPaused = true;
        ApplyTimeScale();
    }

    private void HandleUnpause()
    {
        isPaused = false;
        ApplyTimeScale();
    }

    // The single place that drives Time.timeScale, so a pause can never be lost to an effect
    // writing the scale behind its back, nor the other way round.
    private void ApplyTimeScale()
    {
        Time.timeScale = isPaused ? pausedTimeScale : gameSpeed;
    }
}
