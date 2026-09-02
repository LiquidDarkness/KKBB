using System.Globalization;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const string SaveFileName = "save.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // Bumped whenever a save written by an older build has to be put right on the way in. Version 1
    // is the first stamp there has ever been, so a file without one was written before settings had
    // defaults - which is the whole reason this exists. Written as the first line and marked with a
    // hash, so it can never be read as a setting: FromString expects three parts and would assert.
    private const int SaveFormatVersion = 1;
    private const string VersionPrefix = "#version ";

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
            outputFile.WriteLine(VersionPrefix + SaveFormatVersion.ToString(CultureInfo.InvariantCulture));

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
                string[] lines = File.ReadAllLines(SaveFilePath);

                foreach (var item in lines)
                {
                    // Anything marked with a hash is about the file rather than about a setting.
                    if (item.StartsWith("#"))
                    {
                        continue;
                    }

                    TypeDistinguisher.FromString(item);
                }

                if (VersionOf(lines) < SaveFormatVersion)
                {
                    Repair();
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

    // What version wrote this file. No stamp means it was written before there were any, which is
    // the state that needs putting right.
    public static int VersionOf(string[] lines)
    {
        if (lines == null)
        {
            return 0;
        }

        foreach (string line in lines)
        {
            if (!line.StartsWith(VersionPrefix))
            {
                continue;
            }

            if (int.TryParse(line.Substring(VersionPrefix.Length).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int version))
            {
                return version;
            }
        }

        return 0;
    }

    // Run once, on the way in, for a save with no version stamp. See
    // TypeDistinguisher.RepairIfStoredValueLooksUnwritten for what counts as needing repair and
    // what is deliberately left alone.
    private static void Repair()
    {
        var all = Resources.LoadAll<TypeDistinguisher>("TypeDistinguishers");
        int repaired = 0;

        foreach (var t in all)
        {
            if (t.RepairIfStoredValueLooksUnwritten())
            {
                repaired++;
            }
        }

        Debug.Log($"[{nameof(SaveManager)}] Save written before settings had defaults: {repaired} of {all.Length} put back to what they were authored with.");

        // Written out whatever the count, because the stamp itself has to land - otherwise every
        // start from here on would go looking for the same thing again.
        Save();
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
