using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionExposer : MonoBehaviour
{
    public UnityEvent onCollided;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Trigger();
    }

    [ContextMenu("Test Trigger")]
    private void Trigger()
    {
        onCollided.Invoke();
    }
}
