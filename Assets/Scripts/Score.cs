using System;
using UnityEngine;

public static class Score
{
    [Min(0)]
    public static int currentScore;
    public static event Action<int> OnScoreChanged;

    // What a point is worth right now. One for every written scenario; endless raises it wave by
    // wave, which is the whole of "the further in you get, the better it pays". Set by
    // EndlessRunController and put back to one the moment that goes away, so a story scenario can
    // never inherit the multiplier of the endless run before it.
    public static float pointsMultiplier = 1f;

    public static void AddToScore(int points)
    {
        // Only what is earned is multiplied. Spending comes through here as a negative, and the
        // shop, the continue and everything else that charges the player has to cost exactly what
        // it said it would.
        int amount = points > 0 ? Mathf.RoundToInt(points * pointsMultiplier) : points;

        currentScore = currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    public static void SetScore(int points)
    {
        currentScore = points;
        OnScoreChanged?.Invoke(currentScore);
    }
}
