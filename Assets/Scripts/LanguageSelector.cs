using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "NewLanguageSelector", menuName = "Custom/LanguageSelector")]
public class LanguageSelector : ScriptableObject
{
    public string currentLanguage = "EN"; // Domyœlnie ustawiony jêzyk
    public static event Action<string> OnLanguageSelected;

    private static LanguageSelector instance;

    public static LanguageSelector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<LanguageSelector>("LanguageSelector");
            }
            return instance;
        }
    }

    public void SetLanguage(string language)
    {
        currentLanguage = language;
    }

    public string GetLanguage()
    {
        return currentLanguage;
    }
}
