using UnityEngine;

// Takes its object away in a build that runs inside a browser tab, and leaves it alone everywhere
// else. There is nothing for Application.Quit to do on WebGL - the tab belongs to the page, not to
// the game - so a Quit button there is a button that visibly does nothing, which is worse than not
// offering it. A player on a portal closes the tab, or leaves the page.
//
// Done in Awake rather than by deleting the object from the scene, because the same scene and the
// same prefabs build the desktop game, where quitting is exactly what a player expects.
[DisallowMultipleComponent]
public class HideOnWeb : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }
}
