using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InteractableEnabler : MonoBehaviour
{
    public TypeDistinguisher typeDistinguisher;
    public Selectable target;

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
        // Unsubscribe to avoid calls after this object is destroyed.
        if (typeDistinguisher != null)
            typeDistinguisher.OnValueChanged -= Toggle;
    }

    private void Toggle()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(InteractableEnabler)}: Target has been destroyed or is missing.", this);
            return;
        }

        target.interactable = typeDistinguisher.BoolValue;
    }
}
