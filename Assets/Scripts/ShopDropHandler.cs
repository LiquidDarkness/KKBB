using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopDropHandler : MonoBehaviour
{
    public MonoBehaviour dropToSpawn;

    // The price and the button live on the ShopEntry next to this one, which ShopManager reads
    // to lock and unlock the offers. Keeping a second copy here would give the same offer two
    // prices to edit and only one of them any effect.
    private ShopEntry entry;

    private void Awake()
    {
        entry = GetComponent<ShopEntry>();

        if (entry == null)
        {
            Debug.LogWarning($"{name} has no ShopEntry, so its offer is free.");
        }
    }

    private void OnValidate()
    {
        if (dropToSpawn == null)
        {
            return;
        }

        if (dropToSpawn is not IDropReceiver)
        {
            Debug.LogError("DropToSpawn must be IDropReceiver.");
            dropToSpawn = null;
        }
    }

    public void SpawnDrop()
    {
        int price = entry == null ? 0 : entry.price;

        // Charged here rather than trusting the button state: the other listeners on the same
        // click - the sound and the purchase flash - fire independently of this one.
        if (Score.currentScore < price)
        {
            Debug.Log($"Cannot afford {name}: {Score.currentScore} of {price}.");
            return;
        }

        Score.AddToScore(-price);
        (dropToSpawn as IDropReceiver).DigestDrop(null);
    }
}
