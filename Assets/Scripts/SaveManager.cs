using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "save.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    static TypeDistinguishersManager manager = TypeDistinguishersManager.Instance;

    public static bool HasLoaded { get; private set; } = false;

    public static void Save()
    {
        /*
        if (!File.Exists(SaveFilePath))
        {
            File.Create(SaveFilePath).Dispose();
        }

        if (manager == null)
        {
            Debug.LogWarning("[SaveManager] No TypeDistinguishersManager found in scene. Nothing to save.");
            return;
        }

        Debug.Log($"[SaveManager] Saving {manager.typeDistinguishers?.Count ?? 0} distinguishers.");
        SaveToFile(manager.typeDistinguishers);
        */

        //Debug.Log(SaveFilePath);
        if (!File.Exists(SaveFilePath))
        {
            File.Create(SaveFilePath).Dispose();
        }

        var typeDistinguishers = Resources.LoadAll<TypeDistinguisher>("TypeDistinguishers");

        using (StreamWriter outputFile = new StreamWriter(SaveFilePath, false))
        {
            foreach (TypeDistinguisher item in typeDistinguishers)
            {
                outputFile.WriteLine(item.ToString());
            }
            outputFile.Close();
        }

        //Application.OpenURL($"file://{SaveFilePath}");
    }

    public static void Load()
    {
        if (HasLoaded) return;

        try
        {
            if (File.Exists(SaveFilePath))
            {
                foreach (var item in File.ReadAllLines(SaveFilePath))
                {
                    TypeDistinguisher.FromString(item);
                }
            }
            else
            {
                Debug.Log("Save file not found. Loading default TypeDistinguisher values...");
                File.Create(SaveFilePath);
                LoadDefaults();
            }
        }
        finally
        {
            HasLoaded = true;
        }

        //PlayerPrefs.Save();
        //Save();
    }

    public static void SaveToFile(IEnumerable<TypeDistinguisher> distinguishers)
    {
        using (StreamWriter outputFile = new StreamWriter(SaveFilePath, false))
        {
            foreach (TypeDistinguisher item in manager.typeDistinguishers)
            {
                outputFile.WriteLine(item.ToString());
                Debug.Log($"Save file path: {SaveFilePath}, item: {item.ToString()}");
            }
            outputFile.Close();
        }
        /*
        var export = distinguishers.Select(d => d.ToString()).ToArray();
        File.WriteAllLines(SaveFilePath, export);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] Saved {export.Length} entries to {SaveFilePath}");
        */
    }

    public static void LoadFromFile()
    {
        /*
        var lines = File.ReadAllLines(SaveFilePath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            TypeDistinguisher.FromString(line);
        }

        Debug.Log($"[SaveManager] Loaded {lines.Length} entries from {SaveFilePath}");
        */
        foreach (var item in File.ReadAllLines(SaveFilePath))
        {
            TypeDistinguisher.FromString(item);
        }
        //.Log($"[SaveManager] Loaded {lines.Length} entries from {SaveFilePath}");
    }

    public static void LoadDefaults()
    {
        var all = Resources.LoadAll<TypeDistinguisher>("TypeDistinguishers");
        foreach (var t in all)
        {
            switch (t.prefType)
            {
                case TypeDistinguisher.PlayerPrefType.INT:
                    PlayerPrefs.SetInt(t.PrefsKey, t.IntValue);
                    break;
                case TypeDistinguisher.PlayerPrefType.FLOAT:
                    PlayerPrefs.SetFloat(t.PrefsKey, t.FloatValue);
                    break;
                case TypeDistinguisher.PlayerPrefType.STRING:
                    PlayerPrefs.SetString(t.PrefsKey, t.StringValue);
                    break;
                case TypeDistinguisher.PlayerPrefType.BOOL:
                    PlayerPrefs.SetInt(t.PrefsKey, t.BoolValue ? 1 : 0);
                    break;
            }
        }

        Debug.Log($"[SaveManager] Loaded defaults for {all.Length} TypeDistinguishers.");
    }

    public static void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("Debug/Show SaveFilePath")]
    public static void ShowSaveFilePath()
    {
        Debug.Log(SaveFilePath);
    }

#endif
}
