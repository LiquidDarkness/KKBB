using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The endless achievements ask three questions - how far, on what, and as whom - and two of the
// three answers are held as bare indices into a list in the Gameplay scene, because there is nothing
// on an avatar to name it by. Reordering that list would move an achievement onto a different cat
// without anything complaining, so these hold the indices against something the cats actually carry.
//
// Nothing here touches PlayerPrefs, the save or Steam: it reads assets and asks a pure method.
public class EndlessAchievementTests
{
    private const string GameSessionPath = "Assets/Prefabs/GameSession.prefab";

    private static AchievementTracker Tracker()
    {
        GameObject session = AssetDatabase.LoadAssetAtPath<GameObject>(GameSessionPath);
        Assert.That(session, Is.Not.Null, GameSessionPath + " is missing.");

        AchievementTracker tracker = session.GetComponent<AchievementTracker>();
        Assert.That(tracker, Is.Not.Null, "GameSession has no AchievementTracker - run Debug > Steam - wire the achievement tracker.");

        return tracker;
    }

    // --- What a milestone asks for -------------------------------------------------------------

    [Test]
    public void AWaveShortOfTheMarkEarnsNothing()
    {
        var milestone = new AchievementTracker.EndlessMilestone { wave = 10 };

        Assert.That(milestone.IsReachedBy(9, null, 2), Is.False);
        Assert.That(milestone.IsReachedBy(10, null, 2), Is.True);
        Assert.That(milestone.IsReachedBy(400, null, 2), Is.True, "a deeper run has still passed the mark");
    }

    [Test]
    public void ANamedDifficultyIsTheOnlyOneThatCounts()
    {
        DifficultySettings metal = ScriptableObject.CreateInstance<DifficultySettings>();
        DifficultySettings easy = ScriptableObject.CreateInstance<DifficultySettings>();

        var milestone = new AchievementTracker.EndlessMilestone { wave = 10, difficulty = metal };

        Assert.That(milestone.IsReachedBy(10, metal, 2), Is.True);
        Assert.That(milestone.IsReachedBy(10, easy, 2), Is.False);
        Assert.That(milestone.IsReachedBy(10, null, 2), Is.False, "a difficulty nobody could read is not the one asked for");

        Object.DestroyImmediate(metal);
        Object.DestroyImmediate(easy);
    }

    [Test]
    public void NoDifficultyNamedMeansAnyOfThem()
    {
        DifficultySettings anything = ScriptableObject.CreateInstance<DifficultySettings>();
        var milestone = new AchievementTracker.EndlessMilestone { wave = 5 };

        Assert.That(milestone.IsReachedBy(5, anything, 0), Is.True);
        Assert.That(milestone.IsReachedBy(5, null, 3), Is.True);

        Object.DestroyImmediate(anything);
    }

    [Test]
    public void ANamedCatIsTheOnlyOneThatCounts()
    {
        var milestone = new AchievementTracker.EndlessMilestone { wave = 10, cat = 2 };

        Assert.That(milestone.IsReachedBy(10, null, 2), Is.True);
        Assert.That(milestone.IsReachedBy(10, null, 1), Is.False);
        Assert.That(milestone.IsReachedBy(10, null, AchievementTracker.EndlessMilestone.AnyCat), Is.False);
    }

    // --- What the game actually ships with -----------------------------------------------------

    [Test]
    public void TheTrackerCanReadWhichCatIsInPlay()
    {
        AchievementTracker tracker = Tracker();

        Assert.That(tracker.chosenAvatar, Is.Not.Null, "without ChosenAvatar wired, every cat reads as the first one.");
        Assert.That(tracker.chosenAvatar.name, Is.EqualTo("ChosenAvatar"));
    }

    [Test]
    public void TheThreeMilestonesAreTheOnesThatWereAskedFor()
    {
        AchievementTracker tracker = Tracker();

        Assert.That(tracker.endlessForAnyone.wave, Is.EqualTo(5));
        Assert.That(tracker.endlessForAnyone.difficulty, Is.Null, "the first one is meant to count on any difficulty");
        Assert.That(tracker.endlessForAnyone.cat, Is.EqualTo(AchievementTracker.EndlessMilestone.AnyCat), "and with any cat");

        Assert.That(tracker.endlessForZiggy.wave, Is.EqualTo(10));
        Assert.That(tracker.endlessForZiggy.difficulty, Is.Not.Null, "Ziggy's is meant to be METAL only");
        Assert.That(tracker.endlessForZiggy.difficulty.name, Is.EqualTo("METAL"));

        Assert.That(tracker.endlessForSimbaBimba.wave, Is.EqualTo(15));
        Assert.That(tracker.endlessForSimbaBimba.difficulty, Is.Not.Null, "Simba Bimba's is meant to be Easy only");
        Assert.That(tracker.endlessForSimbaBimba.difficulty.name, Is.EqualTo("EasyDifficultySetting"));
    }

    // The one that guards the indices. A cat has no name of its own, but it does carry its own paws,
    // and those are named after it. If this fails after the avatar list was reordered, the fix is the
    // index in AchievementTrackerSetup - not this test.
    [Test]
    public void TheCatIndicesStillPointAtTheCatsTheyAreNamedFor()
    {
        AchievementTracker tracker = Tracker();

        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity", OpenSceneMode.Single);
        AvatarSwitcher switcher = Object.FindObjectsOfType<AvatarSwitcher>(true).FirstOrDefault();

        Assert.That(switcher, Is.Not.Null, "no AvatarSwitcher in Gameplay to check the indices against.");

        AssertCatIs(switcher, tracker.endlessForZiggy.cat, "Ziggy");
        AssertCatIs(switcher, tracker.endlessForSimbaBimba.cat, "Simba");
    }

    private static void AssertCatIs(AvatarSwitcher switcher, int index, string expected)
    {
        Assert.That(index, Is.InRange(0, switcher.avatars.Count - 1),
            $"avatar index {index} is outside the {switcher.avatars.Count} cats there are.");

        Sprite paws = switcher.avatars[index].paws;

        Assert.That(paws, Is.Not.Null,
            $"avatar {index} carries no paws of its own, so there is nothing here to tell which cat it is.");

        Assert.That(paws.name.ToLower(), Does.Contain(expected.ToLower()),
            $"avatar {index} was taken to be {expected}, but its paws are '{paws.name}'.");
    }
}
