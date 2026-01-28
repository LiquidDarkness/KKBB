using UnityEngine;
using UnityEngine.UI;

public class ToggleSetter : MonoBehaviour
{
    public Toggle toggle;
    public TypeDistinguisher typeDistinguisher;

    public void OnEnable()
    {
        toggle.SetIsOnWithoutNotify(typeDistinguisher.BoolValue);
    }
}
