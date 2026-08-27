using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// The best score each scenario has ever been finished with, on each difficulty - finishing
// ZiggiesMunda on Easy has nothing to say about finishing it on METAL, and the two should not be
// competing for one number.
//
// All of it lives in one setting rather than one per scenario or, worse, one per pair: a scenario
// or a difficulty added later needs no new asset and the save file gains no new line. The format is
// the one Controls already uses for the key bindings - name=value;name=value - which the save file
// can carry, holding neither a slash nor a newline. The name half is scenario|difficulty.
public static class ScenarioRecords
{
    private const char EntrySeparator = ';';
    private const char ValueSeparator = '=';
    private const char PairSeparator = '|';

    public static string Key(string scenario, string difficulty)
    {
        return scenario + PairSeparator + difficulty;
    }

    public static Dictionary<string, int> Read(TypeDistinguisher setting)
    {
        var records = new Dictionary<string, int>();
        string stored = setting != null ? setting.StringValue : string.Empty;

        if (string.IsNullOrEmpty(stored))
        {
            return records;
        }

        foreach (string entry in stored.Split(EntrySeparator))
        {
            string[] halves = entry.Split(ValueSeparator);

            if (halves.Length != 2 || string.IsNullOrEmpty(halves[0]))
            {
                continue;
            }

            // Invariant, like everything else that crosses machines through the save file: a run
            // recorded on a Polish Windows has to read back the same on an English one.
            if (int.TryParse(halves[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int score))
            {
                records[halves[0]] = score;
            }

            // An entry nobody can read is dropped rather than guessed at. The rest are still worth
            // keeping, and the next write puts the whole thing back out clean.
        }

        return records;
    }

    public static int Best(TypeDistinguisher setting, string scenario, string difficulty)
    {
        return Read(setting).TryGetValue(Key(scenario, difficulty), out int best) ? best : 0;
    }

    // True when this run beat what was already there, which is what the ending screen says out loud.
    public static bool TrySetBest(TypeDistinguisher setting, string scenario, string difficulty, int score)
    {
        if (setting == null || string.IsNullOrEmpty(scenario) || string.IsNullOrEmpty(difficulty))
        {
            Debug.LogWarning($"[{nameof(ScenarioRecords)}] No setting, no scenario or no difficulty - the score was not recorded.");
            return false;
        }

        if (!IsNameUsable(scenario) || !IsNameUsable(difficulty))
        {
            // Renaming an asset to something with a separator in it would quietly corrupt every
            // record stored beside it, so it is refused here rather than discovered later.
            Debug.LogError($"[{nameof(ScenarioRecords)}] '{scenario}' / '{difficulty}': a name holds one of {EntrySeparator} {ValueSeparator} {PairSeparator} - rename the asset, or the record cannot be stored.");
            return false;
        }

        Dictionary<string, int> records = Read(setting);
        string key = Key(scenario, difficulty);

        if (records.TryGetValue(key, out int best) && best >= score)
        {
            return false;
        }

        records[key] = score;
        Write(setting, records);
        return true;
    }

    private static bool IsNameUsable(string name)
    {
        return name.IndexOf(EntrySeparator) < 0 && name.IndexOf(ValueSeparator) < 0 && name.IndexOf(PairSeparator) < 0;
    }

    private static void Write(TypeDistinguisher setting, Dictionary<string, int> records)
    {
        var text = new StringBuilder();

        foreach (KeyValuePair<string, int> record in records)
        {
            if (text.Length > 0)
            {
                text.Append(EntrySeparator);
            }

            text.Append(record.Key).Append(ValueSeparator).Append(record.Value.ToString(CultureInfo.InvariantCulture));
        }

        setting.SetValue(text.ToString());
        SaveManager.Save();
    }
}
