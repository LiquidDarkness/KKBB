using TMPro;
using UnityEngine;

// The same warm halo the cat wears in gameplay, put around a line of text. BallHighlight draws hers
// as a soft disc behind a SpriteRenderer, which is not a thing that exists under a Canvas - so this
// asks the font's own shader for it instead, and the letters glow rather than a shape behind them.
//
// The material is instanced the moment it is asked for, so nothing else drawn in this font is
// touched: one glowing button does not light up every other word in the menu.
[RequireComponent(typeof(TMP_Text))]
public class TextGlow : MonoBehaviour
{
    [Tooltip("Colour of the halo. The default is the one BallHighlight gives the cat, so the two read as the same effect. Alpha is what decides how loud it is.")]
    public Color glow = new Color(1f, 0.95f, 0.45f, 0.72f);

    [Tooltip("How strongly it burns. 0 is nothing at all, 1 is as much as the shader will give.")]
    [Range(0f, 1f)]
    public float power = 0.75f;

    [Tooltip("How far out from the letters it reaches.")]
    [Range(0f, 1f)]
    public float spread = 0.45f;

    [Tooltip("How far the halo bleeds inwards over the letters themselves. Usually best left at nothing.")]
    [Range(0f, 1f)]
    public float inwards;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Apply();
        }
    }

    [ContextMenu(nameof(Apply))]
    public void Apply()
    {
        TMP_Text text = GetComponent<TMP_Text>();

        if (text == null)
        {
            return;
        }

        // fontMaterial, not fontSharedMaterial: the shared one belongs to the font asset and every
        // text drawn in it.
        Material material = text.fontMaterial;

        if (material == null || !material.HasProperty(ShaderUtilities.ID_GlowColor))
        {
            // A font drawn with something other than the distance field shader has no glow to ask
            // for, and quietly doing nothing would look like the component was not working.
            Debug.LogWarning($"[{nameof(TextGlow)}] {name} is drawn with a material that has no glow in it - the shader has to be one of the TextMeshPro distance field ones.", this);
            return;
        }

        material.EnableKeyword(ShaderUtilities.Keyword_Glow);
        material.SetColor(ShaderUtilities.ID_GlowColor, glow);
        material.SetFloat(ShaderUtilities.ID_GlowPower, power);
        material.SetFloat(ShaderUtilities.ID_GlowOuter, spread);
        material.SetFloat(ShaderUtilities.ID_GlowInner, inwards);
        material.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
    }
}
