using UnityEngine;
using UnityEngine.UI;

public class SliderSetter : MonoBehaviour
{
    public Slider slider;
    public TypeDistinguisher typeDistinguisher;

    [Tooltip("Optional. While this one is on the slider reads overriddenValue and cannot be dragged - the slider counterpart of overrideOff on ToggleSetter, so one master Reduce motion switch can speak for sliders as well as toggles.")]
    public TypeDistinguisher overrideOff;

    [Tooltip("What the slider reads while overrideOff is on: the end that means the effect is not happening. On a slider that turns something down that is its far end - 1 on Dim the flames, where the far end is the fire gone.")]
    public float overriddenValue = 1f;

    private bool wasOverridden;

    public void OnEnable()
    {
        slider.SetValueWithoutNotify(typeDistinguisher.FloatValue);
        wasOverridden = false;
        Refresh();
    }

    // Polled rather than driven by the setting's event, for the same reason ToggleSetter polls: the
    // override sits on a different tab from the sliders it covers, and a tab that is not on screen
    // is switched off - so it misses the event entirely and comes back still showing a value that
    // is not in effect.
    //
    // Nothing is written unless it actually changed, so this costs a comparison per slider per
    // frame and never touches the layout.
    private void Update()
    {
        Refresh();
    }

    // The stored value is never touched: switching the master off has to hand back exactly the
    // setting the player had, so the override decides what is drawn, not what is saved.
    private void Refresh()
    {
        if (slider == null || typeDistinguisher == null)
        {
            return;
        }

        bool overridden = overrideOff != null && overrideOff.BoolValue;

        if (overridden)
        {
            if (!Mathf.Approximately(slider.value, overriddenValue))
            {
                slider.SetValueWithoutNotify(overriddenValue);
            }
        }
        else if (wasOverridden)
        {
            // On the way back the slider is still wearing the override, so the saved setting has to
            // be put back on it - and only then, because reading the store every frame would fight
            // the player halfway through a drag.
            slider.SetValueWithoutNotify(typeDistinguisher.FloatValue);
        }

        if (slider.interactable == overridden)
        {
            slider.interactable = !overridden;
        }

        wasOverridden = overridden;
    }
}
