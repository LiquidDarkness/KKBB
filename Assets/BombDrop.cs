using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombDrop : MonoBehaviour, IDropReceiver
{
    public void DigestDrop(DropReceiver source)
    {
        Debug.Log("Dupa");
        GiveBomb(source.GetComponent<BombSource>());
    }

    private void GiveBomb(BombSource bombSource)
    {
        bombSource.AddBomb();
    }
}
