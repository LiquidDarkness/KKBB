using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointAdder : MonoBehaviour, IDropReceiver
{
    public int pointsUponCollection;

    public void DigestDrop()
    {
        Score.AddToScore(pointsUponCollection);
    }
}
