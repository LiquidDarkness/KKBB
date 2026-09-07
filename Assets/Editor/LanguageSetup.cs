using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builder for the language switch: the setting that remembers the choice, the asset that says which
// languages there are, and the places that have to be told about it.
//
// The switch had never worked, and it was not one fault but six. Nothing called SetLanguage and
// SetLanguage announced nothing; the deserializer always asked for "default" and only ever knew
// about EN.json; the choice lived on a plain ScriptableObject field, which a build throws away;
// Instance looked for an asset under a name nothing was called; and the deserializer existed twice,
// once in a scene that dies and takes nothing off the static event with it. The code half is fixed
// in LanguageSelector and TranslationJSONDeserializer; this is the wiring half.
//
// Written as an editor tool rather than done by hand for the same reason as the setups next to it -
// Unity keeps every fileID and reference straight, and it can be run again after a change without
// making a second of anything. Every step looks for what it would build before building it.
public static class LanguageSetup
{
    private const string SettingPath = "Assets/Resources/TypeDistinguishers/language.asset";
    private const string SelectorPath = "Assets/Resources/LanguageSelector.asset";
    private const string OldSelectorPath = "Assets/Resources/NewLanguageSelector.asset";

    private const string TextsFolder = "Assets/Stories/texts/";
    private const string OptionsPrefabPath = "Assets/Prefabs/Menu/OptionsBG.prefab";
    private const string GameSessionPrefabPath = "Assets/Prefabs/GameSession.prefab";
    private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";

    // The header, and the row under it that carries the dropdown. Both were parked while EN.json was
    // the only file anything read.
    private const string HeaderRowName = "Language";
    private const string TextRowName = "Text";

    private class Entry
    {
        public string Id;
        public string Name;
        public SystemLanguage System;
    }

    // In the order the options window offers them. The first is the language every key is written
    // in and the one anything unrecognised falls back to.
    private static readonly Entry[] Languages =
    {
        new Entry { Id = "EN", Name = "English", System = SystemLanguage.English },
        new Entry { Id = "PL", Name = "Polski", System = SystemLanguage.Polish },
    };

    [MenuItem("Debug/Language - wire up the language switch", priority = 130)]
    public static void RunAll()
    {
        TypeDistinguisher setting = EnsureSetting();
        LanguageSelector selector = EnsureSelector(setting);

        if (selector == null)
        {
            return;
        }

        WireDeserializer(selector);
        RemoveDuplicateDeserializer();
        WireOptionsRow(selector);

        AssetDatabase.SaveAssets();
        Debug.Log("[LanguageSetup] done.");
    }

    private static TypeDistinguisher EnsureSetting()
    {
        var setting = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(SettingPath);

        if (setting == null)
        {
            setting = ScriptableObject.CreateInstance<TypeDistinguisher>();
            AssetDatabase.CreateAsset(setting, SettingPath);
            Debug.Log("[LanguageSetup] made the setting that remembers the language.");
        }

        setting.prefType = TypeDistinguisher.PlayerPrefType.INT;

        // Kept through a New Game, like every other setting: which language someone reads in is not
        // progress to be wiped.
        setting.purgable = false;

        // Deliberately blank. A default would be seeded on the first start, and "nothing has been
        // written yet" is exactly what tells a machine that has never chosen from one that has -
        // which is how LanguageSelector knows it may take the system language as its first guess.
        setting.defaultValue = string.Empty;

        EditorUtility.SetDirty(setting);
        return setting;
    }

    private static LanguageSelector EnsureSelector(TypeDistinguisher setting)
    {
        var selector = AssetDatabase.LoadAssetAtPath<LanguageSelector>(SelectorPath);

        if (selector == null)
        {
            // The same asset under the name Instance actually looks for. It was called
            // NewLanguageSelector, so Resources.Load of "LanguageSelector" came back null every time
            // it was ever asked.
            var old = AssetDatabase.LoadAssetAtPath<LanguageSelector>(OldSelectorPath);

            if (old != null)
            {
                string moved = AssetDatabase.MoveAsset(OldSelectorPath, SelectorPath);

                if (!string.IsNullOrEmpty(moved))
                {
                    Debug.LogError("[LanguageSetup] could not rename " + OldSelectorPath + ": " + moved);
                    return null;
                }

                Debug.Log("[LanguageSetup] renamed NewLanguageSelector to the name Instance looks for.");
                selector = AssetDatabase.LoadAssetAtPath<LanguageSelector>(SelectorPath);
            }
            else
            {
                selector = ScriptableObject.CreateInstance<LanguageSelector>();
                AssetDatabase.CreateAsset(selector, SelectorPath);
                Debug.Log("[LanguageSetup] made the language list.");
            }
        }

        var entries = new List<LanguageSelector.Language>();

        foreach (Entry language in Languages)
        {
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(TextsFolder + language.Id + ".json") == null)
            {
                Debug.LogError("[LanguageSetup] there is no " + language.Id + ".json under " + TextsFolder
                    + " - the language was left out of the list.");
                continue;
            }

            entries.Add(new LanguageSelector.Language
            {
                id = language.Id,
                displayName = language.Name,
                systemLanguage = language.System,
            });
        }

        selector.languages = entries.ToArray();
        selector.setting = setting;
        EditorUtility.SetDirty(selector);
        return selector;
    }

    // Every language file, not only the one the game shipped reading. The default stays the first
    // entry, which is what a language nobody wrote a file for falls back to.
    private static void WireDeserializer(LanguageSelector selector)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameSessionPrefabPath);

        try
        {
            var deserializer = root.GetComponentInChildren<TranslationJSONDeserializer>(true);

            if (deserializer == null)
            {
                Debug.LogError("[LanguageSetup] no TranslationJSONDeserializer on " + GameSessionPrefabPath);
                return;
            }

            var files = new List<TextAsset>();

            foreach (LanguageSelector.Language language in selector.languages)
            {
                var file = AssetDatabase.LoadAssetAtPath<TextAsset>(TextsFolder + language.id + ".json");

                if (file != null)
                {
                    files.Add(file);
                }
            }

            if (files.Count == 0)
            {
                Debug.LogError("[LanguageSetup] not one translation file was found - the deserializer was left as it was.");
                return;
            }

            deserializer.translations = files.ToArray();
            deserializer.defaultTranslation = files[0];

            PrefabUtility.SaveAsPrefabAsset(root, GameSessionPrefabPath);
            Debug.Log("[LanguageSetup] the deserializer on GameSession now knows about " + files.Count + " language files.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // GameSession carries DontDestroyOnLoad and its deserializer outlives every scene, so the copy
    // sitting in Gameplay was never anything but a second one - and a second one that dies with the
    // scene while still on a static event, which is the standing way to silence a whole chain.
    private static void RemoveDuplicateDeserializer()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        TranslationJSONDeserializer[] found = Object.FindObjectsOfType<TranslationJSONDeserializer>(true);

        if (found.Length == 0)
        {
            return;
        }

        foreach (TranslationJSONDeserializer copy in found)
        {
            Debug.Log("[LanguageSetup] took the spare deserializer off " + copy.gameObject.name + " in " + GameplayScenePath + ".");
            Object.DestroyImmediate(copy, true);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void WireOptionsRow(LanguageSelector selector)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(OptionsPrefabPath);

        try
        {
            Transform content = root.transform.Find("Options/Scroll View/Viewport/Content");

            if (content == null)
            {
                Debug.LogError("[LanguageSetup] the options window is not shaped the way this expects.");
                return;
            }

            Transform header = FindRow(content, HeaderRowName);
            Transform row = FindRow(content, TextRowName);

            if (header == null || row == null)
            {
                Debug.LogError("[LanguageSetup] the language rows are missing from the options window - run the options builder first.");
                return;
            }

            var dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);

            if (dropdown == null)
            {
                Debug.LogError("[LanguageSetup] the " + TextRowName + " row has no dropdown to drive.");
                return;
            }

            // The list is the languages' own names, filled from the asset at runtime, so neither of
            // the two components that write into a dropdown belongs here: DropdownSetter would put
            // an index back without telling anything, and DropdownOptionTranslator would translate
            // Polski into whatever the current language calls Polish.
            Remove(dropdown.GetComponent<DropdownSetter>());
            Remove(dropdown.GetComponent<DropdownOptionTranslator>());

            var driver = dropdown.GetComponent<LanguageDropdown>();

            if (driver == null)
            {
                driver = dropdown.gameObject.AddComponent<LanguageDropdown>();
            }

            driver.dropdown = dropdown;
            driver.selector = selector;

            // What this row was carrying was the resolution dropdown's wiring, copied along with the
            // row it was cloned from and never cleaned up because the row was switched off: picking
            // a language would have changed the screen resolution. The click it also carried is
            // worth keeping, and it plays first, the way it does on every other dropdown here.
            while (dropdown.onValueChanged.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(dropdown.onValueChanged, 0);
            }

            var click = dropdown.GetComponent<AudioSource>();

            if (click != null)
            {
                UnityEventTools.AddVoidPersistentListener(dropdown.onValueChanged, click.Play);
            }

            UnityEventTools.AddPersistentListener(dropdown.onValueChanged, driver.Choose);

            // The entries as they will be at runtime, so the row reads properly in the Inspector and
            // in a scene view too.
            var names = new List<string>();

            foreach (LanguageSelector.Language language in selector.languages)
            {
                names.Add(language.displayName);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(names);
            dropdown.RefreshShownValue();

            Show(header);
            Show(row);

            PrefabUtility.SaveAsPrefabAsset(root, OptionsPrefabPath);
            Debug.Log("[LanguageSetup] the language row is on and offering " + names.Count + " languages.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Show(Transform row)
    {
        if (!row.gameObject.activeSelf)
        {
            row.gameObject.SetActive(true);
        }
    }

    private static void Remove(Component component)
    {
        if (component != null)
        {
            Object.DestroyImmediate(component, true);
        }
    }

    // The rows sit in a group each rather than straight under Content, so a row is looked for one
    // level down as well.
    private static Transform FindRow(Transform content, string name)
    {
        foreach (Transform child in content)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform found = child.Find(name);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
