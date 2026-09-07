using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TranslationJSONDeserializer : MonoBehaviour
{
    public TextAsset[] translations;
    public TextAsset defaultTranslation;
    public static Dictionary<string, string> dataDictionary = new();

    public static event Action OnTransaltionUpdated;

    // Subscribed here rather than in Awake, and dropped again, because this is a static event on a
    // class that outlives every scene: a copy of this that dies with a scene without letting go is
    // called on a destroyed object, and the exception stops every handler queued behind it.
    public void OnEnable()
    {
        LanguageSelector.OnLanguageSelected += SelectLanguage;
    }

    public void OnDisable()
    {
        LanguageSelector.OnLanguageSelected -= SelectLanguage;
    }

    // In Start, not in OnEnable: the stored language is read out of PlayerPrefs, and PlayerPrefs is
    // not what the player chose until SaveManager.Load has applied save.json to it - which happens
    // in an Awake. Asking any earlier reads an empty setting and would take the first start's guess
    // over a choice that has been on file for months.
    public void Start()
    {
        SelectLanguage(LanguageSelector.CurrentLanguageId);
    }

    // The id is matched against the names of the TextAssets in the list, so it has to be spelled
    // the way the file is - EN, PL. Anything else falls back to the default file, which is the one
    // every key is written in.
    private void SelectLanguage(string language)
    {
        foreach (var item in translations)
        {
            if (item.name != language)
            {
                continue;
            }
            LoadStory(item);
            return;
        }
        LoadStory(defaultTranslation);
    }

    public void LoadStory(TextAsset rawTranslation)
    {
        if (rawTranslation == null)
        {
            Debug.LogError("rawTranslation is null");
            return;
        }

        dataDictionary.Clear();

        string json = rawTranslation.text;
        //Debug.Log($"Loaded JSON: {json}");

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("JSON text is empty or null");
            return;
        }

        try
        {
            // Deserializacja JSON do obiektu StoryContainer
            TranslationContainerJSON storyJSON = JsonConvert.DeserializeObject<TranslationContainerJSON>(json);

            if (storyJSON == null)
            {
                Debug.LogError("Deserialized object is null");
                return;
            }

            //Debug.Log("Stories count: " + (storyJSON.translations?.Count ?? 0));

            if (storyJSON.translations == null)
            {
                Debug.LogError("Stories list is null");
                return;
            }

            foreach (var item in storyJSON.translations)
            {
                if (!dataDictionary.ContainsKey(item.key))
                {
                    dataDictionary.Add(item.key, item.value);
                    //Debug.Log($"Added story: {item.key} = {item.value}");
                }
                else
                {
                    Debug.LogWarning($"Duplicate key found: {item.key}");
                }
            }

            //Debug.Log("Story data loaded successfully");
            OnTransaltionUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during deserialization: {e}");
        }
    }
}
