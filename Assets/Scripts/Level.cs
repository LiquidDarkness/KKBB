using System;
using UnityEngine;

public class Level : MonoBehaviour
{
    public int breakableBlocks;
    public static event Action OnLevelCompleted;

    public Transform content;

    public void Awake()
    {
        MainManager.OnLevelLoaded += CountBlocks;
        Block.OnBlockBroken += BlockDestroyed;
    }

    public void CountBlocks()
    {
        breakableBlocks = 0;

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
        if (breakableBlocks == 0)
        {
            OnLevelCompleted?.Invoke();
            Debug.Log("Block hit and level completed");
        }
    }
}

