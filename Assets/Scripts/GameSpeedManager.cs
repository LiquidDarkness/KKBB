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
    private float cachedGameSpeed;
    private float elapsedTime = 0.0f;

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
        Time.timeScale = gameSpeed;
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
        originalGameSpeed = diffcultyManager.CurrentSettings.baseGameSpeed;
        gameSpeed = originalGameSpeed;
        Time.timeScale = gameSpeed;
    }

    public void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        StopAllCoroutines();
        Time.timeScale = gameSpeed;
    }

    public IEnumerator ChangeGameSpeed(float dropInfluence, float duration)
    {
        elapsedTime = 0;
        gameSpeed += dropInfluence;
        float maxGameSpeed = diffcultyManager.CurrentSettings.maxSpeed;

        OnGameSpeedChanged?.Invoke(true);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            effectCountDown = duration - elapsedTime;
            Time.timeScale = gameSpeed;
            bool isGameSpedUp = gameSpeed > originalGameSpeed;
            OnGameSpeedModified?.Invoke(effectCountDown, isGameSpedUp);
            yield return null;
        }

        gameSpeed = originalGameSpeed;
        Time.timeScale = gameSpeed;
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
        cachedGameSpeed = gameSpeed;
        gameSpeed = 0.0001f;
        Time.timeScale = gameSpeed;
    }

    private void HandleUnpause()
    {
        gameSpeed = cachedGameSpeed;
        Time.timeScale = gameSpeed;
    }
}
