using System;
using UnityEngine;

// Which language everything a player reads is written in.
//
// One asset in Resources, so the two things that need it can find it without a reference wired into
// every scene: the dropdown in the options window that offers the choice, and the deserializer that
// fills the dictionary from it. The choice itself is not kept here - a plain field on a
// ScriptableObject is written in the editor and thrown away in a build - but in a TypeDistinguisher
// like every other setting, so it reaches PlayerPrefs, save.json and Steam Cloud.
//
// An id has to be exactly the name of the TextAsset it stands for, because that is what
// TranslationJSONDeserializer matches against: EN, PL.
[CreateAssetMenu(fileName = "LanguageSelector", menuName = "Custom/LanguageSelector")]
public class LanguageSelector : ScriptableObject
{
    [Serializable]
    public class Language
    {
        [Tooltip("The name of the translation file this stands for, without the extension: EN, PL.")]
        public string id;

        [Tooltip("How the language is offered in the options, written in that language - English, Polski. Never translated: a player looking for their own language looks for its own name, not for what the language they cannot read calls it.")]
        public string displayName;

        [Tooltip("The system language this is the answer to, used once - on a machine that has never chosen. Unknown means it is never picked that way, which is what the fallback language wants.")]
        public SystemLanguage systemLanguage = SystemLanguage.Unknown;
    }

    [Tooltip("Every language the game is written in, in the order the options window offers them. The first one is what an unrecognised system language falls back to.")]
    public Language[] languages;

    [Tooltip("Where the choice is kept - an index into the list above. Blank default on purpose: nothing is written until either the player picks a language or the first start guesses one from the system, and 'nothing written yet' is what tells those two apart.")]
    public TypeDistinguisher setting;

    // Raised whenever the language changes, and by nothing else. The deserializer listens, reloads
    // the dictionary and raises OnTransaltionUpdated in its turn, which is what every label on
    // screen is already listening to.
    public static event Action<string> OnLanguageSelected;

    private const string ResourceName = "LanguageSelector";

    private static LanguageSelector instance;

    public static LanguageSelector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<LanguageSelector>(ResourceName);

                if (instance == null)
                {
                    Debug.LogError($"[{nameof(LanguageSelector)}] no asset called {ResourceName} under Resources - the game will stay in whatever the deserializer calls its default.");
                }
            }

            return instance;
        }
    }

    public int Count => languages == null ? 0 : languages.Length;

    // The language in force. Read after the save has been applied to PlayerPrefs - which is why
    // nothing asks for it in Awake - because the first read is also what settles the first start:
    // with nothing stored, the system language decides and the answer is written, so the guess is
    // made once and never again over a choice the player has since made.
    public static int CurrentIndex
    {
        get
        {
            LanguageSelector selector = Instance;

            if (selector == null || selector.Count == 0 || selector.setting == null)
            {
                return 0;
            }

            if (!selector.setting.HasStoredValue)
            {
                int guess = selector.IndexOfSystemLanguage();
                selector.setting.SetValue(guess);
                return guess;
            }

            return Mathf.Clamp(selector.setting.IntValue, 0, selector.Count - 1);
        }
    }

    public static string CurrentLanguageId
    {
        get
        {
            LanguageSelector selector = Instance;
            return selector == null || selector.Count == 0 ? string.Empty : selector.languages[CurrentIndex].id;
        }
    }

    // Written and announced, in that order, so nothing can redraw itself from a setting that has not
    // been stored yet. Saved on the spot as well: picking a language is a deliberate, rare act, and
    // a game closed from the menu straight afterwards would otherwise come back in the old one.
    public static void Select(int index)
    {
        LanguageSelector selector = Instance;

        if (selector == null || selector.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, selector.Count - 1);

        if (selector.setting != null)
        {
            selector.setting.SetValue(index);
            SaveManager.Save();
        }

        OnLanguageSelected?.Invoke(selector.languages[index].id);
    }

    public static void Select(string id)
    {
        LanguageSelector selector = Instance;

        if (selector == null)
        {
            return;
        }

        for (int i = 0; i < selector.Count; i++)
        {
            if (selector.languages[i].id == id)
            {
                Select(i);
                return;
            }
        }

        Debug.LogWarning($"[{nameof(LanguageSelector)}] nothing here is called '{id}' - the language was left as it was.");
    }

    // First start only. Anything the list does not claim gets the first entry, which is the language
    // the text files are written in and the one every key falls back to.
    private int IndexOfSystemLanguage()
    {
        SystemLanguage system = Application.systemLanguage;

        for (int i = 0; i < Count; i++)
        {
            if (languages[i].systemLanguage != SystemLanguage.Unknown && languages[i].systemLanguage == system)
            {
                return i;
            }
        }

        return 0;
    }
}
