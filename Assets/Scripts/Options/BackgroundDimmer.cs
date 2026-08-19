using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Darkens whatever is drawn under this object by the player's background-dim setting, so the things
// that matter - the blocks, the cat, the story text - stand off their backdrop. The osu! idea: the
// art stays where it is, it just stops competing with the foreground.
//
// The colour each renderer was authored with is remembered the first time it is seen and never
// overwritten, so the dim is always applied to the original and repeated changes cannot compound.
// Only the colour is touched, never the alpha: a dimmed background is darker, not see-through.
[DisallowMultipleComponent]
public class BackgroundDimmer : MonoBehaviour
{
    public TypeDistinguisher dimSetting;

    [Tooltip("The strongest dim the setting can ask for, and the top of the options slider. Short of 1 on purpose - fully black would throw the art away rather than quiet it down.")]
    public float maxDim = 0.8f;

    [Tooltip("Renderers that are switched off when this object wakes up are picked up too.")]
    public bool includeInactive = true;

    private struct Tinted
    {
        public Graphic Graphic;
        public SpriteRenderer Sprite;
        public Color Original;
    }

    private readonly Dictionary<int, Tinted> baselines = new Dictionary<int, Tinted>();

    private void OnEnable()
    {
        if (dimSetting != null)
        {
            dimSetting.OnValueChanged += Apply;
        }

        Rescan();
    }

    private void OnDisable()
    {
        // The setting is a ScriptableObject and outlives every scene, so a handler left behind here
        // would be called on a destroyed component for the rest of the run.
        if (dimSetting != null)
        {
            dimSetting.OnValueChanged -= Apply;
        }
    }

    // Call after building background art at runtime; anything new is recorded and the dim reapplied.
    [ContextMenu(nameof(Rescan))]
    public void Rescan()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(includeInactive))
        {
            Remember(graphic.GetInstanceID(), new Tinted { Graphic = graphic, Original = graphic.color });
        }

        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(includeInactive))
        {
            Remember(sprite.GetInstanceID(), new Tinted { Sprite = sprite, Original = sprite.color });
        }

        Apply();
    }

    private void Remember(int id, Tinted entry)
    {
        if (!baselines.ContainsKey(id))
        {
            baselines[id] = entry;
        }
    }

    public float CurrentDim
    {
        get
        {
            float stored = dimSetting != null ? dimSetting.FloatValue : 0f;
            return Mathf.Clamp(stored, 0f, maxDim);
        }
    }

    private void Apply()
    {
        float lit = 1f - CurrentDim;
        List<int> gone = null;

        foreach (KeyValuePair<int, Tinted> entry in baselines)
        {
            Tinted tinted = entry.Value;
            Color original = tinted.Original;
            var dimmed = new Color(original.r * lit, original.g * lit, original.b * lit, original.a);

            if (tinted.Graphic != null)
            {
                tinted.Graphic.color = dimmed;
            }
            else if (tinted.Sprite != null)
            {
                tinted.Sprite.color = dimmed;
            }
            else
            {
                (gone ??= new List<int>()).Add(entry.Key);
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
