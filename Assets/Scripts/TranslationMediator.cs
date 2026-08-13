using UnityEngine;

public class TranslationMediator : MonoBehaviour
{
    public string key;
    public UnityStringEvent onTranslationSet;

    public void OnEnable()
    {
        //Debug.Log($"TranslationMediator enabled for key: {key}");
        TranslationJSONDeserializer.OnTransaltionUpdated += UpdateTranslation;
        UpdateTranslation();
    }

    public void UpdateTranslation()
    {
        if (TranslationJSONDeserializer.dataDictionary.ContainsKey(key))
        {
            string translation = TranslationJSONDeserializer.dataDictionary[key];
            //Debug.Log($"Updating translation for key: {key}, translation: {translation}");
            onTranslationSet.Invoke(translation);
        }
        else
        {
            Debug.LogWarning($"Key not found in storyDataDictionary: {key}");
#if UNITY_EDITOR
            onTranslationSet.Invoke(key);
#endif
        }
    }

    private void OnDisable()
    {
        //Debug.Log($"TranslationMediator disabled for key: {key}");
        TranslationJSONDeserializer.OnTransaltionUpdated -= UpdateTranslation;
    }
}
