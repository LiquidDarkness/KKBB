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
    [SerializeField] ParticleFedder particleFedderDown;
    [SerializeField] ParticleFedder particleFedderUp;

    [Tooltip("Optional. How far the border has been turned down in the options: 0 leaves it as designed, 1 takes it off the screen altogether. Held as a reduction rather than as an intensity because a fresh install has nothing saved yet, and the zero PlayerPrefs answers with then has to mean \"as designed\" - the same reasoning behind backgroundDim and flameDim.")]
    public TypeDistinguisher dimSetting;

    // What the effect currently reads, kept so that turning the slider while a drop is running
    // redraws what is on screen instead of waiting for the next frame of the countdown.
    private bool effectShowing;
    private bool showingSpedUp;

    private void Awake()
    {
        DisplayBorderEffect(false);
        speedManager.OnGameSpeedChanged += DisplayBorderEffect;
        speedManager.OnGameSpeedModified += UpdateTimer;

        if (dimSetting != null)
        {
            dimSetting.OnValueChanged += Reapply;
        }
    }

    // 1 is the border as designed, 0 is no border at all, and everything between is the player
    // asking for less of it.
    private float Presence => 1f - Mathf.Clamp01(dimSetting != null ? dimSetting.FloatValue : 0f);

    private void DisplayBorderEffect(bool isSpeedChanged)
    {
        effectShowing = isSpeedChanged;

        // Turned the whole way down is off, not faded to nothing: no border drawn, no countdown
        // read out, and no particles left to simulate.
        bool shown = isSpeedChanged && Presence > 0f;

        border.enabled = shown;
        timer.enabled = shown;
        particleFedderDown.particleSystem.gameObject.SetActive(shown);
        particleFedderUp.particleSystem.gameObject.SetActive(shown);
    }

    private void UpdateTimer(float timeLeft, bool isSpedUp)
    {
        showingSpedUp = isSpedUp;
        timer.text = $"{timeLeft:F1}s";

        Color colour = Faded(isSpedUp ? speedUpColor : speedDownColor);
        border.color = colour;
        timer.color = colour;

        if (isSpedUp)
        {
            particleFedderUp.Play();
        }
        else
        {
            particleFedderDown.Play();
        }
    }

    // Only the alpha is touched. A border turned down is fainter, not a different colour, and the
    // countdown has to stay readable against whatever the level is drawing behind it.
    private Color Faded(Color colour)
    {
        return new Color(colour.r, colour.g, colour.b, colour.a * Presence);
    }

    private void Reapply()
    {
        DisplayBorderEffect(effectShowing);

        if (effectShowing)
        {
            Color colour = Faded(showingSpedUp ? speedUpColor : speedDownColor);
            border.color = colour;
            timer.color = colour;
        }
    }

    private void OnDestroy()
    {
        speedManager.OnGameSpeedChanged -= DisplayBorderEffect;
        speedManager.OnGameSpeedModified -= UpdateTimer;

        // The setting is a ScriptableObject and outlives every scene, so a handler left behind here
        // would be called on a destroyed component for the rest of the run.
        if (dimSetting != null)
        {
            dimSetting.OnValueChanged -= Reapply;
        }
    }
}
