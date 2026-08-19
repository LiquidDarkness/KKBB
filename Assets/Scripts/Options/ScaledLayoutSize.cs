using UnityEngine;
using UnityEngine.UI;

// Grows a control's layout column with the text-size setting, so a row scales as a whole instead of
// the label ballooning past a slider and a readout that stayed the size they were drawn at.
//
// Sits next to FontScaler and reads the same setting, so text and controls can never drift apart.
[RequireComponent(typeof(LayoutElement))]
public class ScaledLayoutSize : MonoBehaviour
{
    public TypeDistinguisher fontScaleSetting;

    [Tooltip("Size at scale 1. A value of 0 or less leaves that axis to the layout system.")]
    public float baseWidth = -1f;
    public float baseHeight = -1f;

    [Tooltip("Used while the setting is unset - PlayerPrefs answers 0 for a key nobody has written.")]
    public float defaultScale = 1f;

    public float minScale = 0.6f;
    public float maxScale = 2f;

    [Tooltip("Hard ceiling on the column, as a share of the row's width. This is what stops a scaled-up control from eating the row and squeezing the label down to one letter per line. 0 turns the ceiling off.")]
    [Range(0f, 1f)]
    public float maxFractionOfRow = 0f;

    private LayoutElement element;

    private void OnEnable()
    {
        if (fontScaleSetting != null)
        {
            fontScaleSetting.OnValueChanged += Apply;
        }

        Apply();
    }

    private void OnDisable()
    {
        // The setting outlives every scene, so a handler left here would be called on a destroyed
        // component for the rest of the run.
        if (fontScaleSetting != null)
        {
            fontScaleSetting.OnValueChanged -= Apply;
        }
    }

    // The row resizing is the other half of the answer: the ceiling is a share of the row, so it
    // has to be recomputed whenever the row changes shape, not only when the setting changes.
    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (element == null)
        {
            element = GetComponent<LayoutElement>();
        }

        float stored = fontScaleSetting != null ? fontScaleSetting.FloatValue : 0f;
        float scale = stored > 0f ? Mathf.Clamp(stored, minScale, maxScale) : defaultScale;

        if (baseWidth > 0f)
        {
            float width = baseWidth * scale;
            var row = transform.parent as RectTransform;

            // A row of zero width means layout has not run yet; the ceiling would be nonsense, so
            // the plain scaled size stands until the next call.
            if (maxFractionOfRow > 0f && row != null && row.rect.width > 1f)
            {
                width = Mathf.Min(width, row.rect.width * maxFractionOfRow);
            }

            // Assigned only when it actually moves. Writing the same number back would dirty the
            // layout every frame this component is asked to recompute.
            if (!Mathf.Approximately(element.preferredWidth, width))
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }
        }

        if (baseHeight > 0f && !Mathf.Approximately(element.preferredHeight, baseHeight * scale))
        {
            element.preferredHeight = baseHeight * scale;
        }
    }
}
