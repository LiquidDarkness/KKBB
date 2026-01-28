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
        List<Block> neighboringBlocks = CollectNeighboringBlocks();

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
            //TODO: wartoœæ 0.15f przerzuciæ jako zmienn¹ do wybranego poziomu trudnoœci, gdzie 0.15 to najwy¿sza sensowna wartoœæ
            neighbor.HandleHit(chance * 0.15f);
        }

        Destroy(gameObject);
    }

    [ContextMenu("Test Chain")]
    private List<Block> CollectNeighboringBlocks()
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
        neighboringBlocks.Remove(targetBlock);
        return neighboringBlocks;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
