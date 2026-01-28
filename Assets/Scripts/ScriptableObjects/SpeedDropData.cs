using UnityEngine;

public class SpeedDropData : MonoBehaviour, IDropReceiver
{
    public float gameSpeedInfluence;
    public float influenceDuration;

    public void DigestDrop()
    {
        GameSpeedManager.speedChangeSemaphore.Invoke(gameSpeedInfluence, influenceDuration);
    }
}
