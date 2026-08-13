using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainDestroyer : MonoBehaviour
{
    /* 0. Poczekaæ 0,2 sekundy 
     * * 1a. Zebraæ elementy, z którymi collidujemy. 
     * * 1b. Usun¹æ z elementów bloczek, pod którym jesteœmy. 
     * * 2. Destroyowaæ bloczek, pod którym jesteœmy. 
     * * 3. Dla ka¿dego z s¹siadów zespawnowaæ ChainDestroyeya. */
    [SerializeField] private float delayBeforeDestruction = 0.2f;
    [SerializeField] private float radius;
    [SerializeField] private LayerMask blockLayerMask;
    [SerializeField] private ChainDestroyer chainDestroyerPrefab;
    [SerializeField] DiffcultyManager difficultySettings;
    public float chance;

    private Block targetBlock;

    public void OnEnable()
    {
        HandleDifficultyChanged();
        Debug.Log(chance);
    }

    private void HandleDifficultyChanged()
    {
        chance = difficultySettings.CurrentSettings.chainDestructionProbability;
    }

    internal void Trigger(Block blockToDestroy)
    {
        targetBlock = blockToDestroy;
        StartCoroutine(ChainDestructionCoroutine());
    }

    private IEnumerator ChainDestructionCoroutine()
    {
        // 1a. Pobranie wszystkich s¹siadów przez Overlap
        List<Block> neighboringBlocks = targetBlock.CollectNeighboringBlocks(radius, blockLayerMask);

        // 2. Niszczenie bloku za pomoc¹ Twojej metody
        targetBlock.DestroyBlock();

        // 3. Dla ka¿dego s¹siada tworzymy nowy ChainDestroyer
        foreach (Block neighbor in neighboringBlocks)
        {
            if (Random.value > chance /*difficultySettings.CurrentSettings.chainDestructionProbability*/)
            {
                continue;
            }
            // 0. Czekamy 0.2 sekundy
            yield return new WaitForSeconds(delayBeforeDestruction);

            // While we waited, this neighbour may already have been destroyed by something
            // else (another chain, a bomb, the ball). Unity reports destroyed objects as null,
            // and calling into one throws MissingReferenceException the moment it touches
            // transform.
            if (neighbor == null)
            {
                continue;
            }

            neighbor.HandleHit(chance * difficultySettings.CurrentSettings.chainDamageMultiplier);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
