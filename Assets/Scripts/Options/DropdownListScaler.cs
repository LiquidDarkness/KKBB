using UnityEngine;

// Keeps a dropdown's open list readable. TMP_Dropdown lays the list out from the height of its
// template item, and that height is authored once - so raising the entry font (or the text-size
// setting) puts 25pt text into a 20px row and the list comes out looking blank.
//
// Both the row height and the list height are driven from the same setting FontScaler reads, so
// the entries can never outgrow the rows they sit in.
public class DropdownListScaler : MonoBehaviour
{
    public TypeDistinguisher fontScaleSetting;

    [Tooltip("The template's single item row - Template/Viewport/Content/Item.")]
    public RectTransform item;

    [Tooltip("The template itself; its height is how much of the list is on screen at once.")]
    public RectTransform list;

    public float baseItemHeight = 44f;

    [Tooltip("How many entries the list shows before it starts scrolling. Always a whole number of them - a list sized to three and a half entries cuts the last one in half against the mask.")]
    public int visibleItems = 3;

    [Tooltip("The list is never allowed past this, whatever the text size. It is drawn inside the options window's mask, so a list taller than the window is not scrollable - it is simply cut off.")]
    public float maxListHeight = 220f;

    public float defaultScale = 1f;
    public float minScale = 0.6f;
    public float maxScale = 1.5f;

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
        if (fontScaleSetting != null)
        {
            fontScaleSetting.OnValueChanged -= Apply;
        }
    }

    private void Apply()
    {
        float stored = fontScaleSetting != null ? fontScaleSetting.FloatValue : 0f;
        float scale = stored > 0f ? Mathf.Clamp(stored, minScale, maxScale) : defaultScale;

        // The template is switched off while the list is closed, which is fine: a RectTransform can
        // be resized whether or not its object is active, and TMP_Dropdown reads these sizes when
        // it builds the list on opening.
        if (item != null)
        {
            item.sizeDelta = new Vector2(item.sizeDelta.x, baseItemHeight * scale);
        }

        if (list != null)
        {
            float entry = baseItemHeight * scale;

            // Fewer entries at a larger text size rather than a taller list: the list is drawn
            // inside the options window's mask, and anything past the window is cut off, not
            // scrollable. Whole entries either way.
            int visible = Mathf.Max(1, Mathf.Min(visibleItems, Mathf.FloorToInt(maxListHeight / Mathf.Max(1f, entry))));
            list.sizeDelta = new Vector2(list.sizeDelta.x, entry * visible);
        }
    }
}
