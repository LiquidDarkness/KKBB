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
            }

            // After the file, not only in place of it. A save written before a setting existed is
            // silent about it, and the setting would otherwise start from the zero PlayerPrefs
            // hands back for a key nobody wrote - which for a switch reads as the player having
            // turned it off.
            LoadDefaults();
        }
        finally
        {
            HasLoaded = true;
        }

        //PlayerPrefs.Save();
        //Save();
    }

    // Gives every setting that has a default and nothing saved the value it was authored with.
    // Anything the player has already set is left exactly as they left it, so this is safe to run
    // on every start - and it has to run on every start, because a setting can be added to the
    // game long after a save file was written.
    //
    // It used to write each key back the value PlayerPrefs already held, which for an untouched
    // install meant writing zeros over zeros: the log line said defaults had been loaded while
    // nothing of the kind had happened.
    public static void LoadDefaults()
    {
        var all = Resources.LoadAll<TypeDistinguisher>("TypeDistinguishers");
        int seeded = 0;

        foreach (var t in all)
        {
            if (t.ApplyDefaultIfUnset())
            {
                seeded++;
            }
        }

        if (seeded == 0)
        {
            return;
        }

        Debug.Log($"[{nameof(SaveManager)}] Seeded {seeded} of {all.Length} settings from their defaults.");

        // Written straight back out, so the next run reads them from the file instead of seeding
        // them again, and so a setting added today cannot later look like one the player turned off.
        Save();
    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("Debug/Show SaveFilePath")]
    public static void ShowSaveFilePath()
    {
        Debug.Log(SaveFilePath);
    }

#endif
}
