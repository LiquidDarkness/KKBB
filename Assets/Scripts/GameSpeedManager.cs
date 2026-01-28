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

    public static bool isGamePaused;
    public event Action<float, bool> OnGameSpeedModified;
    public event Action<bool> OnGameSpeedChanged;

    public DiffcultyManager diffcultyManager;
    private Coroutine routine;

    public void Awake()
    {
        DiffcultyManager.OnSettingsChanged += HandleDifficultySettingsChanged;
    }

    private void HandleDifficultySettingsChanged(DifficultySettings _)
    {
        SetGameSpeed();
    }

    public void Start()
    {
        speedChangeSemaphore.RemoveAllListeners();
        speedChangeSemaphore.AddListener(StartValueChange);
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

    //TODO: jeœli wp³yw dropsa sprawia, ¿e prêdkoœæ gry spada poni¿ej 0, wówczas czas trwania spowolnienia
    //zostaje przed³u¿ona o 'duration'.
    //TODO: jeœli wp³yw dropsa sprawia, ¿e prêdkoœæ gry przekracza prêdkoœæ maksymaln¹ ustalon¹ w wybranym
    //poziomie trudnoœci, wówczas czas trwania przyspieszenia zostaje przed³u¿ona o 'duration'.


    //TODO: jak skoñczy siê level, to koñczy siê efekt
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
        if ((gameSpeed > originalGameSpeed && dropInfluence > 0)
            || (gameSpeed < originalGameSpeed && dropInfluence < 0))
        {
            elapsedTime -= duration;
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }
        gameSpeed = originalGameSpeed;
        routine = StartCoroutine(ChangeGameSpeed(dropInfluence, duration));
    }

    public void Pause()
    {
        if (isGamePaused)
        {
            return;
        }
        cachedGameSpeed = gameSpeed;
        gameSpeed = 0.0001f;
        Time.timeScale = gameSpeed;
        isGamePaused = true;
    }

    public void Unpause()
    {
        gameSpeed = cachedGameSpeed;
        Time.timeScale = gameSpeed;
        isGamePaused = false;
    }
}
    /*public IEnumerator ChangeGameSpeed(float dropInfluence, float duration)
    {
        float elapsedTime = 0.0f;
        if (gameSpeed > dropInfluence && (gameSpeed + dropInfluence) <= 20)
        {
            gameSpeed += dropInfluence;
        }

        while (duration > elapsedTime)
        {
            elapsedTime += Time.deltaTime;
            effectCountDown = duration - elapsedTime;
            Time.timeScale = gameSpeed;
            yield return null;
        }

        gameSpeed = originalGameSpeed;
    } */

