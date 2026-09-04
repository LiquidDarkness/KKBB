using UnityEngine;

// Keeps the whole arena inside the picture whatever shape the window is.
//
// The camera is orthographic, so its size fixes how much world fits vertically and the window's
// shape alone decides how much fits across: half-width is size times aspect. At size 15 that means
// a 16:9 window sees 26.7 units either side of the middle - and the side barriers stand at 26.9.
// It has always been that close, which is why nobody noticed until a build went out at 960x600. At
// that shape the camera sees 24 units, the barriers are nearly three units outside the picture, and
// a player watches the cat bounce off nothing at all near the edge of the screen. The blocks
// standing furthest out go the same way.
//
// So the size is raised, never lowered, until the barriers are back inside. A window wide enough to
// need no help is left exactly as it was - at 16:9 this asks for 15.14 against the 15 it started
// with, which is under a percent and is the whole cost of never having to think about it again.
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class ArenaFraming : MonoBehaviour
{
    [Tooltip("Distance from the middle of the arena to the outside of a side barrier, in world units. Measured from Leftbarrier and RightBarrier in Gameplay.")]
    public float arenaHalfWidth = 26.91f;

    [Tooltip("The size the camera is framed at when the window is already wide enough. Never goes below this.")]
    public float restingSize = 15f;

    private Camera view;

    private void Awake()
    {
        view = GetComponent<Camera>();
    }

    // In LateUpdate so that anything moving the camera has already had its say, and every frame
    // because a browser window is resized by the player at any moment and a canvas that fills the
    // page changes shape with it.
    private void LateUpdate()
    {
        if (!view.orthographic || view.aspect <= 0f)
        {
            return;
        }

        float wanted = SizeFor(restingSize, arenaHalfWidth, view.aspect);

        if (!Mathf.Approximately(view.orthographicSize, wanted))
        {
            view.orthographicSize = wanted;
        }
    }

    // Pulled out so it can be asked the question without a camera, a scene or a running game.
    public static float SizeFor(float restingSize, float arenaHalfWidth, float aspect)
    {
        if (aspect <= 0f)
        {
            return restingSize;
        }

        return Mathf.Max(restingSize, arenaHalfWidth / aspect);
    }
}
