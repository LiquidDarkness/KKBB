using System;
using UnityEngine;

public class DropReceiver : MonoBehaviour
{
    public static event Action<SpeedDropData> OnSpeedDropReceived;
    public static event Action OnDropCollected;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDropReceiver[] dropReceivers = collision.gameObject.GetComponents<IDropReceiver>();

        foreach (var dropReceiver in dropReceivers)
        {
            ProcessReceiver(collision, dropReceiver);
        }

        Destroy(collision.gameObject);
    }

    private void ProcessReceiver(Collider2D collision, IDropReceiver dropReceiver)
    {
        SpeedDropData speedDropData = collision.gameObject.GetComponent<SpeedDropData>();

        if (speedDropData != null)
        {
            OnSpeedDropReceived?.Invoke(speedDropData);
        }

        OnDropCollected?.Invoke();
        dropReceiver.DigestDrop(this);
    }
}
