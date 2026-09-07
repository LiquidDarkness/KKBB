using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Checks that picking a language actually changes what is on screen.
//
// It never did. Nothing called SetLanguage, SetLanguage told nobody, the deserializer asked for a
// language called "default" every single start, Instance looked for an asset under a name nothing
// was called, the choice was kept on a field a build throws away, and only EN.json was ever handed
// to the thing that reads them. Six faults, none of which anything would have noticed, because a
// switch that does nothing looks exactly like a switch nobody has touched.
//
// So the test walks the whole road: choose Polish, and the dictionary every label on screen reads
// from has Polish in it; choose English, and it has English again. The last test is for the months
// of translating ahead rather than for the code - it fails the moment a key is written into one
// file and not the other, which is the one mistake that shows up as a label reading its own key.
//
// The real setting is written to, so the real save file is put back afterwards, byte for byte.
public class LanguageSwitchTests
{
    private const string EnglishPath = "Assets/Stories/texts/EN.json";
    private const string PolishPath = "Assets/Stories/texts/PL.json";

    // A key that exists in both files and whose two sides differ, so "did the language change" has
    // an answer that is not the same string twice.
    private const string SampleKey = "Endless";

    private GameObject host;
    private TranslationJSONDeserializer deserializer;

    private string savePath;
    private byte[] saveBefore;
    private bool hadStoredLanguage;
    private int storedLanguage;

    [SetUp]
    public void Setup()
    {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");
        saveBefore = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;

        TypeDistinguisher setting = LanguageSelector.Instance.setting;
        hadStoredLanguage = setting.HasStoredValue;
        storedLanguage = setting.IntValue;

        // Edit mode runs no lifecycle of its own, so the two halves the deserializer needs are
        // called by hand: OnEnable to get it onto the event, Start to fill the dictionary once.
        host = new GameObject("LanguageSwitchTests");
        deserializer = host.AddComponent<TranslationJSONDeserializer>();
        deserializer.translations = new[] { Load(EnglishPath), Load(PolishPath) };
        deserializer.defaultTranslation = deserializer.translations[0];
        deserializer.OnEnable();
        deserializer.Start();
    }

    [TearDown]
    public void CleanUp()
    {
        deserializer.OnDisable();
        Object.DestroyImmediate(host);

        TypeDistinguisher setting = LanguageSelector.Instance.setting;

        if (hadStoredLanguage)
        {
            PlayerPrefs.SetInt(setting.PrefsKey, storedLanguage);
        }
        else
        {
            PlayerPrefs.DeleteKey(setting.PrefsKey);
        }

        PlayerPrefs.Save();

        // The file rather than a snapshot in memory: choosing a language saves, and what it saved
        // is not this project's business.
        if (saveBefore != null)
        {
            File.WriteAllBytes(savePath, saveBefore);
        }
        else if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    [Test]
    public void ChoosingPolishPutsPolishInTheDictionary()
    {
        Dictionary<string, string> english = Read(EnglishPath);
        Dictionary<string, string> polish = Read(PolishPath);

        Assert.AreNotEqual(english[SampleKey], polish[SampleKey],
            "The sample key reads the same in both files, so it cannot tell them apart - pick another one.");

        LanguageSelector.Select("PL");
        Assert.AreEqual(polish[SampleKey], TranslationJSONDeserializer.dataDictionary[SampleKey],
            "Polish was chosen and the dictionary is still not Polish.");

        LanguageSelector.Select("EN");
        Assert.AreEqual(english[SampleKey], TranslationJSONDeserializer.dataDictionary[SampleKey],
            "English was chosen back and the dictionary stayed where it was.");
    }

    [Test]
    public void TheChoiceIsRememberedForTheNextStart()
    {
        LanguageSelector.Select("PL");
        Assert.AreEqual("PL", LanguageSelector.CurrentLanguageId, "The choice did not reach the setting.");

        // What a fresh start does: read the stored language and load that file, with nobody having
        // raised an event in the meantime.
        TranslationJSONDeserializer.dataDictionary.Clear();
        deserializer.Start();

        Assert.AreEqual(Read(PolishPath)[SampleKey], TranslationJSONDeserializer.dataDictionary[SampleKey],
            "A start after Polish was chosen came up in something else.");
    }

    [Test]
    public void EveryLineInOneFileHasOneInTheOther()
    {
        Dictionary<string, string> english = Read(EnglishPath);
        Dictionary<string, string> polish = Read(PolishPath);

        var missingFromPolish = new List<string>();
        var missingFromEnglish = new List<string>();

        foreach (string key in english.Keys)
        {
            if (!polish.ContainsKey(key))
            {
                missingFromPolish.Add(key);
            }
        }

        foreach (string key in polish.Keys)
        {
            if (!english.ContainsKey(key))
            {
                missingFromEnglish.Add(key);
            }
        }

        Assert.IsEmpty(missingFromPolish, "PL.json has no line for: " + string.Join(", ", missingFromPolish));
        Assert.IsEmpty(missingFromEnglish, "EN.json has no line for: " + string.Join(", ", missingFromEnglish));
    }

    private static TextAsset Load(string path)
    {
        var file = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        Assert.IsNotNull(file, "There is no translation file at " + path);
        return file;
    }

    private static Dictionary<string, string> Read(string path)
    {
        var container = JsonConvert.DeserializeObject<TranslationContainerJSON>(Load(path).text);
        var lines = new Dictionary<string, string>();

        foreach (TranslationJSON line in container.translations)
        {
            lines[line.key] = line.value;
        }

        return lines;
    }
}
