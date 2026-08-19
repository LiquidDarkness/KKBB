using System.Collections.Generic;
using UnityEngine;

// Reaches a setting by name, for the handful of components that exist in hundreds of copies and
// cannot each carry a serialized reference. Block sits on 848 objects in this project; wiring a
// field on every one of them - and on every block added later - is not a thing anyone would keep up.
//
// Everywhere else, keep using the serialized reference: it is visible in the Inspector, it survives
// a rename, and it does not depend on an asset staying in a particular folder.
public static class OptionSettings
{
    private const string Folder = "TypeDistinguishers/";

    private static readonly Dictionary<string, TypeDistinguisher> cache = new Dictionary<string, TypeDistinguisher>();

    public static TypeDistinguisher Find(string key)
    {
        TypeDistinguisher found;

        if (cache.TryGetValue(key, out found))
        {
            return found;
        }

        found = Resources.Load<TypeDistinguisher>(Folder + key);

        if (found == null)
        {
            Debug.LogWarning($"{nameof(OptionSettings)}: no setting called '{key}' under Resources/{Folder}");
        }

        // Cached either way. A missing setting is a wiring mistake, not something to re-report on
        // every block that breaks.
        cache[key] = found;
        return found;
    }

    public static bool IsOn(string key)
    {
        TypeDistinguisher setting = Find(key);
        return setting != null && setting.BoolValue;
    }

    // The one switch that turns every animation down at once. Named here so the string is not
    // spelled out in four different files.
    public const string ReduceMotion = "reduceMotion";

    public static bool MotionReduced => IsOn(ReduceMotion);
}
