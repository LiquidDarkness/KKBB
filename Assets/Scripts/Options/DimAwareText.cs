using TMPro;
using UnityEngine;

// Lightens a text as the background behind it is dimmed. The story is written in a dark purple that
// reads beautifully on the artwork and disappears entirely once the backdrop behind it is taken down
// to near black - dark on dark, which is the opposite of what the dim setting is for.
//
// The authored colour is what stands at dim 0, so nothing changes for a player who never touches the
// slider.
[DisallowMultipleComponent]
public class DimAwareText : MonoBehaviour
{
    public TypeDistinguisher dimSetting;

    [Tooltip("Left empty, the text on this object is used.")]
    public TMP_Text target;

    [Tooltip("The dim the slider tops out at. The lift is measured against this, so a full dim means a full lift.")]
    public float maxDim = 0.9f;

    [Tooltip("How much of the dim is answered by lightening the text. 1 follows it all the way to white; lower keeps more of the authored colour.")]
    [Range(0f, 1f)]
    public float response = 0.9f;

    private Color authored;
    private bool remembered;

    private void Awake()
    {
        Remember();
    }

    private void OnEnable()
    {
        Remember();

        if (dimSetting != null)
        {
            dimSetting.OnValueChanged += Apply;
        }

        Apply();
    }

    private void OnDisable()
    {
        // The setting outlives every scene, so a handler left behind here would be called on a
        // destroyed component for the rest of the run.
        if (dimSetting != null)
        {
            dimSetting.OnValueChanged -= Apply;
        }
    }

    // Read once and never again: reading it later would pick up a colour this component had already
    // lightened, and the text would creep towards white a little more on every scene load.
    private void Remember()
    {
        if (remembered)
        {
            return;
        }

        if (target == null)
        {
            target = GetComponent<TMP_Text>();
        }

        if (target == null)
        {
            Debug.LogWarning($"{nameof(DimAwareText)}: no text to lighten.", this);
            return;
        }

        authored = target.color;
        remembered = true;
    }

    private void Apply()
    {
        if (target == null || !remembered)
        {
            return;
        }

        float dim = dimSetting != null ? dimSetting.FloatValue : 0f;
        float lift = Mathf.Clamp01(dim / Mathf.Max(0.0001f, maxDim)) * response;
        target.color = Color.Lerp(authored, Color.white, lift);
    }
}
