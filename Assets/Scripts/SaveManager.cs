using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "save.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasLoaded { get; private set; } = false;

    public static void Save()
    {
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
                // Disposed, unlike before: the handle stayed open for the rest of the run and the
                // first Save could then fail to open the same path for writing.
                File.Create(SaveFilePath).Dispose();
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

#if UNITY_EDITOR

    [UnityEditor.MenuItem("Debug/Show SaveFilePath")]
    public static void ShowSaveFilePath()
    {
        Debug.Log(SaveFilePath);
    }

#endif
}
