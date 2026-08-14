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
    private Coroutine routine;

    public void Awake()
    {
        DiffcultyManager.OnSettingsChanged += HandleDifficultySettingsChanged;
        PauseManager.OnPause += HandlePause;
        PauseManager.OnUnpause += HandleUnpause;
        Level.OnLevelCompleted += HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        DiffcultyManager.OnSettingsChanged -= HandleDifficultySettingsChanged;
        PauseManager.OnPause -= HandlePause;
        PauseManager.OnUnpause -= HandleUnpause;
        Level.OnLevelCompleted -= HandleLevelCompleted;
    }

    private void HandleLevelCompleted()
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
        originalGameSpeed = ClampToDifficulty(diffcultyManager.CurrentSettings.baseGameSpeed);
        gameSpeed = originalGameSpeed;
        ApplyTimeScale();
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
