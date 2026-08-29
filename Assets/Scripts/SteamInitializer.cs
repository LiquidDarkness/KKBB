using UnityEngine;
using UnityEngine.SceneManagement;

// Starts Steam, keeps its callbacks running, and shuts it down again.
//
// NO_STEAM, a scripting define like DEMO_BUILD, takes the whole of Steam out of a build: no
// client is started, no callbacks are pumped, no achievements are sent and no scores go to a
// leaderboard. It is what a copy uploaded somewhere other than Steam is built with - itch, a
// direct download, a key handed to a festival - where there is no client to talk to and the
// native steam_api dll need not ship at all, since nothing here ever loads it.
//
// Everything Steam-facing in the game asks one of three switches, all of which NO_STEAM answers
// no to: this initialiser, Achievements.IsSteamAvailable and Leaderboards.IsAvailable. Playing,
// scoring, records and the save file are untouched by it - they were never Steam's to begin with.
public class SteamInitializer : MonoBehaviour
{
    public int demoID, fullID;

    private void Start()
    {
#if NO_STEAM
        Debug.Log("Built with NO_STEAM: no Steam client is started, and nothing is reported to one.");
#else
        //skomentuj, żeby unablnąć aczki
        //PlayerPrefs.SetInt("storiesRead", storiesRead.);

        // Driven by the DEMO_BUILD scripting define symbol, the same switch that gates which
        // scenarios are playable - so the build can't end up reporting the wrong app to Steam.
#if DEMO_BUILD
        uint id = (uint)demoID;
#else
        uint id = (uint)fullID;
#endif

        try
        {
            Steamworks.SteamClient.Init(id);
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
        Debug.Log("Init's working");

        //SceneManager.LoadScene(1);

        HookStats();
#endif
    }

    // Steam will not answer a question about achievements until it has sent the stats over, and it
    // only sends them when asked. Once they land, everything the save already knows about is handed
    // over - which is how a demo player's earnings turn up in the full game on its first run.
    private void HookStats()
    {
        if (!Steamworks.SteamClient.IsValid)
        {
            return;
        }

        Steamworks.SteamUserStats.OnUserStatsReceived += HandleStatsReceived;
        Steamworks.SteamUserStats.RequestCurrentStats();
    }

    private void HandleStatsReceived(Steamworks.SteamId id, Steamworks.Result result)
    {
        // The same callback carries other people's stats - a friends list asking after them is
        // enough to raise it - and those say nothing about what this player has earned.
        if (!Steamworks.SteamClient.IsValid || id != Steamworks.SteamClient.SteamId)
        {
            return;
        }

        if (result != Steamworks.Result.OK)
        {
            Debug.LogWarning($"Steam sent no stats ({result}), so achievements earned offline stay unsent for now.");
            return;
        }

        Achievements.PushEarned();
    }

    // Steam talks back through callbacks, and it only gets to run them when it is asked to. Without
    // this the answers never arrive: stats come back empty, and an achievement unlocks in silence
    // with no notification in the corner. This object carries DontDestroyOnLoaded, so asking here
    // covers the whole run.
    private void Update()
    {
#if !NO_STEAM
        if (!Steamworks.SteamClient.IsValid)
        {
            return;
        }

        Steamworks.SteamClient.RunCallbacks();
#endif
    }

    // Steam wants telling that the game is going, and a client left running is what makes the next
    // Init fail with the app already open.
    private void OnDestroy()
    {
#if !NO_STEAM
        if (!Steamworks.SteamClient.IsValid)
        {
            return;
        }

        Steamworks.SteamUserStats.OnUserStatsReceived -= HandleStatsReceived;
        Steamworks.SteamClient.Shutdown();
#endif
    }
}
