using UnityEngine;
using UnityEngine.UI;

public class SliderSetter : MonoBehaviour
{
    public Slider slider;
    public TypeDistinguisher typeDistinguisher;

    public void OnEnable()
    {
        slider.SetValueWithoutNotify(typeDistinguisher.FloatValue);
    }
}