using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// The one owner of Escape. It closes whichever window is on top, and opens the options window when
// there is nothing to close.
//
// Before this the key was read in two places at once - WindowManager toggled the pause overlay on
// the Pause axis, which had escape on it, while WindowButton.PullMenu read KeyCode.Escape directly -
// so one press with the options window open both closed it and opened the pause overlay. The Pause
// axis is now the P key alone.
public class EscapeShortcut : MonoBehaviour
{
    [Tooltip("Read as a button so the gamepad's B comes along for free; Cancel is escape + joystick button 1.")]
    public string cancelButton = "Cancel";

    public WindowManager windowManager;

    [Tooltip("Opened when Escape is pressed and nothing is open.")]
    public GameObject optionsWindow;

    [Tooltip("Scene where Escape does not open anything: the menu is already the top level and has its own button for this.")]
    public string topLevelScene = "Menu";

    private void Update()
    {
        if (!Input.GetButtonDown(cancelButton))
        {
            return;
        }

        // A rebinding button waiting for a key owns Escape: it means "never mind", not "close the
        // window".
        if (KeyBindingButton.Capturing)
        {
            return;
        }

        EscapeWindow top = EscapeWindow.Topmost;

        if (top != null)
        {
            // A dropdown handles Cancel itself, to roll its list back up. Closing the window in the
            // same press would take the whole thing away from under the player's hand.
            if (HasExpandedDropdown(top))
            {
                return;
            }

            top.Close();
            return;
        }

        if (SceneManager.GetActiveScene().name == topLevelScene)
        {
            return;
        }

        if (windowManager != null && optionsWindow != null && !optionsWindow.activeInHierarchy)
        {
            windowManager.ToggleWindow(optionsWindow);
        }
    }

    private static bool HasExpandedDropdown(EscapeWindow window)
    {
        foreach (TMP_Dropdown dropdown in window.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            if (dropdown.IsExpanded)
            {
                return true;
            }
        }

        return false;
    }
}
