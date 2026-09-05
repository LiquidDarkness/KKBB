using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Every Steam achievement the game knows about, and the one way of earning one.
//
// The names below are the API Names from the Steamworks partner site - the ids, not the titles the
// player reads. They are written down once here and referred to by constant everywhere else, so a
// typo fails to compile instead of failing in silence on someone else's machine.
//
// How earning works, and why it is in two halves:
//
// The demo and the full game are separate app ids with separate achievement lists, so an unlock in
// the demo does not travel to the full game by itself - Valve's own advice is to leave achievements
// out of a demo entirely and grant them after the purchase instead. This does both. Earning is
// first written into the save, which is the same save either build reads: same company and product
// name, so the same folder on disk and the same Steam Cloud. Steam is then told separately, and
// only about achievements its own app id actually has.
//
// So a player who earns something in the demo keeps it in the demo, and gets it again in the full
// game the moment they first run it - PushEarned catches up everything the save knows about. That
// also covers the player who played with Steam closed, and the one who moved to another machine
// and brought the cloud save with them.
public static class Achievements
{
    // --- Scenarios -----------------------------------------------------------------------------
    public const string TutorialFinished = "TUTORIAL_FINISHED";
    public const string LiquidDarknessFinished = "LIQUID_DARKNESS_FINISHED";
    public const string SimbaBimbaFinished = "SIMBA_BIMBA_FINISHED";
    public const string ZiggiesMundaFinished = "ZIGGIES_MUNDA_FINISHED";

    // --- Playing well --------------------------------------------------------------------------
    // Hard or harder, and it has to hold for the whole scenario - dropping the difficulty part way
    // through breaks the attempt.
    public const string ScenarioOnHard = "SCENARIO_ON_HARD";
    public const string LevelUnscathed = "LEVEL_UNSCATHED";
    public const string ScenarioUnscathed = "SCENARIO_UNSCATHED";
    public const string NineHearts = "NINE_HEARTS";
    public const string SpeedStreakOnHard = "SPEED_STREAK_ON_HARD";

    // --- Endless -------------------------------------------------------------------------------
    // Measured in waves reached rather than survived: the card for a wave goes up when the player
    // has cleared everything before it, so being shown wave ten is having got there.
    public const string EndlessWaveFive = "ENDLESS_WAVE_FIVE";
    public const string EndlessZiggyOnMetal = "ENDLESS_ZIGGY_ON_METAL_WAVE_TEN";
    public const string EndlessSimbaOnEasy = "ENDLESS_SIMBA_ON_EASY_WAVE_FIFTEEN";

    // The one that asks how cleanly rather than how far: any cat, any difficulty, and not a
    // single continue bought since the run began.
    public const string EndlessWithoutContinues = "ENDLESS_NO_CONTINUES_WAVE_TEN";

    // --- Playing at all ------------------------------------------------------------------------
    public const string TutorialGameOver = "TUTORIAL_GAME_OVER";
    public const string FirstPurchase = "FIRST_PURCHASE";
    public const string BoughtAContinue = "BOUGHT_A_CONTINUE";
    public const string SpeedBoughtInZiggiesMunda = "SPEED_BOUGHT_IN_ZIGGIES_MUNDA";
    public const string FiveHundredDrops = "FIVE_HUNDRED_DROPS";

    // Which achievements exist on which app id. An entry marked for the demo has to be entered on
    // the demo's own partner page as well as the full game's, under exactly this api name; one that
    // is not marked is never sent while a demo build is running.
    //
    // A new scenario adds a line here and a line on the partner site, and nothing else changes. Do
    // not repurpose a name that has shipped: players who earned it keep it, so changing what it
    // means splits it into two different achievements wearing the same badge.
    private static readonly Dictionary<string, bool> Register = new Dictionary<string, bool>
    {
        { TutorialFinished, true },
        { LiquidDarknessFinished, true },
        { SimbaBimbaFinished, false },
        { ZiggiesMundaFinished, false },
        { ScenarioOnHard, false },
        { LevelUnscathed, false },
        { ScenarioUnscathed, true },
        { NineHearts, false },
        { SpeedStreakOnHard, false },
        { TutorialGameOver, true },
        { FirstPurchase, true },
        { BoughtAContinue, false },
        { SpeedBoughtInZiggiesMunda, false },
        { FiveHundredDrops, false },
        // Endless is playable in the demo, so these can be earned there - and they are, into the
        // save, which is what carries them into the full game on its first run. They are marked as
        // the full game's alone because a name marked for the demo has to be entered on the demo's
        // own partner page too, and that is a decision rather than an oversight.
        { EndlessWaveFive, false },
        { EndlessZiggyOnMetal, false },
        { EndlessSimbaOnEasy, false },
        { EndlessWithoutContinues, false },
    };

    // The save holds the earned names in one entry, the way the key bindings do - one asset rather
    // than one per achievement, and it survives a New Game because the asset is not purgable.
    private const string SaveKey = "achievementsEarned";
    private const char Separator = ',';

    private static TypeDistinguisher store;

    private static TypeDistinguisher Store
    {
        get
        {
            if (store == null)
            {
                store = Resources.Load<TypeDistinguisher>("TypeDistinguishers/" + SaveKey);

                if (store == null)
                {
                    Debug.LogError($"[Achievements] missing Resources/TypeDistinguishers/{SaveKey}.asset - nothing can be recorded.");
                }
            }

            return store;
        }
    }

    // Whether Steam is there at all. Never true in the editor without a steam_appid.txt beside the
    // project, and never true in a build the player launched outside Steam.
    //
    // NO_STEAM makes it never true at all: see SteamInitializer for what that define is for.
    // Every call this class makes to Steam goes through here, so switching it off switches all of
    // them off - earning still happens, it is written to the save, and nothing is sent.
    public static bool IsSteamAvailable =>
#if NO_STEAM
        false;
#else
        Steamworks.SteamClient.IsValid;
#endif

    // --- Earning -------------------------------------------------------------------------------

    public static void Unlock(string apiName)
    {
        if (!IsKnown(apiName))
        {
            return;
        }

        if (Record(apiName))
        {
            SaveManager.Save();
        }

        Push(apiName);
    }

    public static bool IsEarned(string apiName)
    {
        return Earned().Contains(apiName);
    }

    // Every name the save knows about, in the order they were earned.
    public static IEnumerable<string> Earned()
    {
        string raw = Store == null ? string.Empty : Store.StringValue;

        return string.IsNullOrEmpty(raw)
            ? Enumerable.Empty<string>()
            : raw.Split(Separator).Where(name => !string.IsNullOrEmpty(name));
    }

    // For the collect-them-all kind: an achievement that asks for a named set of others. Spell the
    // set out at the call site rather than asking for "all of them" - the list of scenarios grows,
    // and an achievement that quietly grows with it is a different achievement from the one players
    // already earned.
    public static bool AllEarned(params string[] apiNames)
    {
        return apiNames != null && apiNames.Length > 0 && apiNames.All(IsEarned);
    }

    // Hands Steam everything the save already knows about. Call it once the stats have arrived -
    // Steam cannot be told anything before that - and it is what carries a demo player's earnings
    // into the full game on their first run.
    public static void PushEarned()
    {
        if (!IsSteamAvailable)
        {
            return;
        }

        foreach (string apiName in Earned().ToList())
        {
            Push(apiName);
        }
    }

    // --- The two halves ------------------------------------------------------------------------

    // Returns true when this is the first time.
    private static bool Record(string apiName)
    {
        if (Store == null || IsEarned(apiName))
        {
            return false;
        }

        List<string> earned = Earned().ToList();
        earned.Add(apiName);
        Store.SetValue(string.Join(Separator.ToString(), earned));

        Debug.Log($"[Achievements] earned '{apiName}'.");
        return true;
    }

    private static void Push(string apiName)
    {
        if (!IsOnThisAppId(apiName) || !IsSteamAvailable)
        {
            return;
        }

#if !NO_STEAM
        // IsSteamAvailable already answers no under NO_STEAM, so nothing below would run - but the
        // type still has to disappear, not merely go unreached. Where both Facepunch assemblies end
        // up referenced, which is every platform their Windows filters do not cover, naming a type
        // they share is ambiguous and the build stops there.
        try
        {
            var achievement = new Steamworks.Data.Achievement(apiName);

            if (achievement.State)
            {
                return;
            }

            // Trigger stores the stats as it goes, which is what makes the notification appear.
            achievement.Trigger();
            Debug.Log($"[Achievements] Steam unlocked '{apiName}'.");
        }
        catch (System.Exception e)
        {
            // An api name that is not on the partner site throws here. Never worth taking the game
            // down over - the player would lose a run to a missing line on a website.
            Debug.LogWarning($"[Achievements] could not unlock '{apiName}': {e.Message}");
        }
#endif
    }

    private static bool IsOnThisAppId(string apiName)
    {
#if DEMO_BUILD
        return Register.TryGetValue(apiName, out bool inDemo) && inDemo;
#else
        return Register.ContainsKey(apiName);
#endif
    }

    private static bool IsKnown(string apiName)
    {
        if (string.IsNullOrEmpty(apiName))
        {
            return false;
        }

        if (!Register.ContainsKey(apiName))
        {
            Debug.LogWarning($"[Achievements] '{apiName}' is not in the register - add it there first.");
            return false;
        }

        // The save file is a line per setting, split on '/', so a name carrying one would break
        // every setting written after it. Nothing in the register does, but the register is edited
        // by hand every time a scenario is added.
        if (apiName.IndexOf('/') >= 0 || apiName.IndexOf(Separator) >= 0)
        {
            Debug.LogError($"[Achievements] '{apiName}' carries a separator the save file cannot.");
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    // Wipes the lot - the save's record and Steam's - so the unlocks can be watched happening
    // again. Editor only, and Steam's half needs Steam running with a steam_appid.txt beside the
    // project.
    [UnityEditor.MenuItem("Debug/Steam - clear all achievements")]
    private static void ClearAll()
    {
        if (Store != null)
        {
            Store.SetValue(string.Empty);
            SaveManager.Save();
        }

        if (!IsSteamAvailable)
        {
            Debug.Log("[Achievements] the save's record is cleared. Steam is not running, so its own copy is untouched.");
            return;
        }

        foreach (var achievement in Steamworks.SteamUserStats.Achievements)
        {
            achievement.Clear();
        }

        Steamworks.SteamUserStats.StoreStats();
        Debug.Log("[Achievements] cleared, both halves.");
    }
#endif
}
