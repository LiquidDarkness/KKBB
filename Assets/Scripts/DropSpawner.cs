using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

public class DropSpawner : MonoBehaviour
{
    public DiffcultyManager diffcultyManager;
    private readonly Lotto<GameObject> lotto = new();

    public void Awake()
    {
        Block.OnBlockBroken += Spawn;
        DiffcultyManager.OnSettingsChanged += SetSettings;
        if (diffcultyManager.CurrentSettings == null)
        {
            Debug.LogWarning("DropDifficultySettings.currentSettings is null at Awake");
        }
        else
        {
            SetSettings(diffcultyManager.CurrentSettings);
        }
        //Debug.Log("DropSpawnerInited");
    }


    public void OnDestroy()
    {
        Block.OnBlockBroken -= Spawn;
        DiffcultyManager.OnSettingsChanged -= SetSettings;
    }

    void SetSettings(DifficultySettings settings)
    {
        lotto.FillBucket(settings.drops.Select(d => new KeyValuePair<GameObject, int>(d.prefab, d.weight)));
    }

    public void Spawn(Vector3 position)
    {
        // Refresh right before rolling: DiffcultyManager.CurrentSettings is always current, but
        // OnSettingsChanged is not raised on every path that changes difficulty (e.g. loading a save),
        // which could leave the cached lotto stale.
        SetSettings(diffcultyManager.CurrentSettings);

        GameObject ticket = lotto.GetRandomTicket();
        if (ticket == null)
        {
            // Either the difficulty has no drops at all, or the one drawn has no prefab on it.
            // Instantiate(null) throws, and this runs on every block broken, so a single gap in
            // the pool would bury the level in exceptions.
            return;
        }

        Instantiate(ticket, position, Quaternion.identity);
    }
}
