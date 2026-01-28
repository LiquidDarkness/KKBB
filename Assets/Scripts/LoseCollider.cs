using UnityEngine;

public class LoseCollider : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<BallMovement>(out BallMovement ball))
        {
            PlayerHealth.LoseHealth();

            // zamiast ball.ReLockBall()
            PlayerRig.instance.LockToPaddle();

            return;
        }

        Destroy(collision.gameObject);
    }
}

