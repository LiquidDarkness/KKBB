using UnityEngine;
using UnityEngine.UI;

public class ExchangePointsForBonus : MonoBehaviour
{
    public GameSession gameSession;
    public Button buyHealthButton;
    public Button paddle1Buy;
    public Button paddle2Buy;
    public Button paddle3Buy;
    public PaddleMovement paddle1;
    public PaddleMovement paddle2;
    public PaddleMovement paddle3;

    public void Update()
    {
        if (Score.currentScore < 1000)
        {
            buyHealthButton.interactable = false;
        }

        if (Score.currentScore < 500)
        {
            //TODO: tu zakoduj kupno paddli
            paddle1Buy.interactable = false;
            paddle2Buy.interactable = false;
            paddle3Buy.interactable = false;
        }

        else
        {
            buyHealthButton.interactable = true;
            paddle1Buy.interactable = true;
            paddle2Buy.interactable = true;
            paddle3Buy.interactable = true;
        }
    }

    public void Pay(int price)
    {
        if (Score.currentScore >= price)
        {
            Score.AddToScore(-price);
        }
    }

    public void FinaliseHealthExchange()
    {
        if (Score.currentScore >= 1000)
        {
            PlayerHealth.GainHealth();
            Score.AddToScore(-1000);
        }

        else
        {
            Debug.Log("Can't buy more health.");
        }
    }

    public void FinalisePaddleExchange()
    {
        if (Score.currentScore >= 500)
        {
            Score.AddToScore(-500);
        }

        else
        {
            Debug.Log("Can't buy another paddle.");
        }
    }
}
