using UnityEngine;

public class SpeedDropData : MonoBehaviour, IDropReceiver
{
    public float gameSpeedInfluence;
    public float influenceDuration;

    public void DigestDrop(DropReceiver _)
    {
        GameSpeedManager.speedChangeSemaphore.Invoke(gameSpeedInfluence, influenceDuration);
    }
}
