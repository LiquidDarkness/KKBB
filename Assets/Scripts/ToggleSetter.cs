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
        Refresh();
    }

    // Polled rather than driven by the setting's event. The override is set on a different tab from
    // the toggles it covers, and a tab that is not on screen is switched off - so its toggles are
    // disabled, unsubscribed, and miss the event entirely. Waiting for OnEnable to catch up left
    // them showing on while the effect they name was already off.
    //
    // Nothing is written unless it actually changed, so this costs a comparison per toggle per
    // frame and never touches the layout.
    private void Update()
    {
        Refresh();
    }

    // The stored value is never touched: switching the master off has to hand back exactly the
    // toggles the player had set, so the override decides what is drawn, not what is saved.
    private void Refresh()
    {
        if (toggle == null || typeDistinguisher == null)
        {
            return;
        }

        bool overridden = overrideOff != null && overrideOff.BoolValue;
        bool shouldBeOn = typeDistinguisher.BoolValue && !overridden;

        if (toggle.isOn != shouldBeOn)
        {
            toggle.SetIsOnWithoutNotify(shouldBeOn);
        }

        if (toggle.interactable == overridden)
        {
            toggle.interactable = !overridden;
        }
    }
}
