using UnityEngine;
using UnityEngine.UI;

// A red pulse around the edges of the screen when the cat is lost. Losing a life used to say
// nothing at all: the cat simply was not there any more, and a player watching the blocks rather
// than the hearts found out two rallies later.
//
// Everything it draws is built at runtime out of a generated texture and Sprites/Default, the same
// way MetalAmbienceFX builds its glow - no art to keep in sync, and nothing that can be stripped
// out of a build. Edges only: the middle of the playfield stays clear, so the thing that just went
// wrong is not hidden behind the notice that it did.
//
// Lives on GameSession, which outlives every scene load and is where PlayerHealth's signals are
// already listened to.
[DisallowMultipleComponent]
public class LifeLostFlash : MonoBehaviour
{
    [Tooltip("Optional. With Reduce motion on the flash is held steady and cut, rather than swelling and fading.")]
    public TypeDistinguisher reduceMotion;

    [Tooltip("Optional. The player's own switch. A red pulse across the edges of the screen is exactly the shape of thing photosensitive epilepsy answers to, so it has to be possible to say no to it - and the heart leaving the row says the same thing without any of that.")]
    public TypeDistinguisher enabledSetting;

    public Color colour = new Color(0.85f, 0.05f, 0.05f, 1f);

    [Range(0f, 1f)] public float strength = 0.55f;

    [Tooltip("How long the pulse takes from nothing to its strongest.")]
    public float riseSeconds = 0.06f;

    [Tooltip("How long it takes to fade back out again.")]
    public float fallSeconds = 0.45f;

    [Tooltip("Sorting order of the flash's own canvas. Above the HUD (1330) so it is not hidden behind the hearts, below the loading screen (32757) so a fade to black still covers it.")]
    public int sortingOrder = 1400;

    private Canvas canvas;
    private RawImage vignette;
    private Texture2D texture;
    private float remaining;
    private float total;

    private void OnEnable()
    {
        PlayerHealth.OnHealthLost += Flash;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthLost -= Flash;
        remaining = 0f;

        if (canvas != null)
        {
            canvas.enabled = false;
        }
    }

    private void OnDestroy()
    {
        // Made in code, so it belongs to no scene and nothing else will ever collect it.
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(texture);
        }
        else
        {
            DestroyImmediate(texture);
        }
    }

    public void Flash()
    {
        // Switched off in the options: nothing is built, so there is not even a canvas left behind.
        if (enabledSetting != null && !enabledSetting.BoolValue)
        {
            return;
        }

        Build();

        total = MotionAllowed ? riseSeconds + fallSeconds : fallSeconds;
        remaining = total;
        canvas.enabled = true;
        Draw();
    }

    // Unscaled throughout: Time.timeScale is the pace of play here - METAL runs the whole scene at
    // eight - and a warning that lasts a different length of time on every difficulty is no warning.
    private void Update()
    {
        if (remaining <= 0f)
        {
            return;
        }

        remaining -= Time.unscaledDeltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            canvas.enabled = false;
            return;
        }

        Draw();
    }

    private void Draw()
    {
        vignette.color = new Color(colour.r, colour.g, colour.b, strength * CurrentShare());
    }

    // 0 to 1 and back down again while the pulse runs. With Reduce motion on there is no swell at
    // all - it comes up at once, holds, and is taken away.
    private float CurrentShare()
    {
        if (!MotionAllowed)
        {
            return 1f;
        }

        float elapsed = total - remaining;

        if (elapsed < riseSeconds && riseSeconds > 0f)
        {
            return Mathf.Clamp01(elapsed / riseSeconds);
        }

        return fallSeconds > 0f ? Mathf.Clamp01(remaining / fallSeconds) : 0f;
    }

    private bool MotionAllowed => reduceMotion == null || !reduceMotion.BoolValue;

    private void Build()
    {
        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("Life Lost Flash", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.layer = LayerMask.NameToLayer("UI");

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var imageObject = new GameObject("Vignette", typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        imageObject.layer = canvasObject.layer;

        texture = BuildVignetteTexture();

        vignette = imageObject.GetComponent<RawImage>();
        vignette.texture = texture;
        // Nothing here may ever swallow a click meant for the shop underneath it.
        vignette.raycastTarget = false;

        RectTransform rect = vignette.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Solid around the rim, clear over the middle, with nothing like a hard edge in between.
    private static Texture2D BuildVignetteTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float dy = ((y + 0.5f) / size - 0.5f) * 2f;

            for (int x = 0; x < size; x++)
            {
                float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                // Squashed sideways, so a wide screen keeps the clear middle wide as well.
                float radius = Mathf.Sqrt(dx * dx * 0.72f + dy * dy);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.05f, radius));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(edge) * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
