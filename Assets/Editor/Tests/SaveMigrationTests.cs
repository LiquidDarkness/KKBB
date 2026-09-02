using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Checks the repair that runs once for a save written before any setting had a default. A player
// turned up with exactly such a file: every switch stored as the zero PlayerPrefs hands back for a
// key nobody wrote, which left them with no sound, no animations and - the one that cost them the
// game - no rescue for a wedged ball. Nothing could put it right, because a stored zero is a stored
// value and the seeding pass only ever fills in what is missing.
//
// What this is for: the repair has to be brave enough to fix that and timid enough never to touch
// anything the player actually chose, and those two pull in opposite directions. The line between
// them is "a stored value that cannot be told apart from never having been written", and it is
// worth a test because getting it wrong quietly rewrites someone's settings.
//
// Nothing here touches the game's own settings or the save file. Every setting under test is built
// in memory with a name of its own, so the only PlayerPrefs keys written are this file's, and they
// are deleted again in TearDown.
public class SaveMigrationTests
{
    private const string KeyPrefix = "test_SaveMigration_";

    private readonly List<TypeDistinguisher> built = new List<TypeDistinguisher>();

    [TearDown]
    public void CleanUp()
    {
        foreach (TypeDistinguisher setting in built)
        {
            if (setting == null)
            {
                continue;
            }

            PlayerPrefs.DeleteKey(setting.name);
            Object.DestroyImmediate(setting);
        }

        built.Clear();
    }

    private TypeDistinguisher Build(string key, TypeDistinguisher.PlayerPrefType type, string defaultValue)
    {
        var setting = ScriptableObject.CreateInstance<TypeDistinguisher>();
        setting.name = KeyPrefix + key;
        setting.prefType = type;
        setting.defaultValue = defaultValue;
        built.Add(setting);

        // Whatever a previous run left behind is not this run's business.
        PlayerPrefs.DeleteKey(setting.name);
        return setting;
    }

    // The ball recall: the setting whose stored false actually cost a player their run.
    [Test]
    public void ASwitchStoredOffIsPutBackOnWhenItShipsOn()
    {
        TypeDistinguisher recall = Build("recall", TypeDistinguisher.PlayerPrefType.BOOL, "true");
        PlayerPrefs.SetInt(recall.name, 0);

        Assert.IsTrue(recall.RepairIfStoredValueLooksUnwritten(), "a stored false should have been repaired");
        Assert.IsTrue(recall.BoolValue, "the switch should be on again");
    }

    // The three volumes, all of which that player had at zero.
    [Test]
    public void ANumberStoredAtZeroIsPutBackWhenItShipsAboveZero()
    {
        TypeDistinguisher volume = Build("volume", TypeDistinguisher.PlayerPrefType.FLOAT, "1");
        PlayerPrefs.SetFloat(volume.name, 0f);

        Assert.IsTrue(volume.RepairIfStoredValueLooksUnwritten());
        Assert.AreEqual(1f, volume.FloatValue, 0.0001f);
    }

    [Test]
    public void AValueThePlayerChoseIsLeftAlone()
    {
        TypeDistinguisher volume = Build("chosen", TypeDistinguisher.PlayerPrefType.FLOAT, "1");
        PlayerPrefs.SetFloat(volume.name, 0.4f);

        Assert.IsFalse(volume.RepairIfStoredValueLooksUnwritten(), "0.4 is nobody's accident");
        Assert.AreEqual(0.4f, volume.FloatValue, 0.0001f);
    }

    // backgroundDim sat at 0.9 and the text scale at 0.75 in that player's file. Both are stored
    // extremes rather than stored zeros, so both are choices and both stay.
    [Test]
    public void AValueAtTheEndOfItsSliderIsStillAChoice()
    {
        TypeDistinguisher dim = Build("dim", TypeDistinguisher.PlayerPrefType.FLOAT, "0");
        PlayerPrefs.SetFloat(dim.name, 0.9f);

        Assert.IsFalse(dim.RepairIfStoredValueLooksUnwritten());
        Assert.AreEqual(0.9f, dim.FloatValue, 0.0001f);
    }

    // Reduce motion ships off. Its stored false is the same as its default, so there is nothing to
    // repair towards and the pass must not claim to have done anything.
    [Test]
    public void ASwitchThatShipsOffIsNotTouched()
    {
        TypeDistinguisher motion = Build("motion", TypeDistinguisher.PlayerPrefType.BOOL, "false");
        PlayerPrefs.SetInt(motion.name, 0);

        Assert.IsFalse(motion.RepairIfStoredValueLooksUnwritten());
        Assert.IsFalse(motion.BoolValue);
    }

    // Progress - the score, the chosen scenario, the beat - carries no default on purpose, because
    // zero is already the right answer for all of it. The repair must never reach in there.
    [Test]
    public void ProgressIsNeverRepaired()
    {
        TypeDistinguisher score = Build("score", TypeDistinguisher.PlayerPrefType.INT, string.Empty);
        PlayerPrefs.SetInt(score.name, 0);

        Assert.IsFalse(score.RepairIfStoredValueLooksUnwritten());
        Assert.AreEqual(0, score.IntValue);
    }

    [Test]
    public void NothingStoredIsTheSeedingPassesJobNotThisOne()
    {
        TypeDistinguisher fresh = Build("fresh", TypeDistinguisher.PlayerPrefType.BOOL, "true");

        Assert.IsFalse(fresh.RepairIfStoredValueLooksUnwritten(), "with nothing stored there is nothing to repair");
        Assert.IsTrue(fresh.ApplyDefaultIfUnset(), "seeding is what fills that in");
        Assert.IsTrue(fresh.BoolValue);
    }

    // The rule that made the repair necessary, kept honest: seeding leaves a stored value alone,
    // whatever it is.
    [Test]
    public void SeedingStillRefusesToOverwriteAnythingStored()
    {
        TypeDistinguisher setting = Build("stored", TypeDistinguisher.PlayerPrefType.BOOL, "true");
        PlayerPrefs.SetInt(setting.name, 0);

        Assert.IsFalse(setting.ApplyDefaultIfUnset());
        Assert.IsFalse(setting.BoolValue);
    }

    [Test]
    public void AFileWithNoStampReadsAsTheVersionBeforeThereWereAny()
    {
        Assert.AreEqual(0, SaveManager.VersionOf(new[] { "masterVolume/FLOAT/0", "reduceMotion/BOOL/0" }));
        Assert.AreEqual(0, SaveManager.VersionOf(null));
    }

    [Test]
    public void AStampIsRead()
    {
        Assert.AreEqual(1, SaveManager.VersionOf(new[] { "#version 1", "masterVolume/FLOAT/1" }));
    }

    // Read-only, and pointed at the real asset: if the ball recall ever stops shipping switched on,
    // the repair above is quietly repairing towards nothing and the run that started all of this
    // could happen again.
    [Test]
    public void TheBallRecallStillShipsSwitchedOn()
    {
        var recall = AssetDatabase.LoadAssetAtPath<TypeDistinguisher>(
            "Assets/Resources/TypeDistinguishers/ballRecallActive.asset");

        Assert.IsNotNull(recall, "the ball recall setting has moved or been renamed");
        Assert.AreEqual("true", recall.defaultValue.Trim().ToLowerInvariant());
    }
}
