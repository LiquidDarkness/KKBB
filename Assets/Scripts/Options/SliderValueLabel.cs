using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Prints a slider's value beside it, so a setting reads as "20 s" instead of "somewhere past the
// middle". Polled rather than driven by onValueChanged: SliderSetter restores the stored value in
// its own OnEnable without firing the event, and nothing guarantees which of the two runs first.
public class SliderValueLabel : MonoBehaviour
{
    public Slider slider;
    public TMP_Text label;

    [Tooltip("Value is multiplied by this before it is printed - 100 turns 1.15 into 115 for a percentage.")]
    public float multiplier = 1f;

    [Tooltip("Standard .NET numeric format: 0 for whole numbers, 0.00 for two decimals.")]
    public string format = "0";

    [Tooltip("Printed straight after the number, space included if one is wanted.")]
    public string suffix = "";

    private float lastValue = float.NaN;

    private void OnEnable()
    {
        lastValue = float.NaN;
    }

    private void Update()
    {
        if (slider == null || label == null || Mathf.Approximately(lastValue, slider.value))
        {
            return;
        }

        lastValue = slider.value;

        // Invariant culture on purpose: this is a number on screen next to a slider, and a decimal
        // comma on one machine and a dot on another is noise, not localisation.
        label.text = (lastValue * multiplier).ToString(format, CultureInfo.InvariantCulture) + suffix;
    }
}
