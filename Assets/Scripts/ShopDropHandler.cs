using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopDropHandler : MonoBehaviour
{
    public MonoBehaviour dropToSpawn;

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
        (dropToSpawn as IDropReceiver).DigestDrop();
    }
}
