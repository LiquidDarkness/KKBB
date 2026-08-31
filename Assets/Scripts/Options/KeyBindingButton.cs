using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One key on the rebinding screen. Shows what the action is bound to, and on a click waits for the
// next key pressed and binds that instead.
public class KeyBindingButton : MonoBehaviour
{
    public string action;

    [Tooltip("Which of the two keys this button stands for.")]
    public bool secondary;

    public Button button;
    public TMP_Text label;

    [Tooltip("Shown while the button is waiting for a key. A translation key like every other string a player reads, so it needs an entry in EN.json and PL.json.")]
    public string waitingText = "press a key";

    // Every button on screen, so that binding a key on one of them can redraw the rest - taking a
    // key away from whichever action had it is not visible on the button doing the taking.
    private static readonly List<KeyBindingButton> live = new List<KeyBindingButton>();

    private static KeyCode[] everyKey;

    private bool waiting;
    private int startedOnFrame;

    // Escape cancels a capture, and Escape also closes the options window. The window checks this
    // so one press does not do both.
    public static bool Capturing
    {
        get
        {
            foreach (KeyBindingButton candidate in live)
            {
                if (candidate != null && candidate.waiting)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void OnEnable()
    {
        live.Add(this);

        // The face is looked up rather than printed as written, so it has to be drawn again when
        // the language changes under it.
        TranslationJSONDeserializer.OnTransaltionUpdated += Refresh;

        if (button != null)
        {
            // Runtime listeners only; anything wired in the Inspector, like the click sound, stays.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(BeginCapture);
        }

        Refresh();
    }

    private void OnDisable()
    {
        TranslationJSONDeserializer.OnTransaltionUpdated -= Refresh;
        live.Remove(this);
        waiting = false;
    }

    public void Refresh()
    {
        if (label == null)
        {
            return;
        }

        // Both of these are translation keys: waitingText is a sentence, and what Describe returns
        // is the name of a key. A name with no entry of its own - "A", "F5" - prints itself, which
        // is what it would have said anyway.
        label.text = TranslationLookup.Get(waiting
            ? waitingText
            : Controls.Describe(secondary ? Controls.Secondary(action) : Controls.Primary(action)));
    }

    private void BeginCapture()
    {
        waiting = true;
        startedOnFrame = Time.frameCount;
        Refresh();
    }

    private void Update()
    {
        if (!waiting)
        {
            return;
        }

        // The click that started this is still down on the frame it started, and would bind itself.
        if (Time.frameCount == startedOnFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waiting = false;
            Refresh();
            return;
        }

        if (everyKey == null)
        {
            everyKey = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
        }

        foreach (KeyCode key in everyKey)
        {
            if (!Input.GetKeyDown(key))
            {
                continue;
            }

            // A pad is not rebound here. Its buttons are fixed in Controls, and on Steam it is
            // remapped in Steam's configurator - so a stray press while this screen is open should
            // not quietly take a key away from the keyboard.
            if (key >= KeyCode.JoystickButton0)
            {
                continue;
            }

            waiting = false;
            TakeKeyFromOthers(key);
            Controls.Bind(action, key, secondary);
            RefreshAll();
            return;
        }
    }

    // A key belongs to one action. Whoever held it loses it, rather than both answering to the same
    // press and the player wondering why serving the ball also opens the shop. Everything is cleared
    // except the slot about to be written.
    private void TakeKeyFromOthers(KeyCode key)
    {
        foreach (string other in Controls.All)
        {
            bool sameAction = other == action;

            if (Controls.Primary(other) == key && !(sameAction && !secondary))
            {
                Controls.Bind(other, KeyCode.None, false);
            }

            if (Controls.Secondary(other) == key && !(sameAction && secondary))
            {
                Controls.Bind(other, KeyCode.None, true);
            }
        }
    }

    public static void RefreshAll()
    {
        foreach (KeyBindingButton candidate in live)
        {
            if (candidate != null)
            {
                candidate.Refresh();
            }
        }
    }
}
