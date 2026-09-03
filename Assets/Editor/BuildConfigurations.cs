using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The four builds this project ships, and what each of them has to be set to. They differ by
// nothing but a pair of scripting defines and a platform, which is exactly the kind of difference
// nobody notices going wrong - the defines live in ProjectSettings.asset, which is deliberately not
// committed, so switching platform hands you an empty list and the repository cannot warn anyone.
//
// It has gone wrong twice already: a build once went out booting from the wrong scene, and the
// downloadable demo went out carrying the Steam libraries it had no use for. This is the recipe
// written down somewhere that IS committed, so the settings can be wrong but the answer cannot be
// lost.
//
// Nothing here builds anything. It puts the project into a state and says what state that is; the
// Build button is still yours to press.
public static class BuildConfigurations
{
    private const string SteamInitScene = "Assets/Scenes/SteamInit.unity";
    private const string NonSteamInitScene = "Assets/Scenes/NonSteamInit.unity";

    [MenuItem("Build/Set up: Steam demo", priority = 0)]
    public static void SteamDemo()
    {
        Apply(BuildTargetGroup.Standalone, "DEMO_BUILD", "the demo on Steam");
    }

    [MenuItem("Build/Set up: Steam full game", priority = 1)]
    public static void SteamFull()
    {
        Apply(BuildTargetGroup.Standalone, string.Empty, "the full game on Steam");
    }

    [MenuItem("Build/Set up: itch demo, downloadable", priority = 2)]
    public static void ItchDemo()
    {
        Apply(BuildTargetGroup.Standalone, "DEMO_BUILD;NO_STEAM", "the downloadable demo for itch");
    }

    [MenuItem("Build/Set up: WebGL demo", priority = 3)]
    public static void WebGLDemo()
    {
        Apply(BuildTargetGroup.WebGL, "DEMO_BUILD;NO_STEAM", "the demo in a browser");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[Build] Switching the active target to WebGL - this reimports and takes a while.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }
    }

    [MenuItem("Build/What is this set to right now?", priority = 20)]
    public static void Report()
    {
        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

        Debug.Log($"[Build] Target {EditorUserBuildSettings.activeBuildTarget}, defines \"{symbols}\" - that is {Describe(symbols)}.");
        CheckScenes();
    }

    private static string Describe(string symbols)
    {
        List<string> set = Split(symbols);
        bool demo = set.Contains("DEMO_BUILD");
        bool steamless = set.Contains("NO_STEAM");

        if (set.Contains("AUTOPLAY"))
        {
            return "NOT SHIPPABLE - autoplay is compiled in";
        }

        if (demo && steamless)
        {
            return "a demo with no Steam in it - itch, or the browser";
        }

        if (demo)
        {
            return "the demo on Steam";
        }

        if (steamless)
        {
            return "the FULL GAME with no Steam in it";
        }

        return "the full game on Steam";
    }

    private static void Apply(BuildTargetGroup group, string symbols, string what)
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);

        // The defines belong to the group, but the Build button builds whatever platform is active -
        // so setting one without the other is how you carefully configure a build you are not making.
        if (group == BuildTargetGroup.Standalone
            && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            Debug.Log("[Build] Switching the active target back to Windows - this reimports and takes a while.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        Debug.Log($"[Build] {group} is now set up for {what}: defines \"{symbols}\".");

        if (Split(symbols).Contains("NO_STEAM"))
        {
            // The one thing people reach for instead, and the reason a build once shipped from an
            // empty-looking scene nobody meant to use.
            Debug.Log("[Build] A build with no Steam still starts from SteamInit. The define is the switch, not the scene.");
        }

        CheckScenes();
        AssetDatabase.SaveAssets();
    }

    // Said rather than silently corrected: the scene list is the one part of this that is committed,
    // so a disagreement here means someone changed it on purpose or by accident, and either way they
    // should hear about it rather than have it undone behind their back.
    private static void CheckScenes()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene steam = scenes.FirstOrDefault(s => s.path == SteamInitScene);
        EditorBuildSettingsScene nonSteam = scenes.FirstOrDefault(s => s.path == NonSteamInitScene);

        if (steam == null || !steam.enabled)
        {
            Debug.LogError("[Build] SteamInit is not enabled in the build settings. Every build starts there, whether or not Steam is in it.");
        }

        if (nonSteam != null && nonSteam.enabled)
        {
            Debug.LogError("[Build] NonSteamInit is enabled. It is a leftover of an older idea, nothing refers to it, and it is missing layout fixes SteamInit has - turn it off.");
        }

        int first = System.Array.FindIndex(scenes, s => s.enabled);

        if (first >= 0)
        {
            Debug.Log("[Build] The build starts at " + scenes[first].path);
        }
    }

    private static List<string> Split(string symbols)
    {
        return (symbols ?? string.Empty)
            .Split(';')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
