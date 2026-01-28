using UnityEngine;

public class PlayerRig : MonoBehaviour
{
    [SerializeReference] private BallMovement ball;
    [SerializeReference] private PaddleMovement paddle;
    public TypeDistinguisher healthTD;

    public static PlayerRig instance;

    private void Awake()
    {
        instance = this;

        // Inicjalizacja PlayerHealth
        PlayerHealth.Initialize(healthTD);

        // Subskrypcje eventów
        PaddleChanger.OnPaddleChanged += HandlePaddleChanged;
        PlayerHealth.OnHealthLost += HandleHealthLost;
    }

    private void OnDestroy()
    {
        PaddleChanger.OnPaddleChanged -= HandlePaddleChanged;
        PlayerHealth.OnHealthLost -= HandleHealthLost;
    }

    private void HandleHealthLost()
    {
        if (!ball.hasBeenLaunched)
        {
            LockToPaddle(); // tylko jeœli kulka nie jest w locie
        }

        //SaveManager.Save();
    }

    public void LaunchBall()
    {
        ball.LaunchBall();
    }

    public void LockToPaddle()
    {
        ball.LockBall(paddle.mountPoint);
    }

    private void HandlePaddleChanged(PaddleMovement newPaddle)
    {
        if (paddle != null)
        {
            // Zachowujemy pozycjê starego paddle
            float oldX = paddle.currentPositionX;

            // Dezaktywujemy stary paddle
            paddle.gameObject.SetActive(false);

            // Podpinamy nowy paddle
            newPaddle.transform.SetParent(this.transform);

            // Synchronizujemy pozycjê
            newPaddle.currentPositionX = oldX;
            newPaddle.desiredPositionX = oldX;
            newPaddle.Move();

            // Aktywujemy
            newPaddle.gameObject.SetActive(true);
        }
        else
        {
            newPaddle.transform.SetParent(this.transform);
        }

        // Aktualizujemy referencjê
        paddle = newPaddle;

        // Lock kulki tylko jeœli nie jest w locie
        if (!ball.hasBeenLaunched)
        {
            LockToPaddle();
        }
    }
}
