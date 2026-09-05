using System.Linq;
using UnityEditor;
using UnityEngine;

// Puts the achievement tracker on GameSession and fills in what it reads. Written as an editor tool
// for the same reason as the others here: run it again after a change and it finds what it made
// last time rather than making a second one.
//
// Everything it wires is found by type or by asset name, so it does not need any guids written down
// and it survives assets being moved.
public static class AchievementTrackerSetup
{
    private const string GameSessionPrefabPath = "Assets/Prefabs/GameSession.prefab";

    private const string LevelSetting = "currentLvl";
    private const string DropsSetting = "dropsCaught";
    private const string AvatarSetting = "ChosenAvatar";

    // Which cat is which, as an index into AvatarSwitcher.avatars in the Gameplay scene. There is
    // nothing on an avatar to name it by, so the index is the only handle there is - and it is
    // written here, once, rather than typed into the inspector three times. EndlessAchievementTests
    // holds these against the paws each cat actually carries, so reordering the list fails a test
    // instead of quietly moving an achievement onto a different cat.
    private const int SimbaBimbaCat = 1;
    private const int ZiggyCat = 2;

    // The waves the endless achievements ask for. Changing one is a change here and a re-run of the
    // menu item below - the component is filled in from this, so an inspector edit is undone by the
    // next run.
    private const int WaveForAnyone = 5;
    private const int WaveForZiggy = 10;
    private const int WaveForSimbaBimba = 15;

    // The difficulties that count as "hard or above". Named here, but stored on the component as
    // asset references, so renaming one later cannot quietly stop an achievement from being earned.
    private static readonly string[] HardOrAbove = { "HardDifficultySetting", MetalDifficulty };

    private const string MetalDifficulty = "METAL";
    private const string EasyDifficulty = "EasyDifficultySetting";

    [MenuItem("Debug/Steam - wire the achievement tracker", priority = 102)]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameSessionPrefabPath);

        try
        {
            var tracker = root.GetComponent<AchievementTracker>();

            if (tracker == null)
            {
                tracker = root.AddComponent<AchievementTracker>();
            }

            tracker.scenarioManager = FindOne<ScenarioManager>();
            tracker.diffcultyManager = FindOne<DiffcultyManager>();
            tracker.currentLevel = Setting(LevelSetting);
            tracker.dropsCaught = Setting(DropsSetting);
            tracker.chosenAvatar = Setting(AvatarSetting);

            tracker.countsAsHard = HardOrAbove
                .Select(Difficulty)
                .Where(difficulty => difficulty != null)
                .ToArray();

            if (tracker.countsAsHard.Length != HardOrAbove.Length)
            {
                Debug.LogWarning($"[AchievementTrackerSetup] only {tracker.countsAsHard.Length} of {HardOrAbove.Length} hard difficulties were found - the ones missing will not count.");
            }

            tracker.endlessForAnyone = new AchievementTracker.EndlessMilestone
            {
                wave = WaveForAnyone,
                difficulty = null,
                cat = AchievementTracker.EndlessMilestone.AnyCat,
            };

            tracker.endlessForZiggy = new AchievementTracker.EndlessMilestone
            {
                wave = WaveForZiggy,
                difficulty = Difficulty(MetalDifficulty),
                cat = ZiggyCat,
            };

            tracker.endlessForSimbaBimba = new AchievementTracker.EndlessMilestone
            {
                wave = WaveForSimbaBimba,
                difficulty = Difficulty(EasyDifficulty),
                cat = SimbaBimbaCat,
            };

            PrefabUtility.SaveAsPrefabAsset(root, GameSessionPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[AchievementTrackerSetup] the achievement tracker is wired.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T FindOne<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

        if (guids.Length == 0)
        {
            Debug.LogError($"[AchievementTrackerSetup] no {typeof(T).Name} asset in the project.");
            return null;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning($"[AchievementTrackerSetup] {guids.Length} {typeof(T).Name} assets - taking the first. Check that it is the one the game uses.");
        }

        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static TypeDistinguisher Setting(string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>($"Assets/Resources/TypeDistinguishers/{name}.asset");

        if (asset == null)
        {
            Debug.LogError($"[AchievementTrackerSetup] missing setting asset: {name}");
        }

        return asset;
    }

    private static DifficultySettings Difficulty(string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<DifficultySettings>($"Assets/Resources/DifficultySettings/{name}.asset");

        if (asset == null)
        {
            Debug.LogError($"[AchievementTrackerSetup] missing difficulty asset: {name}");
        }

        return asset;
    }
}
