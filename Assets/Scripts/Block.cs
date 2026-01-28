using System;
using UnityEngine;

public class Block : MonoBehaviour
{
    public GameObject blockSparklesVFX;
    [SerializeField] Sprite[] hitSprites;
    [SerializeField] int pointsPerBlockDestroyed = 10;
    [SerializeField] int timesHit;
    [SerializeField] DiffcultyManager difficultySettings;
    public static event Action<Vector3> OnBlockBroken;
    public static event Action OnBlockHit;

    public ChainDestroyer chainDestroyer;

    public bool breakable;

    public void Awake()
    {
        Debug.Log(difficultySettings.CurrentSettings.name);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (breakable)
        {
            HandleHit(difficultySettings.CurrentSettings.chainDestructionProbability);
        }

        OnBlockHit?.Invoke();
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
                chainDestroyer.chance = difficultySettings.CurrentSettings.chainDestructionProbability;
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

    public void DestroyBlock()
    {
        TriggerSparklesVFX();
        OnBlockBroken?.Invoke(transform.position);
        Destroy(gameObject);
    }


    public void TriggerSparklesVFX()
    {
        Instantiate(blockSparklesVFX, transform.position, transform.rotation);
    }
}
