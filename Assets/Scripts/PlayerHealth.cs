using System;
using UnityEngine;

public static class PlayerHealth
{
    public const int defaultStartingHealth = 3;

    // Number of lives for the currently selected difficulty (DifficultySettings.livesNumber).
    private static int startingHealth = defaultStartingHealth;

    // Referencja do TD (trzymana np. w PlayerRig albo podawana na start gry).
    public static TypeDistinguisher healthTD;

    public static event Action<int> OnHealthChanged;
    public static event Action OnDeath;
    public static event Action OnHealthLost;

    public static int Health => healthTD?.IntValue ?? startingHealth;

    public static void Initialize(TypeDistinguisher td, int? livesForDifficulty = null)
    {
        healthTD = td;

        if (livesForDifficulty.HasValue && livesForDifficulty.Value > 0)
        {
            startingHealth = livesForDifficulty.Value;
        }

        if (healthTD.IntValue <= 0)
        {
            // Pierwsze odpalenie gry -> ustaw startowe serduszka
            healthTD.SetIntValue(startingHealth);
        }

        // Podnieœ event na start, ¿eby UI od razu siê narysowa³o
        OnHealthChanged?.Invoke(healthTD.IntValue);
    }

    public static void ResetHealth()
    {
        healthTD.SetIntValue(startingHealth);
        OnHealthChanged?.Invoke(healthTD.IntValue);
    }

    public static void GainHealth()
    {
        int newHealth = healthTD.IntValue + 1;
        healthTD.SetIntValue(newHealth);
        OnHealthChanged?.Invoke(newHealth);
    }

    public static void LoseHealth()
    {
        int newHealth = healthTD.IntValue - 1;
        healthTD.SetIntValue(newHealth);

        OnHealthChanged?.Invoke(newHealth);
        OnHealthLost?.Invoke();

        if (newHealth < 0)
        {
            OnDeath?.Invoke();
        }
    }
}
