using System.Collections;
using UnityEngine;

// A soft glow behind the cat, so she can be followed across a busy formation at speed. Optional,
// off by default, and deliberately not a trail: a trail is more motion on screen, which is the
// opposite of what a player who turns this on is usually asking for.
//
// The glow is drawn from a texture built at runtime rather than an art asset - nothing to draw,
// nothing to keep in sync with the cats, and it works for every avatar including ones added later.
public class BallHighlight : MonoBehaviour
{
    public TypeDistinguisher activeSetting;

    [Tooltip("Colour and strength of the glow. Alpha is what decides how loud it is.")]
    public Color glow = new Color(1f, 0.95f, 0.45f, 0.55f);

    [Tooltip("How far past the cat the glow reaches, as a multiple of her size.")]
    public float padding = 1.6f;

    private SpriteRenderer highlight;
    private static Sprite discSprite;

    private void OnEnable()
    {
        if (activeSetting != null)
        {
            activeSetting.OnValueChanged += Apply;
        }

        // A frame late: AvatarSwitcher switches the chosen cat's parts on in its own Start, and
        // measuring before that would size the glow to whichever parts happened to be on in the
        // prefab. A coroutine and not Invoke - this can run while the game is paused at timeScale 0.
        StartCoroutine(BuildNextFrame());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (activeSetting != null)
        {
            activeSetting.OnValueChanged -= Apply;
        }
    }

    private IEnumerator BuildNextFrame()
    {
        yield return null;
        Rebuild();
    }

    // Call after swapping the cat for a differently sized one.
    [ContextMenu(nameof(Rebuild))]
    public void Rebuild()
    {
        EnsureHighlight();
        Fit();
        Apply();
    }

    private void EnsureHighlight()
    {
        if (highlight != null)
        {
            return;
        }

        Transform existing = transform.Find("Highlight");

        if (existing != null)
        {
            highlight = existing.GetComponent<SpriteRenderer>();
            return;
        }

        var host = new GameObject("Highlight");
        host.transform.SetParent(transform, false);
        highlight = host.AddComponent<SpriteRenderer>();
        highlight.sprite = Disc();
    }

    // Sized and sorted from whatever is actually on screen, so a new avatar needs no wiring: the
    // glow takes the bounds of the cat's own renderers and sits one step behind the nearest of them.
    private void Fit()
    {
        Bounds? total = null;
        int lowestOrder = int.MaxValue;
        int layer = 0;

        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer == highlight || !renderer.enabled)
            {
                continue;
            }

            total = total.HasValue ? Encapsulated(total.Value, renderer.bounds) : renderer.bounds;

            if (renderer.sortingOrder < lowestOrder)
            {
                lowestOrder = renderer.sortingOrder;
                layer = renderer.sortingLayerID;
            }
        }

        if (!total.HasValue)
        {
            Debug.LogWarning($"{nameof(BallHighlight)}: nothing to measure - the glow was left as it was.", this);
            return;
        }

        Bounds bounds = total.Value;
        highlight.sortingLayerID = layer;
        highlight.sortingOrder = lowestOrder - 1;
        highlight.color = glow;

        // The disc is one world unit across at scale 1, but this object may be scaled by whatever
        // it is parented to, so the parent's scale is divided back out.
        Vector3 lossy = transform.lossyScale;
        highlight.transform.position = bounds.center;
        highlight.transform.localScale = new Vector3(
            bounds.size.x * padding / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            bounds.size.y * padding / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            1f);
    }

    private static Bounds Encapsulated(Bounds bounds, Bounds other)
    {
        bounds.Encapsulate(other);
        return bounds;
    }

    private void Apply()
    {
        if (highlight != null)
        {
            highlight.enabled = activeSetting != null && activeSetting.BoolValue;
        }
    }

    // A round gradient, transparent at the rim. Built once per run and shared by every ball.
    private static Sprite Disc()
    {
        if (discSprite != null)
        {
            return discSprite;
        }

        const int Size = 128;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[Size * Size];
        const float Middle = Size * 0.5f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(Middle, Middle)) / Middle;
                float strength = Mathf.Clamp01(1f - distance);
                strength *= strength;
                pixels[(y * Size) + x] = new Color32(255, 255, 255, (byte)(strength * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        discSprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
        return discSprite;
    }
}
