using System;
using UnityEngine;

public static class Score
{
    [Min(0)]
    public static int currentScore;
    public static event Action<int> OnScoreChanged;

    public static void AddToScore(int points)
    {
        currentScore = currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }
}
