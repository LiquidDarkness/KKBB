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

    public void Awake()
    {
        LanguageSelector.OnLanguageSelected += SelectLanguage;
    }

    public void Start()
    {
        // Load default translation initially
        SelectLanguage("default");
    }

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


//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//public class StoryJSONDeserializer : MonoBehaviour
//{
//    public TextAsset[] translations;
//    public TextAsset defaultTranslation;
//    public static Dictionary<string, string> storyDataDictionary = new();

//    public static event Action OnTransaltionUpdated;

//    public void Start()
//    {
//        LanguageSelector.OnLanguageSelected += SelectLanguage;
//    }

//    private void SelectLanguage(string language)
//    {
//        foreach (var item in translations)
//        {
//            if (item.name != language)
//            {
//                continue;
//            }
//            LoadStory(item);
//            return;
//        }
//        LoadStory(defaultTranslation);
//    }

//    public void LoadStory(TextAsset rawTranslation)
//    {
//        storyDataDictionary.Clear();

//        string json = rawTranslation.text;

//        // Deserializacja JSON do obiektu StoryContainer
//        StoryContainerJSON storyJSON = JsonConvert.DeserializeObject<StoryContainerJSON>(json);
//        foreach (var item in storyJSON.stories)
//        {
//            storyDataDictionary.Add(item.key, item.value);
//        }

//        Debug.Log(storyJSON);
//        OnTransaltionUpdated.Invoke();
//    }
//}
