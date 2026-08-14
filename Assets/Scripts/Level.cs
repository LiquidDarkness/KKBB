using System;
using UnityEngine;

public class Level : MonoBehaviour
{
    public int breakableBlocks;
    private bool hasCompleted;
    public static event Action OnLevelCompleted;

    public Transform content;

    public void Awake()
    {
        MainManager.OnLevelLoaded += CountBlocks;
        Block.OnBlockBroken += BlockDestroyed;
    }

    public void OnDestroy()
    {
        MainManager.OnLevelLoaded -= CountBlocks;
        Block.OnBlockBroken -= BlockDestroyed;
    }

    public void CountBlocks()
    {
        breakableBlocks = 0;
        hasCompleted = false;

        foreach (Block block in content.GetComponentsInChildren<Block>(true))
        {
            if (block.breakable)
            {
                breakableBlocks++;
            }
        }
    }

    public void BlockDestroyed(Vector3 _)
    {
        breakableBlocks--;
        CheckBreakableCount();
    }

    public void CheckBreakableCount()
    {
        // <= instead of == so that a miscount can never softlock the level: overshooting past
        // zero would make an exact comparison miss the finish forever. hasCompleted keeps the
        // event one-shot per level, since further destructions can still drive the count down.
        if (breakableBlocks <= 0 && !hasCompleted)
        {
            hasCompleted = true;
            Debug.Log("Block hit and level completed");
            OnLevelCompleted?.Invoke();
        }
    }
}

