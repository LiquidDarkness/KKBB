using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DiffcultyManager : ScriptableObject
{
    public List<DifficultySettings> dropDifficulties;
    public TypeDistinguisher difficultyLevel;

    public static event Action<DifficultySettings> OnSettingsChanged;

    public DifficultySettings CurrentSettings
    {
        get
        {
            // A saved index can fall outside the list if the list was edited (or if an older
            // build managed to store a bad value). Fall back instead of throwing, so a stale
            // save can never lock the player out of the game.
            int index = difficultyLevel.IntValue;
            if (index < 0 || index >= dropDifficulties.Count)
            {
                Debug.LogWarning($"[{nameof(DiffcultyManager)}] Saved difficulty index {index} is outside dropDifficulties (0-{dropDifficulties.Count - 1}). Falling back to the first entry.", this);
                return dropDifficulties[0];
            }

            return dropDifficulties[index];
        }
    }

    public void SetSettings(DifficultySettings difficultySettings)
    {
        int valueToSet = dropDifficulties.IndexOf(difficultySettings);

        // IndexOf returns -1 for anything missing from the list, and storing that used to make
        // every later CurrentSettings read throw. Keep the previous difficulty instead and say
        // loudly what needs fixing.
        if (valueToSet < 0)
        {
            string assetName = difficultySettings == null ? "<missing asset>" : difficultySettings.name;
            Debug.LogError($"[{nameof(DiffcultyManager)}] '{assetName}' is not in dropDifficulties - add it to the list on this asset. Keeping the current difficulty.", this);
            return;
        }

        difficultyLevel.SetValue(valueToSet);
        SaveManager.Save();
        OnSettingsChanged?.Invoke(difficultySettings);
    }
}