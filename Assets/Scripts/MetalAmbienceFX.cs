using UnityEngine;
using UnityEngine.UI;

// Fire and brimstone for METAL. That difficulty hands out zero spare lives and the fastest ball in
// the game; this is the screen saying so before the first block is hit - a red glow eating into the
// edges of the playfield and a wall of flames licking up from the bottom of the view.
//
// Everything it draws is built at runtime out of generated textures and Sprites/Default, which sits
// in Always Included Shaders, so none of it can be stripped out of a build. No art, no material and
// no prefab to keep in sync: drop the component into the scene, point it at the difficulties it
// plays on, and it looks after itself.
[DisallowMultipleComponent]
public class MetalAmbienceFX : MonoBehaviour
{
    [Header("When it plays")]
    public DiffcultyManager diffcultyManager;

    [Tooltip("Difficulties this ambience plays on. Listed rather than matched by name, so renaming an asset cannot quietly turn it off.")]
    public DifficultySettings[] playsOn;

    [Tooltip("Optional. While Reduce motion is on the flames stay away and the glow stops breathing - it settles at its dimmest, so the difficulty still reads without a thing moving on screen.")]
    public TypeDistinguisher reduceMotion;

    [Tooltip("Optional. How far the fire has been turned down in the options: 0 leaves it as designed, 1 takes it off the screen altogether. Held as a reduction rather than as an intensity because a fresh install has nothing saved yet, and the zero it reads then has to be the answer that means \"as designed\" - the same reasoning behind backgroundDim.")]
    public TypeDistinguisher dimSetting;

    [Tooltip("Optional. Falls back to Camera.main, which is what the Gameplay scene has.")]
    public Camera view;

    [Header("Red glow")]
    public Color glowColour = new Color(0.72f, 0.06f, 0.03f, 1f);

    [Range(0f, 1f)] public float glowStrength = 0.5f;

    [Tooltip("How far the glow swings either side of its strength as it breathes. 0 holds it still.")]
    [Range(0f, 1f)] public float glowPulse = 0.35f;

    public float glowPulseSpeed = 0.55f;

    [Tooltip("Sorting order of the glow's own canvas. Under the HUD canvas (1330) and the loading screen (32757) on purpose: hearts, score, the shop and the fade to black must not come out red.")]
    public int glowSortingOrder = 1000;

    [Header("Flames")]
    [Tooltip("Height of the flame wall as a fraction of what the camera sees.")]
    [Range(0.05f, 0.6f)] public float flameHeight = 0.22f;

    [Tooltip("Flames per world unit of screen width, per second.")]
    [Range(0f, 12f)] public float flameDensity = 3.5f;

    [Range(0f, 1f)] public float emberDensity = 0.35f;

    [Tooltip("Flames draw on this sorting layer above every block and the cat. Left blank they land on Default, under everything drawn on Story.")]
    public string flameSortingLayer = "Story";

    public int flameSortingOrder = 50;

    private Canvas glowCanvas;
    private RawImage glow;
    private ParticleSystem flames;
    private ParticleSystem embers;
    private Texture2D flameTexture;
    private Texture2D emberTexture;
    private Texture2D glowTexture;
    private Material flameMaterial;
    private Material emberMaterial;
    private bool isPlaying;
    private float builtForSize;
    private float builtForAspect;

    private void OnEnable()
    {
        DiffcultyManager.OnSettingsChanged += HandleDifficultyChanged;

        // The difficulty is read out of PlayerPrefs, and on the first gameplay load of a run
        // SaveManager.Load - which is what fills those prefs in - only runs once the scene is up.
        // Start() on its own would read whatever the previous run happened to leave behind.
        SceneLoader.OnGameplayLoaded += Refresh;
    }

    private void OnDisable()
    {
        DiffcultyManager.OnSettingsChanged -= HandleDifficultyChanged;
        SceneLoader.OnGameplayLoaded -= Refresh;
        Stop();
    }

    private void Start()
    {
        if (reduceMotion != null)
        {
            reduceMotion.OnValueChanged += Refresh;
        }

        if (dimSetting != null)
        {
            dimSetting.OnValueChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (reduceMotion != null)
        {
            reduceMotion.OnValueChanged -= Refresh;
        }

        if (dimSetting != null)
        {
            dimSetting.OnValueChanged -= Refresh;
        }

        // Textures and materials made in code belong to no scene, so nothing else will ever collect
        // them.
        Discard(flameTexture);
        Discard(emberTexture);
        Discard(glowTexture);
        Discard(flameMaterial);
        Discard(emberMaterial);
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        Camera camera = ResolveCamera();
        if (camera != null && flames != null
            && (!Mathf.Approximately(camera.orthographicSize, builtForSize) || !Mathf.Approximately(camera.aspect, builtForAspect)))
        {
            // A resolution change - the options screen offers a few - moves the bottom edge of the
            // view, and flames left where they were end up burning in mid-air.
            LayOutFlames(camera);
        }

        glow.color = new Color(glowColour.r, glowColour.g, glowColour.b, CurrentGlowAlpha());
    }

    [ContextMenu(nameof(Refresh))]
    public void Refresh()
    {
        if (ShouldPlay())
        {
            Play();
        }
        else
        {
            Stop();
        }
    }

    private void HandleDifficultyChanged(DifficultySettings _)
    {
        Refresh();
    }

    private bool ShouldPlay()
    {
        if (!isActiveAndEnabled || diffcultyManager == null)
        {
            return false;
        }

        if (FirePresence <= 0f)
        {
            // Turned the whole way down in the options: not dimmed to nothing but off, so there is
            // no canvas left to draw and no particles left to simulate.
            return false;
        }

        if (playsOn == null || playsOn.Length == 0)
        {
            // An empty list means "no difficulty at all" rather than "every difficulty": a glow left
            // burning on Easy by accident is far worse than one missing from METAL.
            Debug.LogWarning($"[{nameof(MetalAmbienceFX)}] playsOn is empty - list METAL there or the ambience will never show.", this);
            return false;
        }

        DifficultySettings current = diffcultyManager.CurrentSettings;

        foreach (DifficultySettings candidate in playsOn)
        {
            if (candidate != null && candidate == current)
            {
                return true;
            }
        }

        return false;
    }

    private bool MotionAllowed => reduceMotion == null || !reduceMotion.BoolValue;

    // 1 is the fire as designed, 0 is no fire at all, and everything between is the player asking
    // for less of it.
    private float FirePresence => 1f - Mathf.Clamp01(dimSetting != null ? dimSetting.FloatValue : 0f);

    private void Play()
    {
        Camera camera = ResolveCamera();
        if (camera == null)
        {
            Debug.LogWarning($"[{nameof(MetalAmbienceFX)}] No camera to build the ambience around - assign one, or tag the scene camera MainCamera.", this);
            return;
        }

        BuildGlow();
        glowCanvas.enabled = true;
        glow.color = new Color(glowColour.r, glowColour.g, glowColour.b, CurrentGlowAlpha());

        if (MotionAllowed)
        {
            BuildFlames(camera);
            LayOutFlames(camera);
            flames.gameObject.SetActive(true);
            embers.gameObject.SetActive(true);
            flames.Play();
            embers.Play();
        }
        else if (flames != null)
        {
            flames.gameObject.SetActive(false);
            embers.gameObject.SetActive(false);
        }

        isPlaying = true;
    }

    private void Stop()
    {
        isPlaying = false;

        if (glowCanvas != null)
        {
            glowCanvas.enabled = false;
        }

        if (flames != null)
        {
            flames.gameObject.SetActive(false);
            embers.gameObject.SetActive(false);
        }
    }

    // The glow breathes on unscaled time on purpose. Time.timeScale is the difficulty game speed
    // here - METAL runs the whole scene at 8 - so anything on scaled time would flutter rather than
    // burn.
    private float CurrentGlowAlpha()
    {
        float strength = glowStrength * FirePresence;

        if (!MotionAllowed)
        {
            return Mathf.Clamp01(strength * (1f - glowPulse));
        }

        float time = Time.unscaledTime * glowPulseSpeed;
        float breath = Mathf.Sin(time * Mathf.PI * 2f) * 0.6f + Mathf.Sin(time * Mathf.PI * 5.3f) * 0.4f;
        return Mathf.Clamp01(strength * (1f + glowPulse * breath));
    }

    private Camera ResolveCamera()
    {
        return view != null ? view : Camera.main;
    }

    private void BuildGlow()
    {
        if (glowCanvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("METAL Glow", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.layer = LayerMask.NameToLayer("UI");

        glowCanvas = canvasObject.GetComponent<Canvas>();
        glowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        glowCanvas.sortingOrder = glowSortingOrder;

        var glowObject = new GameObject("Glow", typeof(RawImage));
        glowObject.transform.SetParent(canvasObject.transform, false);
        glowObject.layer = canvasObject.layer;

        glowTexture = BuildGlowTexture();

        glow = glowObject.GetComponent<RawImage>();
        glow.texture = glowTexture;
        // Nothing here may ever swallow a click meant for the shop underneath it.
        glow.raycastTarget = false;

        RectTransform rect = glow.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void BuildFlames(Camera camera)
    {
        if (flames != null)
        {
            return;
        }

        flameTexture = BuildFlameTexture();
        emberTexture = BuildEmberTexture();
        flameMaterial = BuildParticleMaterial(flameTexture);
        emberMaterial = BuildParticleMaterial(emberTexture);

        flames = BuildFlameSystem(camera);
        embers = BuildEmberSystem(camera);
    }

    private ParticleSystem BuildFlameSystem(Camera camera)
    {
        ParticleSystem system = NewSystem("METAL Flames", camera, flameMaterial, flameSortingOrder);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        main.maxParticles = 600;

        ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
        colour.enabled = true;
        colour.color = new ParticleSystem.MinMaxGradient(BuildFlameGradient());

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.15f)));

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 1.1f;
        noise.damping = true;

        return system;
    }

    private ParticleSystem BuildEmberSystem(Camera camera)
    {
        ParticleSystem system = NewSystem("METAL Embers", camera, emberMaterial, flameSortingOrder + 1);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.maxParticles = 200;

        ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
        colour.enabled = true;
        colour.color = new ParticleSystem.MinMaxGradient(BuildEmberGradient());

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)));

        // Embers wander far more than the flames they come off, which is what sells them as sparks
        // rather than more fire.
        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.frequency = 0.25f;
        noise.strength = 1.4f;
        noise.scrollSpeed = 0.6f;

        return system;
    }

    private ParticleSystem NewSystem(string name, Camera camera, Material material, int sortingOrder)
    {
        var systemObject = new GameObject(name);
        // Parented to the camera so the effect keeps sitting on the bottom edge of the view, but
        // simulated in world space so the particles themselves are never dragged along by it.
        systemObject.transform.SetParent(camera.transform, false);

        ParticleSystem system = systemObject.AddComponent<ParticleSystem>();
        system.Stop();

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;
        main.gravityModifier = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = Vector3.zero;

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = material;
        renderer.sortingOrder = sortingOrder;

        if (!string.IsNullOrEmpty(flameSortingLayer))
        {
            // Naming a layer that does not exist throws, and a mistyped layer must not be what
            // stops the whole scene from running.
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name == flameSortingLayer)
                {
                    renderer.sortingLayerName = flameSortingLayer;
                    break;
                }
            }
        }

        return system;
    }

    // Everything that depends on how much of the world the camera shows: how wide the wall of fire
    // stands, how tall its flames reach, and how many of them there are.
    private void LayOutFlames(Camera camera)
    {
        if (flames == null)
        {
            return;
        }

        builtForSize = camera.orthographicSize;
        builtForAspect = camera.aspect;

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        float width = halfWidth * 2f;
        float height = Mathf.Max(0.1f, halfHeight * 2f * flameHeight);

        // Seated a touch below the edge so flames grow into the frame rather than popping into
        // existence on it. Z drops them onto the plane the blocks and the cat live on.
        var position = new Vector3(0f, -halfHeight - height * 0.08f, -camera.transform.localPosition.z);

        LayOut(flames, position, width, height, flameDensity, 0.55f, 0.85f);
        LayOut(embers, position, width, height * 2.2f, flameDensity * emberDensity, 0.08f, 0.16f);
    }

    private void LayOut(ParticleSystem system, Vector3 position, float width, float height, float density, float minSizeShare, float maxSizeShare)
    {
        system.transform.localPosition = position;

        ParticleSystem.MainModule main = system.main;
        // Lifetime and speed together are what the height is made of: a particle living the average
        // lifetime reaches the top of the wall exactly.
        float averageLifetime = (main.startLifetime.constantMin + main.startLifetime.constantMax) * 0.5f;
        float speed = height / Mathf.Max(0.01f, averageLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.7f, speed * 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(height * minSizeShare, height * maxSizeShare);

        // Turning the fire down takes flames away and fades the ones left over. Thinning alone
        // leaves gaps a player still watches; fading alone leaves the same busy shape on screen.
        float presence = FirePresence;
        main.startColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.4f, 1f, presence));

        ParticleSystem.ShapeModule shape = system.shape;
        shape.scale = new Vector3(width, height * 0.12f, 0.01f);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = width * density * presence;

        ParticleSystem.NoiseModule noise = system.noise;
        if (noise.enabled && Mathf.Approximately(noise.strength.constant, 1f))
        {
            // Left at its default of 1 the flicker is barely visible at this scale. The embers set
            // a strength of their own and are left alone.
            noise.strength = height * 0.35f;
        }
    }

    private static Gradient BuildFlameGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.93f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.25f),
                new GradientColorKey(new Color(0.85f, 0.13f, 0.05f), 0.6f),
                new GradientColorKey(new Color(0.24f, 0.02f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.55f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Gradient BuildEmberGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.08f), 0.5f),
                new GradientColorKey(new Color(0.7f, 0.08f, 0.03f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0.5f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    // Sprites/Default multiplies the texture by the particle colour and premultiplies the result, so
    // plain white shapes carrying nothing but an alpha come out tinted by the gradients above.
    private static Material BuildParticleMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Sprites/Default");
        return new Material(shader)
        {
            mainTexture = texture,
            hideFlags = HideFlags.DontSave
        };
    }

    // A tongue of fire: widest at its foot, tapering to a tip, soft all the way round.
    private static Texture2D BuildFlameTexture()
    {
        const int size = 128;
        Texture2D texture = NewTexture(size, size);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size;
            float taper = Mathf.Pow(1f - v, 0.55f);
            float halfWidth = Mathf.Max(0.1f, taper);
            float foot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.2f, v));
            float tip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.7f, v));

            for (int x = 0; x < size; x++)
            {
                float across = ((x + 0.5f) / size - 0.5f) * 2f;
                float body = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(Mathf.Abs(across) / halfWidth));
                pixels[y * size + x] = Alpha(body * foot * tip);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildEmberTexture()
    {
        const int size = 32;
        Texture2D texture = NewTexture(size, size);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float dy = ((y + 0.5f) / size - 0.5f) * 2f;

            for (int x = 0; x < size; x++)
            {
                float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = Alpha(Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(distance)));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // Dark at the edges and along the floor, clear over the middle of the playfield - the blocks and
    // the cat keep their colours, the frame around them catches fire.
    private static Texture2D BuildGlowTexture()
    {
        const int size = 256;
        Texture2D texture = NewTexture(size, size);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size;
            float dy = (v - 0.5f) * 2f;
            float floorGlow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0f, v)) * 0.9f;

            for (int x = 0; x < size; x++)
            {
                float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                float radius = Mathf.Sqrt(dx * dx * 0.78f + dy * dy);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.05f, radius));
                pixels[y * size + x] = Alpha(Mathf.Max(edge, floorGlow));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Color32 Alpha(float alpha)
    {
        return new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
    }

    private static Texture2D NewTexture(int width, int height)
    {
        return new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }

    private static void Discard(Object generated)
    {
        if (generated == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generated);
        }
        else
        {
            DestroyImmediate(generated);
        }
    }
}
