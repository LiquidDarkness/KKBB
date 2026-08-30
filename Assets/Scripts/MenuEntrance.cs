using System.Collections;
using UnityEngine;

// Brings a menu element out from behind another one and lets it settle on top: it starts hidden
// under the logo, slides out to where it belongs, goes a little past and drops back onto it. What
// makes the endless button the first thing seen without anything else on the menu having to move
// aside for it.
//
// Being behind is a matter of draw order, which under a Canvas is the order of the hierarchy: later
// children are drawn over earlier ones, and a child of a later branch is drawn over the whole of an
// earlier one. So for the flight the element is lent to the branch the thing it hides behind lives
// in, placed before it, and handed back its own place part way out - which is the moment it stops
// being covered and starts covering. Everything in between is flown in world space, so being in one
// parent or the other makes no difference to where it is on screen.
//
// Switched off - by the Menu animations option, or by Reduce motion above it - it never moves
// anything at all: the first thing the flight does is wait a frame, which is the frame the options
// get to disable this in.
[RequireComponent(typeof(RectTransform))]
public class MenuEntrance : MonoBehaviour
{
    [Header("Where it comes from")]
    [Tooltip("What it hides behind, and where it starts. Left empty it simply slides in from the offset below.")]
    public RectTransform hideBehind;

    [Tooltip("Added to the starting point, in canvas units. Use it to push the start further under the artwork - a logo is rarely centred in its own image.")]
    public Vector2 startOffset;

    [Header("The flight")]
    [Tooltip("Seconds of stillness before it sets off, so the menu is drawn first and the movement is noticed.")]
    public float delay = 0.35f;

    [Tooltip("Seconds it takes to get there.")]
    public float travel = 0.7f;

    [Tooltip("How far past its place it goes before dropping back onto it, as a share of the distance. 0 lands flat.")]
    public float overshoot = 0.5f;

    [Tooltip("How far along the flight it stops being drawn under and starts being drawn over, 0 to 1.")]
    [Range(0f, 1f)]
    public float emergeAt = 0.45f;

    private RectTransform rect;
    private Transform restingParent;
    private Vector2 restingPlace;
    private int restingOrder;
    private bool remembered;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        Remember();
        StartCoroutine(Fly());
    }

    // Coroutines outlive the component being switched off - disabling a MonoBehaviour does not stop
    // them - so the flight is stopped by hand, and whatever it was in the middle of is undone.
    private void OnDisable()
    {
        StopAllCoroutines();
        Settle();
    }

    // Read once, from wherever the element was placed in the scene. Everything after that is
    // measured from it, so the flight can never wander off with the thing it is moving - nor leave
    // it in the branch it was only lent to.
    private void Remember()
    {
        if (remembered)
        {
            return;
        }

        restingParent = rect.parent;
        restingPlace = rect.anchoredPosition;
        restingOrder = rect.GetSiblingIndex();
        remembered = true;
    }

    private IEnumerator Fly()
    {
        // A frame first, and every piece of hierarchy work below waits for it. Two reasons, and
        // both of them bite: the option that switches this off does it one frame into the scene, so
        // nothing should have been moved by then - and OnEnable can be part of a parent being
        // switched on, which is a moment Unity refuses to have anything reparented in. A frame later
        // that has finished happening.
        yield return null;

        // A flight cut short by the menu being switched off leaves this lent to the logo's branch,
        // since that is a moment when the hierarchy may not be touched either. Here it may be.
        Settle();

        Vector3 destination = rect.position;
        Vector3 start = StartingPlace(destination);

        GoBehind();
        rect.position = start;

        float waited = 0f;

        while (waited < delay)
        {
            // Unscaled throughout: the menu is drawn while Time.timeScale still holds whatever the
            // last level was played at, and after a few endless waves that is not one.
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        float flown = 0f;
        bool emerged = false;

        while (flown < travel)
        {
            flown += Time.unscaledDeltaTime;
            float howFar = travel > 0f ? Mathf.Clamp01(flown / travel) : 1f;

            rect.position = Vector3.LerpUnclamped(start, destination, Landing(howFar));

            if (!emerged && howFar >= emergeAt)
            {
                // Out from under it, and from here on over the top of it.
                ComeHome();
                emerged = true;
            }

            yield return null;
        }

        Settle();
    }

    // Where it sets off from, in world space: the middle of whatever it hides behind, or its own
    // place nudged by the offset when there is nothing to hide behind.
    private Vector3 StartingPlace(Vector3 destination)
    {
        Vector3 start = hideBehind != null
            ? hideBehind.TransformPoint(hideBehind.rect.center)
            : destination;

        // The offset is written in canvas units, which is what the inspector shows everywhere else,
        // so it is scaled by however large the canvas is being drawn.
        float scale = restingParent != null ? restingParent.lossyScale.x : 1f;
        start.x += startOffset.x * scale;
        start.y += startOffset.y * scale;

        // Flat against the canvas, whatever the depth of the thing it starts behind.
        start.z = destination.z;

        return start;
    }

    private void GoBehind()
    {
        if (hideBehind == null || hideBehind.parent == null)
        {
            return;
        }

        // Lent to the branch the logo is in, and placed before it. Keeping the world position is
        // what makes this invisible: only the order it is drawn in changes.
        rect.SetParent(hideBehind.parent, true);
        rect.SetSiblingIndex(hideBehind.GetSiblingIndex());
    }

    // True once it is back where it belongs. False means the hierarchy could not be touched just
    // now, and it is still lent out - which the next OnEnable puts right.
    private bool ComeHome()
    {
        if (restingParent == null)
        {
            return false;
        }

        if (rect.parent == restingParent && rect.GetSiblingIndex() == restingOrder)
        {
            return true;
        }

        // Unity refuses to reparent anything while an object above it is being switched on or off,
        // and says so loudly. That is not a rare corner here: ObjectActivator switches the whole of
        // Content off and on again as the menu starts, and every switch is an OnDisable through
        // this. While that is happening this object is already out of the hierarchy, which is
        // exactly what makes it safe to leave where it is and put right on the way back in.
        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        rect.SetParent(restingParent, true);
        rect.SetSiblingIndex(restingOrder);

        return true;
    }

    // Overshoots and comes back, which is what reads as landing rather than as stopping.
    private float Landing(float howFar)
    {
        float past = Mathf.Max(0f, overshoot);
        float back = past + 1f;
        float remaining = howFar - 1f;

        return 1f + back * remaining * remaining * remaining + past * remaining * remaining;
    }

    // Where it belongs, in the branch it belongs to, drawn where it belongs. Called at the end of
    // the flight and again whenever this is switched off part way through one.
    private void Settle()
    {
        if (!remembered || rect == null)
        {
            return;
        }

        // The place it belongs is a place in its own parent, so there is no sense writing it while
        // it is still lent to another one - it would land somewhere else entirely. The next
        // OnEnable does both together.
        if (ComeHome())
        {
            rect.anchoredPosition = restingPlace;
        }
    }
}
