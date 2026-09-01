using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Wraps the story TMP_Text in a ScrollRect/Viewport at runtime, so text taller than its box
// becomes scrollable (mouse wheel, drag, keyboard, or auto-scroll) instead of overflowing past
// the readable area. Add this component directly on the StoryText GameObject - no other wiring
// needed (the Canvas it lives under still needs its own GraphicRaycaster for input to reach it).
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TMP_Text))]
public class StoryTextScrollSetup : MonoBehaviour
{
    [Header("Mouse / Drag")]
    [SerializeField] private float scrollSensitivity = 30f;

    [Header("Keyboard (W/S, Up/Down) - lines per second")]
    [SerializeField] private float keyboardScrollSpeed = 3f;

    [Header("Auto-scroll - lines per second")]
    [Tooltip("Used only until the player sets autoScrollSpeedSetting via the options slider (falls back to this while that value is unset).")]
    [SerializeField] private float defaultAutoScrollSpeed = 0.55f;
    public TypeDistinguisher autoScrollAnimation;
    public TypeDistinguisher autoScrollSpeedSetting;
    [Tooltip("How long auto-scroll stays paused after the last manual scroll input (drag, wheel, or keyboard).")]
    [SerializeField] private float manualScrollPauseDuration = 1.5f;
    [Tooltip("Grace period after a new story appears, so the player can read the opening lines before auto-scroll starts moving them.")]
    [SerializeField] private float autoScrollStartDelay = 2.5f;

    private ScrollRect scrollRect;
    private TMP_Text content;
    private float manualScrollCooldown;
    private float autoScrollStartCountdown;
    private string lastText;
    private bool atEnd;

    // The one story text on screen, or none at all while no beat is up. Held rather than searched
    // for, so that whoever asks the question below gets an answer without walking the scene.
    private static StoryTextScrollSetup active;

    // Whether the beat on screen has been read to its last line. The paddle waits on this, and so
    // does the block that carries on. With no story text alive the answer is yes: nothing should
    // ever be held still waiting on something that is not there.
    public static bool TextRead => active == null || active.atEnd;

    // Statics survive entering and leaving play mode in the editor, the way Controls and
    // PauseManager clear theirs.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
    }

    public void OnEnable()
    {
        active = this;

        // A beat can be shown a second time - Continue from the main menu lands on the one last
        // played - carrying the very same text, so watching the text alone would never notice it
        // had come back and the player would be handed a beat already scrolled to its end.
        lastText = null;
        atEnd = false;
    }

    public void OnDisable()
    {
        if (active == this)
        {
            active = null;
        }
    }

    public void Awake()
    {
        RectTransform textRect = (RectTransform)transform;
        TMP_Text text = GetComponent<TMP_Text>();
        content = text;
        Transform panel = textRect.parent;

        if (panel == null)
        {
            Debug.LogWarning("StoryTextScrollSetup needs a parent RectTransform to build the viewport in.");
            return;
        }

        // Viewport takes over the box the text used to fill, and clips anything taller than it.
        GameObject viewportObject = new GameObject("StoryTextViewport", typeof(RectTransform));
        RectTransform viewportRect = (RectTransform)viewportObject.transform;
        viewportRect.SetParent(panel, false);
        viewportRect.anchorMin = textRect.anchorMin;
        viewportRect.anchorMax = textRect.anchorMax;
        viewportRect.pivot = textRect.pivot;
        viewportRect.anchoredPosition = textRect.anchoredPosition;
        viewportRect.sizeDelta = textRect.sizeDelta;
        viewportObject.AddComponent<RectMask2D>();

        // Text becomes the scrolling content: stretches to the viewport width, grows downward
        // from the top to whatever height its wrapped content needs.
        textRect.SetParent(viewportRect, false);
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.anchoredPosition = Vector2.zero;

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = viewportObject.AddComponent<ScrollRect>();
        scrollRect.content = textRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = scrollSensitivity;

        // Reports drag/wheel activity on the ScrollRect itself, so auto-scroll can back off
        // while (and shortly after) the player scrolls manually with the mouse.
        ManualScrollRelay relay = viewportObject.AddComponent<ManualScrollRelay>();
        relay.owner = this;
    }

    public void Update()
    {
        if (scrollRect == null)
        {
            // Nothing was ever built to scroll, so there is nothing left to read: whatever waits on
            // this must not wait forever.
            atEnd = true;
            return;
        }

        // Every new story starts at the top with a short grace period, so the player always
        // sees it from the first line. Watching the text itself catches every path that can
        // swap the content (story progression, language change) without extra wiring.
        if (content.text != lastText)
        {
            lastText = content.text;
            ScrollToTop();
            autoScrollStartCountdown = autoScrollStartDelay;
            manualScrollCooldown = 0f;
        }

        if (autoScrollStartCountdown > 0)
        {
            autoScrollStartCountdown -= Time.unscaledDeltaTime;
        }

        // Normalized-position distance covered by exactly one line, so every configured speed
        // below reads as "lines per second" no matter how long the current chunk of text is.
        float normalizedPerLine = GetNormalizedDistancePerLine();

        float delta = 0f;
        bool manualKeyHeld = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            delta += keyboardScrollSpeed * normalizedPerLine;
            manualKeyHeld = true;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            delta -= keyboardScrollSpeed * normalizedPerLine;
            manualKeyHeld = true;
        }

        if (manualKeyHeld)
        {
            NotifyManualScroll();
        }
        else if (manualScrollCooldown > 0)
        {
            manualScrollCooldown -= Time.unscaledDeltaTime;
        }

        if (autoScrollAnimation.BoolValue && manualScrollCooldown <= 0 && autoScrollStartCountdown <= 0)
        {
            float speed = autoScrollSpeedSetting.FloatValue;
            float linesPerSecond = speed > 0 ? speed : defaultAutoScrollSpeed;
            delta -= linesPerSecond * normalizedPerLine;
        }

        if (delta != 0f)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                scrollRect.verticalNormalizedPosition + delta * Time.unscaledDeltaTime);
        }

        atEnd = IsAtEnd();
    }

    // Read to the end, or too short to have an end worth scrolling to. The second case matters as
    // much as the first: most beats fit their box, and a player must not be held still in front of
    // a text that was never going to move.
    private bool IsAtEnd()
    {
        if (scrollRect.content == null || scrollRect.viewport == null)
        {
            return true;
        }

        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height + 1f)
        {
            return true;
        }

        return scrollRect.verticalNormalizedPosition <= 0.01f;
    }

    private void ScrollToTop()
    {
        // The ContentSizeFitter hasn't resized the text to the new content yet, so force the
        // layout through first - otherwise "top" is measured against the previous story's height.
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.velocity = Vector2.zero;
    }

    private float GetNormalizedDistancePerLine()
    {
        float scrollableRange = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
        if (scrollableRange <= 0f)
        {
            return 0f;
        }

        float lineHeight = (content.textInfo != null && content.textInfo.lineCount > 0)
            ? content.textInfo.lineInfo[0].lineHeight
            : content.fontSize * 1.2f;

        return lineHeight / scrollableRange;
    }

    private void NotifyManualScroll()
    {
        manualScrollCooldown = manualScrollPauseDuration;
    }

    // Lives on the same GameObject as the ScrollRect so it receives the same drag/wheel events.
    private class ManualScrollRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public StoryTextScrollSetup owner;

        public void OnBeginDrag(PointerEventData eventData) => owner.NotifyManualScroll();
        public void OnDrag(PointerEventData eventData) => owner.NotifyManualScroll();
        public void OnEndDrag(PointerEventData eventData) => owner.NotifyManualScroll();
        public void OnScroll(PointerEventData eventData) => owner.NotifyManualScroll();
    }
}
