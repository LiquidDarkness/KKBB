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

    private static Dictionary<string, Binding> bindings;
    private static bool warnedAboutSetting;

    // Cleared on every play, the way PauseManager clears its locks: a static dictionary survives
    // entering and leaving play mode in the editor and would otherwise carry the last run's edits.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        bindings = null;
        warnedAboutSetting = false;
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
        return IsDown(binding.Primary) || IsDown(binding.Secondary);
    }

    public static bool Pressed(string action)
    {
        Binding binding = Resolve(action);
        return WentDown(binding.Primary) || WentDown(binding.Secondary);
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
    }

    public static void ResetToDefaults()
    {
        bindings = new Dictionary<string, Binding>(Defaults);
        Save();
    }

    // "Left Arrow" rather than "LeftArrow", and something readable for the mouse buttons, because
    // this is what the rebinding screen prints on its buttons.
    public static string Describe(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None:
                return "-";
            case KeyCode.Mouse0:
                return "Left mouse";
            case KeyCode.Mouse1:
                return "Right mouse";
            case KeyCode.Mouse2:
                return "Middle mouse";
        }

        string name = key.ToString();
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
