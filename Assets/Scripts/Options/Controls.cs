using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

// What the game asks about instead of asking about keys. Every player-facing action is named here
// once, and what it is bound to comes from a saved setting the player can change.
//
// The legacy Input Manager cannot be rebound at runtime - its axes are fixed at build time - so the
// actions read KeyCodes directly, and the Input Manager keeps only the things that stay put: UI
// navigation, Submit and Cancel.
//
// The bindings live in one string setting rather than a dozen: it is a TypeDistinguisher like every
// other setting, so it rides the same save file, the same Steam Cloud and the same purge rules
// without anyone adding an asset per key.
//
// A gamepad answers to the same actions but is not part of that: its buttons are fixed, and its
// stick is an axis rather than a key. A player who wants a pad laid out differently remaps it in
// Steam's own configurator, which is where someone holding one looks first.
public static class Controls
{
    public const string MoveLeft = "MoveLeft";
    public const string MoveRight = "MoveRight";
    public const string Launch = "Launch";
    public const string Pause = "Pause";
    public const string Shop = "Shop";
    public const string Options = "Options";

    // The order the rebinding screen lists them in.
    public static readonly string[] All = { MoveLeft, MoveRight, Launch, Pause, Shop, Options };

    private const string SettingKey = "keyBindings";

    private struct Binding
    {
        public KeyCode Primary;
        public KeyCode Secondary;
    }

    // Exactly what the Input Manager was set to before any of this existed, so a player who never
    // opens the rebinding screen notices no difference at all.
    private static readonly Dictionary<string, Binding> Defaults = new Dictionary<string, Binding>
    {
        { MoveLeft, new Binding { Primary = KeyCode.LeftArrow, Secondary = KeyCode.A } },
        { MoveRight, new Binding { Primary = KeyCode.RightArrow, Secondary = KeyCode.D } },
        { Launch, new Binding { Primary = KeyCode.Space, Secondary = KeyCode.Mouse0 } },
        { Pause, new Binding { Primary = KeyCode.P, Secondary = KeyCode.None } },
        { Shop, new Binding { Primary = KeyCode.Tab, Secondary = KeyCode.None } },
        { Options, new Binding { Primary = KeyCode.O, Secondary = KeyCode.None } },
    };

    // What a gamepad does with no setting up at all. XInput numbering, which is what Unity reports
    // on Windows: A, X, Back, Start. Left and right are the stick, read as an axis below.
    private static readonly Dictionary<string, KeyCode> GamepadButtons = new Dictionary<string, KeyCode>
    {
        { Launch, KeyCode.JoystickButton0 },
        { Shop, KeyCode.JoystickButton2 },
        { Options, KeyCode.JoystickButton6 },
        { Pause, KeyCode.JoystickButton7 },
    };

    // Joystick-only on purpose. The Input Manager's own "Horizontal" folds the arrow keys into the
    // same reading, so arrows a player had rebound away would go on steering.
    private const string GamepadAxis = "Gamepad Horizontal";

    private static Dictionary<string, Binding> bindings;
    private static bool warnedAboutSetting;
    private static bool warnedAboutAxis;

    // Raised whenever a binding changes, so anything printing a key can write itself again. The
    // rebinding screen redraws its own buttons through KeyBindingButton.RefreshAll, but a key can
    // be moved while a screen behind the options window is still saying what it used to be.
    public static event Action OnBindingsChanged;

    // Cleared on every play, the way PauseManager clears its locks: a static dictionary survives
    // entering and leaving play mode in the editor and would otherwise carry the last run's edits.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        bindings = null;
        warnedAboutSetting = false;
        warnedAboutAxis = false;
        OnBindingsChanged = null;
    }

    public static KeyCode Primary(string action)
    {
        return Resolve(action).Primary;
    }

    public static KeyCode Secondary(string action)
    {
        return Resolve(action).Secondary;
    }

    public static bool Held(string action)
    {
        Binding binding = Resolve(action);
        return IsDown(binding.Primary) || IsDown(binding.Secondary) || IsDown(GamepadButton(action));
    }

    public static bool Pressed(string action)
    {
        Binding binding = Resolve(action);
        return WentDown(binding.Primary) || WentDown(binding.Secondary) || WentDown(GamepadButton(action));
    }

    // How far the stick is pushed sideways, -1 to 1, or 0 with no pad plugged in. The dead zone is
    // the axis's own, so a resting stick reads as nothing here rather than everywhere it is used.
    public static float MoveAxis()
    {
        if (warnedAboutAxis)
        {
            return 0f;
        }

        try
        {
            return Input.GetAxis(GamepadAxis);
        }
        catch (System.ArgumentException)
        {
            warnedAboutAxis = true;
            Debug.LogWarning("Controls: no \"" + GamepadAxis + "\" axis in the Input Manager - a gamepad stick will not steer.");
            return 0f;
        }
    }

    private static KeyCode GamepadButton(string action)
    {
        KeyCode key;
        return GamepadButtons.TryGetValue(action, out key) ? key : KeyCode.None;
    }

    public static void Bind(string action, KeyCode key, bool secondary)
    {
        Binding binding = Resolve(action);

        if (secondary)
        {
            binding.Secondary = key;
        }
        else
        {
            binding.Primary = key;
        }

        bindings[action] = binding;
        Save();
        OnBindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        bindings = new Dictionary<string, Binding>(Defaults);
        Save();
        OnBindingsChanged?.Invoke();
    }

    // "Left Arrow" rather than "LeftArrow", and something readable for the mouse buttons, because
    // this is what the rebinding screen prints on its buttons. The result is also the translation
    // key the screen looks the face up by, which is why the enum names that read as nothing to a
    // player - Alpha1 for the 1 above the letters, Keypad1 for the one on the numpad - are turned
    // into what the key is actually called before they leave here.
    public static string Describe(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None:
                return "-";
            // "Return" would collide with the Return that takes a player out of a window, which is
            // a different word in most languages and would end up on the key.
            case KeyCode.Return:
                return "Enter";
            case KeyCode.Mouse0:
                return "Left mouse";
            case KeyCode.Mouse1:
                return "Right mouse";
            case KeyCode.Mouse2:
                return "Middle mouse";
        }

        string name = key.ToString();

        if (name.StartsWith("Alpha"))
        {
            return name.Substring("Alpha".Length);
        }

        if (name.StartsWith("Keypad"))
        {
            return "Numpad " + Spaced(name.Substring("Keypad".Length));
        }

        if (name.StartsWith("Mouse"))
        {
            return "Mouse " + name.Substring("Mouse".Length);
        }

        return Spaced(name);
    }

    // A space wherever the enum name runs two words together, and nowhere else.
    private static string Spaced(string name)
    {
        var spaced = new StringBuilder(name.Length + 4);

        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                spaced.Append(' ');
            }

            spaced.Append(name[i]);
        }

        return spaced.ToString();
    }

    private static bool IsDown(KeyCode key)
    {
        return key != KeyCode.None && !SwallowedByInterface(key) && Input.GetKey(key);
    }

    private static bool WentDown(KeyCode key)
    {
        return key != KeyCode.None && !SwallowedByInterface(key) && Input.GetKeyDown(key);
    }

    // A click that lands on a window belongs to the window. Launch is bound to the left mouse
    // button by default, so without this, pressing a button in the options window served the ball
    // at the same time.
    private static bool SwallowedByInterface(KeyCode key)
    {
        // A pad's A button is Submit as well, so while a window is up it belongs to the window -
        // otherwise buying something in the shop served the ball at the same time. Only that one:
        // the buttons that open and close windows have to go on working while a window is open.
        if (key == KeyCode.JoystickButton0)
        {
            return PauseManager.IsPaused;
        }

        if (key < KeyCode.Mouse0 || key > KeyCode.Mouse6)
        {
            return false;
        }

        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static Binding Resolve(string action)
    {
        if (bindings == null)
        {
            Load();
        }

        Binding binding;

        if (bindings.TryGetValue(action, out binding))
        {
            return binding;
        }

        Debug.LogWarning("Controls: nothing called " + action + " - the game asked for an action that is not on the list.");
        binding = new Binding { Primary = KeyCode.None, Secondary = KeyCode.None };
        bindings[action] = binding;
        return binding;
    }

    // Format: one entry per action, "name=primary,secondary", entries separated by semicolons.
    // Anything unreadable falls back to that action's default rather than taking the rest down with
    // it - a player whose save got mangled should still find the game playable.
    private static void Load()
    {
        bindings = new Dictionary<string, Binding>(Defaults);
        TypeDistinguisher setting = OptionSettings.Find(SettingKey);

        if (setting == null)
        {
            if (!warnedAboutSetting)
            {
                warnedAboutSetting = true;
                Debug.LogWarning("Controls: no " + SettingKey + " setting - the defaults are in use and nothing will be saved.");
            }

            return;
        }

        string stored = setting.StringValue;

        if (string.IsNullOrEmpty(stored))
        {
            return;
        }

        foreach (string entry in stored.Split(';'))
        {
            string[] halves = entry.Split('=');

            if (halves.Length != 2 || !Defaults.ContainsKey(halves[0]))
            {
                continue;
            }

            string[] keys = halves[1].Split(',');
            Binding binding = bindings[halves[0]];
            int primary;
            int secondary;

            if (keys.Length > 0 && int.TryParse(keys[0], out primary))
            {
                binding.Primary = (KeyCode)primary;
            }

            if (keys.Length > 1 && int.TryParse(keys[1], out secondary))
            {
                binding.Secondary = (KeyCode)secondary;
            }

            bindings[halves[0]] = binding;
        }
    }

    private static void Save()
    {
        TypeDistinguisher setting = OptionSettings.Find(SettingKey);

        if (setting == null)
        {
            return;
        }

        var text = new StringBuilder();

        foreach (string action in All)
        {
            Binding binding = bindings[action];

            if (text.Length > 0)
            {
                text.Append(';');
            }

            text.Append(action).Append('=').Append((int)binding.Primary).Append(',').Append((int)binding.Secondary);
        }

        setting.SetValue(text.ToString());
    }
}
