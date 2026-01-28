using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    public GameSpeedManager gameSpeedHandler;
    public ScoreManager scoreDisplayer;
    public HealthDisplayer healthDisplayer;

    private void Awake()
    {
        PlayerHealth.OnDeath += Pause;

        int gameStatusCount = FindObjectsOfType<GameSession>().Length;
        if (gameStatusCount > 1)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        gameSpeedHandler = GetComponent<GameSpeedManager>();
    }

    public void ResetGame()
    {
        Destroy(gameObject);
    }

    public void Pause()
    {
        gameSpeedHandler.Pause();
    }

    public void Unpause()
    {
        gameSpeedHandler.Unpause();
    }
}
