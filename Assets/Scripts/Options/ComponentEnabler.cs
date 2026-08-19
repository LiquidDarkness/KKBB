using System.Collections;
using UnityEngine;

public class ComponentEnabler : MonoBehaviour
{
    public TypeDistinguisher typeDistinguisher;
    public MonoBehaviour target;

    [Tooltip("Optional. While this one is on the target stays off, whatever the setting above says - it is how a single Reduce motion switch turns every animation off at once without disturbing the individual toggles.")]
    public TypeDistinguisher overrideOff;

    private void Awake()
    {
        typeDistinguisher.OnValueChanged += Toggle;

        if (overrideOff != null)
        {
            overrideOff.OnValueChanged += Toggle;
        }
    }

    private IEnumerator Start()
    {
        yield return null;
        Toggle();
    }

    private void OnDestroy()
    {
        // Ważne — odsubskrybowanie eventu, by uniknąć wywołań po zniszczeniu
        if (typeDistinguisher != null)
            typeDistinguisher.OnValueChanged -= Toggle;

        if (overrideOff != null)
            overrideOff.OnValueChanged -= Toggle;
    }

    private void Toggle()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(ComponentEnabler)}: Target has been destroyed or is missing.", this);
            return;
        }

        target.enabled = typeDistinguisher.BoolValue && !(overrideOff != null && overrideOff.BoolValue);
    }
}
