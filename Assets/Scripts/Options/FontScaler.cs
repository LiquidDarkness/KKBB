using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Scales every TMP text under this object by the player's font-size setting. Drop one on the root
// of anything whose text should follow the setting (the story panel, the options window) - it
// needs no other wiring.
//
// The size each text was authored at is remembered the first time that text is seen and never
// overwritten, so the scale is always applied to the original and repeated changes cannot drift.
[DisallowMultipleComponent]
public class FontScaler : MonoBehaviour
{
    public TypeDistinguisher fontScaleSetting;

    [Tooltip("Used while the setting is still unset - PlayerPrefs answers 0 for a key nobody has written yet, and 0 would make every text vanish.")]
    public float defaultScale = 1f;

    [Tooltip("Range the setting is clamped to, so a broken save file cannot produce unreadable text.")]
    public float minScale = 0.6f;
    public float maxScale = 2f;

    [Tooltip("Texts that are switched off when this object wakes up (dropdown item templates, panels that open later) are picked up too.")]
    public bool includeInactive = true;

    private struct BaseSize
    {
        public TMP_Text text;
        public float fontSize;
        public float autoSizeMin;
        public float autoSizeMax;
    }

    private readonly Dictionary<int, BaseSize> baselines = new Dictionary<int, BaseSize>();

    private void OnEnable()
    {
        if (fontScaleSetting != null)
        {
            fontScaleSetting.OnValueChanged += Apply;
        }

        Rescan();
    }

    private void OnDisable()
    {
        // The setting is a ScriptableObject and outlives every scene, so a handler left behind here
        // would be called on a destroyed component for the rest of the run.
        if (fontScaleSetting != null)
        {
            fontScaleSetting.OnValueChanged -= Apply;
        }
    }

    // Call after building text at runtime; new texts are baselined and everything is re-applied.
    [ContextMenu(nameof(Rescan))]
    public void Rescan()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(includeInactive))
        {
            int id = text.GetInstanceID();

            if (baselines.ContainsKey(id))
            {
                continue;
            }

            baselines[id] = new BaseSize
            {
                text = text,
                fontSize = text.fontSize,
                autoSizeMin = text.fontSizeMin,
                autoSizeMax = text.fontSizeMax,
            };
        }

        Apply();
    }

    public float CurrentScale
    {
        get
        {
            float stored = fontScaleSetting != null ? fontScaleSetting.FloatValue : 0f;
            return stored > 0f ? Mathf.Clamp(stored, minScale, maxScale) : defaultScale;
        }
    }

    private void Apply()
    {
        float scale = CurrentScale;
        List<int> gone = null;

        foreach (KeyValuePair<int, BaseSize> entry in baselines)
        {
            BaseSize baseline = entry.Value;

            if (baseline.text == null)
            {
                (gone ??= new List<int>()).Add(entry.Key);
                continue;
            }

            // An auto-sizing text ignores fontSize entirely - it picks its own between the two
            // bounds - so for those the bounds are what has to move.
            if (baseline.text.enableAutoSizing)
            {
                baseline.text.fontSizeMin = baseline.autoSizeMin * scale;
                baseline.text.fontSizeMax = baseline.autoSizeMax * scale;
            }
            else
            {
                baseline.text.fontSize = baseline.fontSize * scale;
            }
        }

        if (gone == null)
        {
            return;
        }

        foreach (int id in gone)
        {
            baselines.Remove(id);
        }
    }
}
