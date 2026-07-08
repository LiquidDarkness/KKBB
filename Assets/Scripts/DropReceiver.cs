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


    /*
    private void OnCollisionEnter2D_Back(Collision2D collision)
    {
        SpeedDropData dropDataComponent = collision.gameObject.GetComponent<SpeedDropData>();
        PaddleChanger paddleChangerComponent = collision.gameObject.GetComponent<PaddleChanger>();
        PointAdder pointAdderComponent = collision.gameObject.GetComponent<PointAdder>();
        HealthDrop healthDrop = collision.gameObject.GetComponent<HealthDrop>();

        if (pointAdderComponent != null)
        {
            Debug.Log(Score.currentScore);
            Score.AddToScore(pointAdderComponent.pointsUponCollection);
            Debug.Log("Points collected:" + pointAdderComponent.pointsUponCollection);
            Debug.Log(Score.currentScore);
            Destroy(collision.gameObject);
        }

        if (dropDataComponent != null && dropDataComponent.gameSpeedInfluence != 0)
        {
            OnDropReceived?.Invoke(dropDataComponent);
            //gameSession.SetDropData(dropDataComponent);
            //gameSession.dropHandler.StartValueChange(dropDataComponent.gameSpeedInfluence, dropDataComponent.influenceDuration);
            Destroy(collision.gameObject);
        }

        if (paddleChangerComponent != null && paddleChangerComponent.replacingPaddle != null)
        {
            paddleChangerComponent.ChangePaddle();
            //gameSession = paddleMovement.gameSession;
            Destroy(collision.gameObject);
        }

        if (healthDrop != null)
        {
            healthDrop.GainHealth();
            Destroy(collision.gameObject);
        }

        OnDropCollected?.Invoke();
    }
    */
}
