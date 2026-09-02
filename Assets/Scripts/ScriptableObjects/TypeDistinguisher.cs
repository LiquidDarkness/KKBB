using System;
using System.Globalization;
using UnityEngine;

[CreateAssetMenu]
public class TypeDistinguisher : ScriptableObject
{
    public string PrefsKey => name;
    public PlayerPrefType prefType;
    public bool purgable = true;

    [Tooltip("What this setting is worth before the player has ever touched it. Written once, the first time the game runs with the setting in it - which includes a save file written before the setting existed. Read with the invariant culture, so 0.55 means the same thing on a Polish machine as on an English one. BOOL takes true/false or 1/0.\n\nLeave it blank and nothing is written at all: that is what the score, the chosen scenario and the rest of the saved progress want, since for them zero is already the right answer.")]
    public string defaultValue;

    public event Action OnValueChanged;

    public bool HasStoredValue => PlayerPrefs.HasKey(PrefsKey);

    private void OnEnable()
    {
        //OnValueChanged += SaveManager.Save;
        //OnValueChanged += PlayerPrefs.Save;
    }

    private void OnDisable()
    {
        //OnValueChanged -= SaveManager.Save;
        //OnValueChanged -= PlayerPrefs.Save;
    }

    public override string ToString()
    {
        return $"{PrefsKey}/{prefType}/{GetExportValue()}";
    }

    [ContextMenu(nameof(LogValue))]
    public  void LogValue()
    {
        Debug.Log(this.ToString());
    }

    // Seeding, done once per setting per install. PlayerPrefs answers 0 for a key nobody has
    // written - and BoolValue therefore false - which is why every component that wanted a
    // non-zero default used to carry a copy of one: FontScaler, VolumeSetter, StuckBallRecall and
    // StoryTextScrollSetup each had their own, and the three animation toggles, having nobody to
    // carry one for them, simply came up switched off on a fresh install. The default belongs to
    // the setting, where there is one answer and the save file can carry it.
    //
    // Returns true when it actually wrote something, so the caller knows the save file is behind.
    public bool ApplyDefaultIfUnset()
    {
        if (string.IsNullOrWhiteSpace(defaultValue) || HasStoredValue)
        {
            return false;
        }

        switch (prefType)
        {
            case PlayerPrefType.INT:
                if (!int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intDefault))
                {
                    return ComplainAboutDefault("a whole number");
                }

                PlayerPrefs.SetInt(PrefsKey, intDefault);
                break;

            case PlayerPrefType.FLOAT:
                if (!float.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatDefault))
                {
                    return ComplainAboutDefault("a number written with a dot for the decimal point");
                }

                PlayerPrefs.SetFloat(PrefsKey, floatDefault);
                break;

            case PlayerPrefType.BOOL:
                if (!TryParseBool(defaultValue, out bool boolDefault))
                {
                    return ComplainAboutDefault("true, false, 1 or 0");
                }

                PlayerPrefs.SetInt(PrefsKey, boolDefault ? 1 : 0);
                break;

            case PlayerPrefType.STRING:
                PlayerPrefs.SetString(PrefsKey, defaultValue);
                break;

            default:
                return false;
        }

        OnValueChanged?.Invoke();
        return true;
    }

    // The one-time repair for a save written before any setting had a default. Back then every
    // setting the player had not deliberately touched was stored as the zero PlayerPrefs hands
    // back for a key nobody wrote, and ApplyDefaultIfUnset cannot help with that: a stored zero is
    // a stored value, so it is left alone for ever. A player who never opened the options was left
    // with no sound at all, every animation off and no rescue for a wedged ball - and nothing short
    // of wiping their progress would give any of it back. One turned up doing exactly that.
    //
    // Only a stored value that cannot be told apart from "never written" is replaced, and only
    // where the default is something other than that same zero. Anything the player set to any
    // other value is theirs and is left where it is. The cost is that a deliberate zero - a game
    // someone muted on purpose - is read as an accident and given its default back. That is the one
    // case there is no way to tell apart, and silence left in place by mistake is the worse of the
    // two to ship.
    //
    // Returns true when it actually wrote something.
    public bool RepairIfStoredValueLooksUnwritten()
    {
        if (string.IsNullOrWhiteSpace(defaultValue) || !HasStoredValue)
        {
            return false;
        }

        switch (prefType)
        {
            case PlayerPrefType.INT:
                if (!int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intDefault)
                    || intDefault == 0 || IntValue != 0)
                {
                    return false;
                }

                PlayerPrefs.SetInt(PrefsKey, intDefault);
                break;

            case PlayerPrefType.FLOAT:
                if (!float.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatDefault)
                    || floatDefault == 0f || FloatValue != 0f)
                {
                    return false;
                }

                PlayerPrefs.SetFloat(PrefsKey, floatDefault);
                break;

            case PlayerPrefType.BOOL:
                // A default of false has nothing to repair towards: false is the zero.
                if (!TryParseBool(defaultValue, out bool boolDefault) || !boolDefault || BoolValue)
                {
                    return false;
                }

                PlayerPrefs.SetInt(PrefsKey, 1);
                break;

            case PlayerPrefType.STRING:
                if (defaultValue.Length == 0 || !string.IsNullOrEmpty(StringValue))
                {
                    return false;
                }

                PlayerPrefs.SetString(PrefsKey, defaultValue);
                break;

            default:
                return false;
        }

        OnValueChanged?.Invoke();
        return true;
    }

    private static bool TryParseBool(string text, out bool value)
    {
        if (bool.TryParse(text, out value))
        {
            return true;
        }

        // 1 and 0 are how a bool is written in PlayerPrefs and in the save file, so they have to be
        // accepted here too - anything else would make the default the odd one out.
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
        {
            value = number > 0;
            return true;
        }

        return false;
    }

    // Loud, and then left alone: a default nobody can read is a wiring mistake to fix in the
    // Inspector, and guessing a value in its place would hide it.
    private bool ComplainAboutDefault(string expected)
    {
        Debug.LogError($"[{nameof(TypeDistinguisher)}] {PrefsKey}: defaultValue \"{defaultValue}\" is not {expected} - the setting is left unset.", this);
        return false;
    }

    public int IntValue => PlayerPrefs.GetInt(PrefsKey);
    public float FloatValue => PlayerPrefs.GetFloat(PrefsKey);
    public string StringValue => PlayerPrefs.GetString(PrefsKey);

    public bool BoolValue => IntValue > 0;


    // Everything below is written and read with the invariant culture. The save file travels
    // between machines - that is the whole point of Steam Cloud - and the system culture decides
    // the decimal separator: a Polish Windows wrote "0,54" where an English one expects "0.54",
    // and cultures that read the comma as a thousands separator would turn it into 54.
    private string GetExportValue()
    {
        switch (prefType)
        {
            case PlayerPrefType.INT:
                return PlayerPrefs.GetInt(PrefsKey).ToString(CultureInfo.InvariantCulture);
            case PlayerPrefType.BOOL:
                return PlayerPrefs.GetInt(PrefsKey).ToString(CultureInfo.InvariantCulture);
            case PlayerPrefType.FLOAT:
                return PlayerPrefs.GetFloat(PrefsKey).ToString("R", CultureInfo.InvariantCulture);
            case PlayerPrefType.STRING:
                return PlayerPrefs.GetString(PrefsKey);
            default:
                return string.Empty;
        }
    }

    public enum PlayerPrefType
    {
        INT,
        FLOAT,
        STRING,
        BOOL,
    }

    public void SetBoolValue(bool value) => SetValue(value);
    public void SetFloatValue(float value) => SetValue(value);
    public void SetIntValue(int value) => SetValue(value);

    internal static void FromString(string item)
    {
        string[] elements = item.Split('/');
        Debug.Assert(elements.Length == 3, $"Unable to parse: {item}");
        // A failed parse used to fall through and store the zero it left behind, which is how a
        // save file written under another culture silenced the game: the three volume keys became
        // 0 without a word to the player. Keeping the value already in PlayerPrefs is always the
        // better answer, and the next Save rewrites the line properly.
        switch (elements[1])
        {
            case nameof(PlayerPrefType.INT):
                if (!int.TryParse(elements[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iValue))
                {
                    Debug.LogError($"Wrong int value: {elements[2]}, for key: {elements[0]} - keeping the current one.");
                    return;
                }
                PlayerPrefs.SetInt(elements[0], iValue);
                break;
            case nameof(PlayerPrefType.BOOL):
                if (!int.TryParse(elements[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bValue))
                {
                    Debug.LogError($"Wrong int value: {elements[2]}, for key: {elements[0]} - keeping the current one.");
                    return;
                }
                PlayerPrefs.SetInt(elements[0], bValue);
                break;
            case nameof(PlayerPrefType.FLOAT):
                if (!float.TryParse(elements[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float fValue))
                {
                    Debug.LogError($"Wrong float value: {elements[2]}, for key: {elements[0]} - keeping the current one.");
                    return;
                }
                PlayerPrefs.SetFloat(elements[0], fValue);
                break;
            case nameof(PlayerPrefType.STRING):
                PlayerPrefs.SetString(elements[0], elements[2]);
                break;
        }
        if (elements[0] == "chosenScenario")
        {
            Debug.Log(item);
        }
    }

    public void SetValue(object valueToSet)
    {
        switch (this.prefType)
        {
            case PlayerPrefType.INT:
                PlayerPrefs.SetInt(PrefsKey, (int)valueToSet);
                break;
            case PlayerPrefType.BOOL:
                PlayerPrefs.SetInt(PrefsKey, (bool)valueToSet ? 1 : 0);
                break;
            case PlayerPrefType.FLOAT:
                PlayerPrefs.SetFloat(PrefsKey, (float)valueToSet);
                break;
            case PlayerPrefType.STRING:
                PlayerPrefs.SetString(PrefsKey, valueToSet.ToString());
                break;
            default:
                break;
        }
        if (name == "chosenScenario")
        {
            Debug.Log(valueToSet);
        }
        OnValueChanged?.Invoke();
    }
}
