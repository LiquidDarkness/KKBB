using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthDrop : MonoBehaviour, IDropReceiver
{
    public void DigestDrop()
    {
        GainHealth();
    }

    public void GainHealth()
    {
        PlayerHealth.GainHealth();
    }
}
