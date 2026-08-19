using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keeps a window reachable without a mouse. Drop one on the root of every window that opens over
// the game - options, pause, shop, game over - and it will:
//
//   * put the selection on that window's first control the moment it opens,
//   * hand the selection back to whatever was selected before, when it closes,
//   * and put the selection back if it was lost (a click on empty space clears it, and after that
//     the arrow keys have nothing to move from).
//
// The last one only happens once the player actually presses a navigation key, so a mouse player
// never sees a highlight appear on its own.
[DisallowMultipleComponent]
public class WindowKeyboardFocus : MonoBehaviour
{
    [Tooltip("Control to select when this window opens. Left empty, the first interactable control found under it is used.")]
    public Selectable firstSelected;

    [Tooltip("Give the selection back to whatever held it before this window opened.")]
    public bool restoreOnClose = true;

    [Tooltip("Axis and button names as set on the EventSystem's Standalone Input Module.")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string submitButton = "Submit";

    private GameObject selectionBeforeOpen;

    private void OnEnable()
    {
        EventSystem events = EventSystem.current;

        if (events == null)
        {
            return;
        }

        selectionBeforeOpen = events.currentSelectedGameObject;

        // One frame late on purpose: layout groups and any group switched on in the same frame as
        // this window have not finished by now, and a control that is not active yet cannot be
        // selected. A coroutine and not Invoke: these windows open with Time.timeScale at 0, and
        // Invoke waits on scaled time, which never advances while the game is paused.
        StartCoroutine(SelectFirstNextFrame());
    }

    private IEnumerator SelectFirstNextFrame()
    {
        yield return null;
        SelectFirst();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (!restoreOnClose || EventSystem.current == null)
        {
            return;
        }

        if (selectionBeforeOpen != null && selectionBeforeOpen.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(selectionBeforeOpen);
        }

        selectionBeforeOpen = null;
    }

    private void Update()
    {
        EventSystem events = EventSystem.current;

        if (events == null)
        {
            return;
        }

        GameObject selected = events.currentSelectedGameObject;

        if (selected != null && selected.activeInHierarchy)
        {
            return;
        }

        if (NavigationPressed())
        {
            SelectFirst();
        }
    }

    private bool NavigationPressed()
    {
        return Mathf.Abs(Input.GetAxisRaw(horizontalAxis)) > 0.1f
            || Mathf.Abs(Input.GetAxisRaw(verticalAxis)) > 0.1f
            || Input.GetButtonDown(submitButton);
    }

    [ContextMenu(nameof(SelectFirst))]
    public void SelectFirst()
    {
        EventSystem events = EventSystem.current;

        if (events == null)
        {
            return;
        }

        Selectable target = firstSelected != null && firstSelected.gameObject.activeInHierarchy && firstSelected.IsInteractable()
            ? firstSelected
            : FindFirstInteractable();

        if (target != null)
        {
            events.SetSelectedGameObject(target.gameObject);
        }
    }

    // Inactive children are skipped: a control on a tab that is currently closed is not somewhere
    // the player can be sent.
    private Selectable FindFirstInteractable()
    {
        foreach (Selectable candidate in GetComponentsInChildren<Selectable>(false))
        {
            if (candidate.IsInteractable() && candidate.navigation.mode != Navigation.Mode.None)
            {
                return candidate;
            }
        }

        return null;
    }
}
