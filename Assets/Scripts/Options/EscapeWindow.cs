using System.Collections.Generic;
using UnityEngine;

// Marks a window that Escape should close. Every one of these keeps itself in a shared list while
// it is on screen, most recently opened last, so the shortcut always knows which window is on top
// without anyone having to wire a list of them by hand - including windows that live in a scene and
// cannot be referenced from a prefab.
[DisallowMultipleComponent]
public class EscapeWindow : MonoBehaviour
{
    private static readonly List<EscapeWindow> open = new List<EscapeWindow>();

    public static EscapeWindow Topmost
    {
        get
        {
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (open[i] != null && open[i].gameObject.activeInHierarchy)
                {
                    return open[i];
                }
            }

            return null;
        }
    }

    private void OnEnable()
    {
        // Removed first so re-opening a window moves it to the top rather than leaving it where it
        // was the first time.
        open.Remove(this);
        open.Add(this);
    }

    private void OnDisable()
    {
        open.Remove(this);
    }

    // Closing has to give back whatever pause lock the opening took, and different windows here were
    // opened by different code. Rather than guess, this asks the thing that opened it to close it.
    public void Close()
    {
        foreach (WindowManager manager in FindObjectsOfType<WindowManager>())
        {
            foreach (WindowManager.WindowToggle entry in manager.windows)
            {
                if (entry.window == gameObject)
                {
                    manager.ToggleWindow(gameObject);
                    return;
                }
            }
        }

        foreach (WindowButton button in GetComponentsInChildren<WindowButton>(true))
        {
            // Its own two references are checked because they are filled by a scene lookup that
            // finds nothing outside the menu, and CloseWindow walks straight into both.
            if (button.targetWindow == gameObject && button.gameSession != null && button.objectActivation != null)
            {
                button.CloseWindow();
                return;
            }
        }

        gameObject.SetActive(false);
        PauseManager.Unpause(name);
    }
}
