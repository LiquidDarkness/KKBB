using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

// Every Steam leaderboard the game knows about, and the one way of posting to one.
//
// One board per scenario and difficulty, because Steam keeps a single entry per player per board -
// a METAL run and an Easy run of the same scenario would otherwise be fighting over one line, and
// the difficulty multiplier means Easy would never win it. The names below are the API names, the
// ids rather than anything a player reads; they are written down once here and referred to by
// constant, so a typo fails to compile instead of failing in silence on someone else's machine.
//
// Boards are looked up, not created: Valve asks for them to be made on the partner site so they can
// carry a Community Name and show up on the community hub at all. A board that is missing is created
// anyway, so the game works while the partner site is still being filled in - it just stays invisible
// there until the name is set.
public static class Leaderboards
{
    // Asset name to the id half that goes into the board name. A new scenario or difficulty adds a
    // line here and a board on the partner site, and nothing else changes.
    private static readonly Dictionary<string, string> Scenarios = new Dictionary<string, string>
    {
        { "LiquidDarkness", "LIQUID_DARKNESS" },
        { "SimbaBimba", "SIMBA_BIMBA" },
        { "ZiggiesMunda", "ZIGGIES_MUNDA" },

        // The one board where the score says how long the player lasted rather than how well they
        // played something with an end to it.
        { "Endless", "ENDLESS" },

        // The tutorial teaches; it is not somewhere to compete, and it is where a player who has
        // never played is at their worst. Listed so a missing board can still be told apart from a
        // scenario nobody has written down.
        { "Tutorial", null },
    };

    private static readonly Dictionary<string, string> Difficulties = new Dictionary<string, string>
    {
        { "EasyDifficultySetting", "EASY" },
        { "MediumDifficultySetting", "MEDIUM" },
        { "HardDifficultySetting", "HARD" },
        { "METAL", "METAL" },
        { "Tutorial", null },
    };

#if !NO_STEAM
    // Cached per board name: finding one is a round trip to Steam, and the same board is asked for
    // twice in a row every time - once to post, once to read the standings back.
    private static readonly Dictionary<string, Leaderboard> Found = new Dictionary<string, Leaderboard>();
#endif

    public static bool IsAvailable
    {
        get
        {
#if NO_STEAM
            // Built for somewhere that is not Steam: see SteamInitializer for what that define does.
            return false;
#elif DEMO_BUILD
            // The demo is a separate app id with a separate set of boards, and the same reasoning
            // that keeps achievements out of it applies here: what is played in the demo is not the
            // game the ladder is for. One line to change if that ever stops being true.
            return false;
#else
            return SteamClient.IsValid;
#endif
        }
    }

    // Null when this pairing deliberately has no board, or when one of the names is not on the list
    // above - which is a wiring mistake worth hearing about.
    public static string NameFor(string scenario, string difficulty)
    {
        if (!Scenarios.TryGetValue(scenario, out string scenarioId))
        {
            Debug.LogWarning($"[{nameof(Leaderboards)}] scenario '{scenario}' is not listed - add it here and on the partner site, or its scores go nowhere.");
            return null;
        }

        if (!Difficulties.TryGetValue(difficulty, out string difficultyId))
        {
            Debug.LogWarning($"[{nameof(Leaderboards)}] difficulty '{difficulty}' is not listed - add it here and on the partner site, or its scores go nowhere.");
            return null;
        }

        if (scenarioId == null || difficultyId == null)
        {
            return null;
        }

        return scenarioId + "_" + difficultyId;
    }

#if !NO_STEAM
    // Posts the score and hands back where the player stands, or null if there is nothing to show:
    // no Steam, no board, or an answer that never came. Steam keeps the better of the two scores,
    // which is what a record means here - a worse run does not cost the player their place.
    //
    // Uploading is rate limited by Valve to ten in ten minutes with one call outstanding at a time;
    // one call at the end of a scenario is nowhere near that, and nothing else here posts.
    public static async Task<LeaderboardEntry[]> PostAndReadAsync(string scenario, string difficulty, int score, int[] details, int above, int below)
    {
        if (!IsAvailable)
        {
            return null;
        }

        string name = NameFor(scenario, difficulty);

        if (name == null)
        {
            return null;
        }

        try
        {
            Leaderboard? board = await FindAsync(name);

            if (!board.HasValue)
            {
                Debug.LogWarning($"[{nameof(Leaderboards)}] Steam has no board called '{name}' and would not make one.");
                return null;
            }

            LeaderboardUpdate? update = await board.Value.SubmitScoreAsync(score, details);

            if (!update.HasValue)
            {
                Debug.LogWarning($"[{nameof(Leaderboards)}] '{name}' did not take the score - it stays on this machine only.");
            }

            return await board.Value.GetScoresAroundUserAsync(-above, below);
        }
        catch (System.Exception problem)
        {
            // A ladder is a nicety: the run is already counted, saved and recorded locally, so
            // anything Steam does here is reported and then dropped.
            Debug.LogWarning($"[{nameof(Leaderboards)}] '{name}' went wrong: {problem.Message}");
            return null;
        }
    }

    private static async Task<Leaderboard?> FindAsync(string name)
    {
        if (Found.TryGetValue(name, out Leaderboard cached))
        {
            return cached;
        }

        Leaderboard? board = await SteamUserStats.FindLeaderboardAsync(name);

        if (!board.HasValue)
        {
            // Nothing on the partner site yet. Made here so the game works during development; it
            // will not appear on the community hub until it is given a Community Name over there.
            Debug.Log($"[{nameof(Leaderboards)}] '{name}' is not on the partner site yet - creating it so scores are not lost.");
            board = await SteamUserStats.FindOrCreateLeaderboardAsync(name, LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
        }

        if (board.HasValue)
        {
            Found[name] = board.Value;
        }

        return board;
    }
#endif
}
