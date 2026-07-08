using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public Object target;

    public void TriggerDestruction()
    {
        Destroy(target);
    }
}
