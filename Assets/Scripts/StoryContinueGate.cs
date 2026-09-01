using UnityEngine;

// Holds the block that carries on out of sight until the beat on screen has been read to the end.
// The block is reached by sliding the paddle into it, and the paddle is held still for the same
// stretch - so a player who scrolled past nothing could previously walk out of a beat without ever
// seeing what it said.
//
// Sits on the block itself and leaves the GameObject alone: StoryManager switches that on and off
// to choose between carrying on and the ending, and two things writing SetActive on one object is
// how a block ends up neither shown nor hidden. Only what draws and what can be touched is taken
// away, and given back the moment the text has been read.
[DisallowMultipleComponent]
public class StoryContinueGate : MonoBehaviour
{
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private bool shown = true;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void OnEnable()
    {
        // Every time the block comes back it starts hidden, whatever it was last time it was up.
        Show(false);
    }

    private void OnDisable()
    {
        // Left as it was found, so nothing else has to know this component was ever here.
        Show(true);
    }

    private void Update()
    {
        Show(StoryTextScrollSetup.TextRead);
    }

    private void Show(bool visible)
    {
        if (shown == visible)
        {
            return;
        }

        shown = visible;

        foreach (Renderer candidate in renderers)
        {
            if (candidate != null)
            {
                candidate.enabled = visible;
            }
        }

        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null)
            {
                candidate.enabled = visible;
            }
        }
    }
}
