using TMPro;
using UnityEngine;

// The dropdown counterpart of ToggleSetter/SliderSetter: puts the stored choice back on screen
// every time the window opens, without firing onValueChanged and writing it straight back.
public class DropdownSetter : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TypeDistinguisher typeDistinguisher;

    public void OnEnable()
    {
        if (dropdown == null || typeDistinguisher == null)
        {
            return;
        }

        // An unset key answers 0, which is the first entry - the intended default for every
        // dropdown here. Clamping guards against a save file written when the list was longer.
        int stored = Mathf.Clamp(typeDistinguisher.IntValue, 0, Mathf.Max(0, dropdown.options.Count - 1));
        dropdown.SetValueWithoutNotify(stored);
        dropdown.RefreshShownValue();
    }
}
