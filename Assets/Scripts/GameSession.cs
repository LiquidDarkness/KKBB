using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    public ScoreManager scoreDisplayer;
    public HealthDisplayer healthDisplayer;

    const string PAUSE_LOCK = nameof(GameSession);

    private void Awake()
    {
        PlayerHealth.OnDeath += Pause;
    }

    public void ResetGame()
    {
        Destroy(gameObject);
    }

    public void Pause()
    {
        PauseManager.Pause(PAUSE_LOCK);
    }

    public void Unpause()
    {
        PauseManager.Unpause(PAUSE_LOCK);
    }
}