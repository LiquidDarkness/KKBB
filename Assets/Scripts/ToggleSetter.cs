using UnityEngine;
using UnityEngine.UI;

public class ToggleSetter : MonoBehaviour
{
    public Toggle toggle;
    public TypeDistinguisher typeDistinguisher;

    [Tooltip("Optional. While this one is on, the toggle shows off and cannot be clicked - it is what the master Reduce motion switch overrides, and a switch that overrides another has to look like it.")]
    public TypeDistinguisher overrideOff;

    public void OnEnable()
    {
        if (overrideOff != null)
        {
            overrideOff.OnValueChanged += Refresh;
        }

        Refresh();
    }

    public void OnDisable()
    {
        // The setting is a ScriptableObject and outlives every scene, so a handler left behind here
        // would be called on a destroyed component for the rest of the run.
        if (overrideOff != null)
        {
            overrideOff.OnValueChanged -= Refresh;
        }
    }

    // The stored value is never touched: switching the master off has to give the player back
    // exactly the toggles they had set, so the override only decides what is shown and whether the
    // control answers to a click.
    private void Refresh()
    {
        bool overridden = overrideOff != null && overrideOff.BoolValue;

        toggle.SetIsOnWithoutNotify(typeDistinguisher.BoolValue && !overridden);
        toggle.interactable = !overridden;
    }
}
