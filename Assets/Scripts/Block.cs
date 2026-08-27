using System;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public GameObject blockSparklesVFX;
    [SerializeField] Sprite[] hitSprites;
    // Five for a block that goes down to one boink, fifteen for one that has to be worked at -
    // the invisible blocks take three. Kept per block rather than worked out from hitSprites, so
    // a block can be worth something unusual without a rule having to be written for it.
    [SerializeField] int pointsPerBlockDestroyed = 5;
    [SerializeField] int timesHit;
    [SerializeField] DiffcultyManager difficultySettings;
    public static event Action<Vector3> OnBlockBroken;
    public static event Action<Block> OnBlockHit;

    public ChainDestroyer chainDestroyer;

    public bool breakable;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (breakable)
        {
            HandleHit(difficultySettings.CurrentSettings.chainDestructionProbability);
        }

        OnBlockHit?.Invoke(this);
    }

    public bool IsDestroyed
    {
        get
        {
            return timesHit >= hitSprites.Length;
        }
    }

    public void HandleHit(float chainChance = 0)
    {
        // Unbreakable blocks are excluded from Level.breakableBlocks, so letting chain
        // destruction or a bomb knock one out desynchronises the count and the level
        // finishes early, with blocks still standing.
        if (!breakable)
        {
            return;
        }

        if (IsDestroyed)
        {
            return;
        }

        timesHit++;
        if (IsDestroyed)
        {
            Score.AddToScore(pointsPerBlockDestroyed);
            float chance = UnityEngine.Random.value;
            if (chance < chainChance)
            {
                Instantiate(chainDestroyer, transform.position, Quaternion.identity).Trigger(this);
            }
            else
            {
                DestroyBlock();
            }
        }

        else
        {
            ShowNextHitSprite();
        }
    }

    private void ShowNextHitSprite()
    {
        int spriteIndex = timesHit - 1;
        if (hitSprites[spriteIndex] != null)
        {
            GetComponent<SpriteRenderer>().sprite = hitSprites[spriteIndex];
        }
        else
        {
            Debug.LogError("Block sprite is missing form array." + gameObject.name);
        }
    }

    // Guards against a block being destroyed twice: several systems (chain destruction,
    // bombs, the ball) can each hold a reference to the same block and act on it in the same
    // frame or across frames.
    private bool isBeingDestroyed;

    public void DestroyBlock()
    {
        if (isBeingDestroyed)
        {
            return;
        }
        isBeingDestroyed = true;

        TriggerSparklesVFX();
        OnBlockBroken?.Invoke(transform.position);
        Destroy(gameObject);
    }


    public void TriggerSparklesVFX()
    {
        // Nothing to spawn is a valid state now: the effect is left unassigned while the
        // player has Reduce motion on, and Instantiate(null) throws.
        if (blockSparklesVFX == null || OptionSettings.MotionReduced)
        {
            return;
        }

        Instantiate(blockSparklesVFX, transform.position, transform.rotation);
    }

    public List<Block> CollectNeighboringBlocks(float radius, int blockLayerMask)
    {
        List<Block> neighboringBlocks = new List<Block>();

        Collider2D[] hitResults = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            blockLayerMask
        );

        foreach (Collider2D hitCollider in hitResults)
        {
            Block block = hitCollider.GetComponent<Block>();
            if (block != null)
            {
                neighboringBlocks.Add(block);
            }
        }

        // 1b. Usuniêcie z listy s¹siadów tego bloku, który w³aœnie bêdzie niszczony
        neighboringBlocks.Remove(this);
        return neighboringBlocks;
    }
}
