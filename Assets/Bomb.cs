using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    public bool hasBomb;
    public float radius;
    public LayerMask blockLayerMask;
    public GameObject bombDisplay;

    public void Awake()
    {
        RemoveBomb();
    }

    [ContextMenu(nameof(ActivateBomb))]
    public void ActivateBomb()
    {
        hasBomb = true;
        Block.OnBlockHit += HandleBlockHit;
        bombDisplay.SetActive(true);
    }

    private void HandleBlockHit(Block block)
    {
        List<Block> neighboringBlocks = block.CollectNeighboringBlocks(radius, blockLayerMask);
        foreach (Block neighbour in neighboringBlocks)
        {
            neighbour.DestroyBlock();
        }
        RemoveBomb();
        SpawnExplosion();
    }

    public GameObject explosionPrefab;
    private void SpawnExplosion()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity).transform.localScale = Vector3.one * radius;
    }

    private void RemoveBomb()
    {
        hasBomb = false;
        Block.OnBlockHit -= HandleBlockHit;
        bombDisplay.SetActive(false);
    }

    private void OnDestroy()
    {
        Block.OnBlockHit -= HandleBlockHit;
    }
}
