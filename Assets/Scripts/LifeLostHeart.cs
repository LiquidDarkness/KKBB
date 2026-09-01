using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The heart that was just spent, leaving. HealthDisplayer throws the whole row away and builds it
// again on every change, so the lost heart is gone before anything could animate it - this puts a
// copy back where it stood and lets that one go instead. Nothing in the row itself is touched, so
// the two cannot fight over the layout.
//
// Where it stood is remembered a frame at a time rather than worked out from the count, because the
// row is a layout group and only the layout knows where a slot ends up.
public class LifeLostHeart : MonoBehaviour
{
    public HealthDisplayer healthDisplayer;

    [Tooltip("Optional. With Reduce motion on the ghost only fades, without swelling.")]
    public TypeDistinguisher reduceMotion;

    public Color tint = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("How long the ghost takes to fade away.")]
    public float seconds = 0.55f;

    [Tooltip("How much bigger it swells before it goes.")]
    public float grow = 1.8f;

    private Vector3 lastHeartPosition;
    private bool hasPosition;

    private void OnEnable()
    {
        PlayerHealth.OnHealthLost += Spawn;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthLost -= Spawn;
    }

    // A frame behind on purpose. LoseHealth raises OnHealthChanged first, which rebuilds the row
    // without the spent heart, and only then OnHealthLost - so by the time we are asked, the slot
    // we want is already empty. What was on screen last frame is exactly what the player saw.
    private void LateUpdate()
    {
        Transform container = Container;

        if (container == null || container.childCount == 0)
        {
            return;
        }

        lastHeartPosition = container.GetChild(container.childCount - 1).position;
        hasPosition = true;
    }

    private Transform Container => healthDisplayer != null ? healthDisplayer.heartContainer : null;

    public void Spawn()
    {
        Transform container = Container;

        if (!hasPosition || container == null || healthDisplayer.heartImage == null || container.parent == null)
        {
            return;
        }

        // Parented beside the row rather than inside it: a child of the layout group would be
        // pushed into a slot of its own and shove the hearts that are left along.
        GameObject ghost = Instantiate(healthDisplayer.heartImage, container.parent);
        ghost.name = "Lost heart";
        ghost.transform.position = lastHeartPosition;

        // A layout element on the copy would be read by the parent's own layout, if it has one.
        foreach (LayoutElement element in ghost.GetComponentsInChildren<LayoutElement>(true))
        {
            element.ignoreLayout = true;
        }

        StartCoroutine(Fade(ghost));
    }

    private IEnumerator<object> Fade(GameObject ghost)
    {
        var graphics = new List<Graphic>(ghost.GetComponentsInChildren<Graphic>(true));
        var startingColours = new List<Color>(graphics.Count);

        foreach (Graphic graphic in graphics)
        {
            startingColours.Add(graphic.color);
            graphic.raycastTarget = false;
        }

        Vector3 startingScale = ghost.transform.localScale;
        bool motion = reduceMotion == null || !reduceMotion.BoolValue;
        float left = seconds;

        while (left > 0f && ghost != null)
        {
            // Unscaled: Time.timeScale is the pace of play, and a life is not lost more slowly on
            // an easier difficulty.
            left -= Time.unscaledDeltaTime;
            float gone = seconds > 0f ? Mathf.Clamp01(1f - left / seconds) : 1f;

            for (int i = 0; i < graphics.Count; i++)
            {
                if (graphics[i] == null)
                {
                    continue;
                }

                Color colour = Color.Lerp(startingColours[i], tint, gone);
                colour.a = startingColours[i].a * (1f - gone);
                graphics[i].color = colour;
            }

            if (motion)
            {
                ghost.transform.localScale = startingScale * Mathf.Lerp(1f, grow, gone);
            }

            yield return null;
        }

        if (ghost != null)
        {
            Destroy(ghost);
        }
    }
}
