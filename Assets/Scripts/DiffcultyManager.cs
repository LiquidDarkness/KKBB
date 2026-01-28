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
        get => dropDifficulties[difficultyLevel.IntValue];
    }

    public void SetSettings(DifficultySettings difficultySettings)
    {
        int valueToSet = dropDifficulties.IndexOf(difficultySettings);
        difficultyLevel.SetValue(valueToSet);
        SaveManager.Save();
        //PlayerPrefs.SetInt(difficultyLevel.PrefsKey, valueToSet);
        OnSettingsChanged?.Invoke(difficultySettings);
        Debug.Log(difficultyLevel.IntValue);
    }
}