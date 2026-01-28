using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public GameObject gameOverScreen;

    public void Awake()
    {
        PlayerHealth.OnDeath += DisplayGameOver;
    }

    public void DisplayGameOver()
    {
        gameOverScreen.SetActive(true);
    }
}
