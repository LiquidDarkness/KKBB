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

    //TODO: dzia³a dla holdera, wartoœæ w DropSpawnerze siê nie aktualizuje. Dlaczego?
    void SetSettings(DifficultySettings settings)
    {
        lotto.FillBucket(settings.drops.Select(d => new KeyValuePair<GameObject, int>(d.prefab, d.weight)));
    }

    public void Spawn(Vector3 position)
    {
        Instantiate(lotto.GetRandomTicket(), position, Quaternion.identity);
    }
}
