using System.Collections;
using UnityEngine;

public class ComponentEnabler : MonoBehaviour
{
    public TypeDistinguisher typeDistinguisher;
    public MonoBehaviour target;

    private void Awake()
    {
        typeDistinguisher.OnValueChanged += Toggle;
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
    }

    private void Toggle()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(ComponentEnabler)}: Target has been destroyed or is missing.", this);
            return;
        }

        target.enabled = typeDistinguisher.BoolValue;
    }
}
