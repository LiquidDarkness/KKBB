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
        get
        {
            // Same guard as DiffcultyManager: a saved index can fall outside the list after the
            // list is edited, and throwing here would leave the player stuck in a broken save.
            int index = chosenScenario.IntValue;
            if (index < 0 || index >= scenarios.Count)
            {
                Debug.LogWarning($"[{nameof(ScenarioManager)}] Saved scenario index {index} is outside scenarios (0-{scenarios.Count - 1}). Falling back to the first entry.", this);
                return scenarios[0];
            }

            return scenarios[index];
        }
    }

    public void SetSettings(StoryContainer scenario)
    {
        int index = scenarios.IndexOf(scenario);

        if (index < 0)
        {
            string assetName = scenario == null ? "<missing asset>" : scenario.name;
            Debug.LogError($"[{nameof(ScenarioManager)}] '{assetName}' is not in scenarios - add it to the list on this asset. Keeping the current scenario.", this);
            return;
        }

        chosenScenario.SetValue(index);
        OnScenarioSettingsChanged?.Invoke(scenario);
        SaveManager.Save();
    }
}
