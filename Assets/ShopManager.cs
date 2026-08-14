using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public List<ShopEntry> shopEntries;

    // The shop spends the score the player can actually see, which lives in Score.currentScore.
    // It used to read the scoreValue TypeDistinguisher instead, but that PlayerPrefs copy is
    // only written when a level is completed - mid-level it still held the previous level's
    // total, zero on the first one, so every offer stayed locked no matter how many points the
    // player had racked up.
    public void OnEnable()
    {
        Score.OnScoreChanged += HandleScoreChanged;
        RefreshEntries();
    }

    public void OnDisable()
    {
        Score.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int _)
    {
        RefreshEntries();
    }

    // Also runs while the shop is open, not just when it is opened: points earned with the shop
    // on screen now unlock their offers straight away.
    private void RefreshEntries()
    {
        foreach (ShopEntry entry in shopEntries)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            // >= rather than >, so an offer costing exactly what the player holds is affordable.
            entry.button.interactable = Score.currentScore >= entry.price;
        }
    }
}
