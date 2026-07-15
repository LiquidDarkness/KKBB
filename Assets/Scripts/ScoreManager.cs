using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    public TypeDistinguisher savedScore;
    public GameObject scoreContainer;
    public bool canBePurchased;

    public static event Action<bool> OnPurchaseAttempt;

    public void Awake()
    {
        Debug.Log("ScoreManager created");
        Score.OnScoreChanged += HandleScoreChanged;
        SceneLoader.OnGameplayLoaded += ShowScore;
        SceneLoader.OnMenuLoaded += HideScore;
        Level.OnLevelCompleted += HandleLevelCompleted;
        LoadScore();
        HideScore();
    }

    private void HandleLevelCompleted()
    {
        savedScore.SetIntValue(Score.currentScore);
    }

    public void OnDestroy()
    {
        Score.OnScoreChanged -= HandleScoreChanged;
    }

    public void Start()
    {
        scoreText.text = Score.currentScore.ToString();
    }

    public void HideScore()
    {
        if (this == null)
        {
            //UnityThings. Nie dzia³a xd
            return;
        }
        scoreContainer.SetActive(false);
    }

    public void HandleScoreChanged(int scoreToDisplay)
    {
        ShowScore();
        scoreText.text = "";
        scoreText.text = scoreToDisplay.ToString();
    }

    private void ShowScore()
    {
        if (scoreContainer.active != true)
        {
            scoreContainer.SetActive(true);
        }
    }

    public void LoadScore()
    {
        Score.SetScore(savedScore.IntValue);
    }

    public void Purchase(int cost, ShopDropHandler dropHandler)
    {
        if (Score.currentScore >= cost)
        {
            Score.AddToScore(-cost);
            dropHandler.SpawnDrop();
            OnPurchaseAttempt.Invoke(true);
            Debug.Log($"[ScoreManager] Purchase successful! Spent {cost} points.");
        }
        else
        {
            OnPurchaseAttempt.Invoke(false);
            Debug.Log("[ScoreManager] Not enough points to buy this item.");
            // tutaj dodaæ wyœwietlenie komunikatu w UI
        }
    }
}
