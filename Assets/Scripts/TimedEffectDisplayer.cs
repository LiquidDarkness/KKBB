using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimedEffectDisplayer : MonoBehaviour
{
    public Image border;
    [SerializeField] TextMeshProUGUI timer;
    [SerializeField] GameSpeedManager speedManager;
    [SerializeField] Color speedUpColor;
    [SerializeField] Color speedDownColor;

    private void Awake()
    {
        DisplayBorderEffect(false);
        speedManager.OnGameSpeedChanged += DisplayBorderEffect;
        speedManager.OnGameSpeedModified += UpdateTimer;
    }

    private void DisplayBorderEffect(bool isSpeedChanged)
    {
        border.enabled = isSpeedChanged;
        timer.enabled = isSpeedChanged;
    }

    private void UpdateTimer(float timeLeft, bool isSpedUp)
    {
        timer.text = $"{timeLeft:F1}s";

        if (isSpedUp)
        {
            border.color = speedUpColor;
            timer.color = speedUpColor;
        }
        else
        {
            border.color = speedDownColor;
            timer.color = speedDownColor;
        }
    }

    private void OnDestroy()
    {
        speedManager.OnGameSpeedChanged -= DisplayBorderEffect;
        speedManager.OnGameSpeedModified -= UpdateTimer;
    }
}
