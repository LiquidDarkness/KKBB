using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ScenarioManager : ScriptableObject
{
    public List<StoryContainer> scenarios;
    public TypeDistinguisher chosenScenario;

    public static event Action<StoryContainer> OnScenarioSettingsChanged;

    public StoryContainer CurrentScenarioSettings
    {
        get => scenarios[chosenScenario.IntValue];
    }

    public void SetSettings(StoryContainer scenario)
    {
        int index = scenarios.IndexOf(scenario);
        chosenScenario.SetValue(index);
        OnScenarioSettingsChanged?.Invoke(scenario);
        //Debug.Log(scenarios[chosenScenario.IntValue]);
        //PlayerPrefs.SetInt(chosenScenario.PrefsKey, index);
        //PlayerPrefs.SetInt(chosenScenario.PrefsKey, index);
        //PlayerPrefs.Save();
        SaveManager.Save();

        /*
        Debug.Log($"Saving to key: {chosenScenario.PrefsKey}");
        Debug.Log($"Scenario set to: " + scenarios[chosenScenario.IntValue]);
        Debug.Log($"Scenario saved as:" + scenarios[PlayerPrefs.GetInt(chosenScenario.PrefsKey)].name);
        Debug.Log(chosenScenario.PrefsKey + chosenScenario.IntValue);
        */
    }
}
