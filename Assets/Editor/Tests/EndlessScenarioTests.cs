using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Checks the endless scenario deals its levels the way a run depends on it dealing them, and that
// the one in the project is wired up. Nothing here writes to PlayerPrefs, the save file or Steam:
// the seed is only ever read, and a scenario built here has no setting behind it at all, so it
// falls back to the fixed seed and stays entirely in memory.
//
// What it is for: the order of levels is not written down anywhere - it is worked out from a seed
// and a wave number, every time it is asked for - so anything that quietly changed it would show up
// as levels repeating, or as a saved run coming back a different game than it was left.
public class EndlessScenarioTests
{
    private const string EndlessAssetPath = "Assets/Stories/Endless/Endless.asset";
    private const string ScenarioManagerPath = "Assets/New Scenario Manager.asset";

    private readonly List<Object> built = new List<Object>();

    [TearDown]
    public void CleanUp()
    {
        foreach (Object item in built)
        {
            if (item != null)
            {
                Object.DestroyImmediate(item);
            }
        }

        built.Clear();
    }

    // A scenario of levels that exist only for this test, plus a farewell beat with no content -
    // the shape every written scenario in the game has.
    private StoryContainer MakeSource(string name, int levels)
    {
        StoryContainer source = ScriptableObject.CreateInstance<StoryContainer>();
        source.name = name;
        source.availableInDemo = true;
        built.Add(source);

        var beats = new List<Story>();

        for (int i = 0; i < levels; i++)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.name = name + i;
            level.content = new GameObject(name + i + " content");
            built.Add(level);
            built.Add(level.content);

            beats.Add(new Story { level = level });
        }

        LevelData farewell = ScriptableObject.CreateInstance<LevelData>();
        farewell.name = name + "Ending";
        built.Add(farewell);
        beats.Add(new Story { level = farewell });

        source.stories = beats.ToArray();

        return source;
    }

    private EndlessScenario MakeEndless(params StoryContainer[] sources)
    {
        EndlessScenario endless = ScriptableObject.CreateInstance<EndlessScenario>();
        endless.name = "TestEndless";
        endless.levelSources = new List<StoryContainer>(sources);
        built.Add(endless);

        return endless;
    }

    [Test]
    public void ThePoolTakesEveryPlayableLevelAndNothingElse()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 3), MakeSource("B", 2));

        Assert.That(endless.Pool.Count, Is.EqualTo(5),
            "The farewell beats carry no content and must never be dealt.");
    }

    [Test]
    public void ALevelNamedTwiceIsOnlyDealtOnce()
    {
        StoryContainer source = MakeSource("A", 2);
        StoryContainer twice = MakeSource("B", 1);
        twice.stories[0] = source.stories[0];

        EndlessScenario endless = MakeEndless(source, twice);

        Assert.That(endless.Pool.Count, Is.EqualTo(2));
    }

    [Test]
    public void EveryLevelComesUpOnceBeforeAnyComesUpTwice()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 6), MakeSource("B", 4));
        int size = endless.Pool.Count;

        var dealt = new List<LevelData>();

        for (int beat = 0; beat < size; beat++)
        {
            dealt.Add(endless.LevelForBeat(beat));
        }

        CollectionAssert.AllItemsAreUnique(dealt);
        CollectionAssert.AreEquivalent(endless.Pool, dealt);
    }

    [Test]
    public void NoLevelIsDealtTwiceRunning()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 6), MakeSource("B", 4));

        // Three whole cycles, so both seams between them are covered - back to back is the one
        // place a shuffle can still repeat itself, and the one place a player would notice.
        for (int beat = 1; beat < endless.Pool.Count * 3; beat++)
        {
            Assert.That(endless.LevelForBeat(beat), Is.Not.SameAs(endless.LevelForBeat(beat - 1)),
                $"Wave {EndlessScenario.WaveOf(beat)} deals the same level as the wave before it.");
        }
    }

    // A run is saved as a seed and a wave number, so the same wave asked again - after the pool has
    // been walked through, after the game has been left and come back to - has to be the same level.
    [Test]
    public void TheSameWaveAlwaysDealsTheSameLevel()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 5));
        int size = endless.Pool.Count;

        LevelData first = endless.LevelForBeat(2);

        for (int beat = 0; beat < size * 2; beat++)
        {
            endless.LevelForBeat(beat);
        }

        Assert.That(endless.LevelForBeat(2), Is.SameAs(first));
    }

    [Test]
    public void ADifferentSeedDealsADifferentOrder()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 12));
        int size = endless.Pool.Count;

        // The cycles of one run are dealt from the same seed and differ only by their number, which
        // is the same thing a different seed does - if these came out alike, so would two runs.
        var first = new List<LevelData>();
        var second = new List<LevelData>();

        for (int beat = 0; beat < size; beat++)
        {
            first.Add(endless.LevelForBeat(beat));
            second.Add(endless.LevelForBeat(beat + size));
        }

        CollectionAssert.AreNotEqual(first, second);
    }

    [Test]
    public void TheFirstWaveRunsAtTheDifficultysOwnPace()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 2));
        DifficultySettings difficulty = Difficulty(3f, 9f);

        Assert.That(endless.SpeedForWave(difficulty, 1), Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void TheRampClimbsPartOfTheWayAndThenStops()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 2));
        endless.wavesToTopSpeed = 10;
        endless.speedRampShare = 0.5f;

        DifficultySettings difficulty = Difficulty(3f, 9f);
        float top = endless.SpeedForWave(difficulty, 11);

        Assert.That(top, Is.EqualTo(6f).Within(0.0001f), "Halfway between 3 and 9.");
        Assert.That(endless.SpeedForWave(difficulty, 400), Is.EqualTo(top).Within(0.0001f),
            "Past the top of the ramp the pace has to settle, or a long run outruns maxSpeed.");
        Assert.That(endless.SpeedForWave(difficulty, 6), Is.GreaterThan(endless.SpeedForWave(difficulty, 2)));
    }

    [Test]
    public void PointsAreWorthMoreEveryWaveUpToTheCeiling()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 2));
        endless.scoreMultiplierPerWave = 0.1f;
        endless.maxScoreMultiplier = 2f;

        Assert.That(endless.ScoreMultiplierForWave(1), Is.EqualTo(1f).Within(0.0001f),
            "The first wave pays as collected.");
        Assert.That(endless.ScoreMultiplierForWave(6), Is.EqualTo(1.5f).Within(0.0001f));
        Assert.That(endless.ScoreMultiplierForWave(500), Is.EqualTo(2f).Within(0.0001f));
    }

    [Test]
    public void ALifeComesBackEveryFewWavesAndNeverOnTheFirst()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 2));
        endless.extraLifeEveryWaves = 5;

        Assert.That(endless.GrantsLifeOnWave(1), Is.False);
        Assert.That(endless.GrantsLifeOnWave(6), Is.True);
        Assert.That(endless.GrantsLifeOnWave(7), Is.False);
        Assert.That(endless.GrantsLifeOnWave(11), Is.True);

        endless.extraLifeEveryWaves = 0;
        Assert.That(endless.GrantsLifeOnWave(6), Is.False, "Zero means none at all.");
    }

    [Test]
    public void ItNeverEndsAndHasNoFarewellToLandOn()
    {
        EndlessScenario endless = MakeEndless(MakeSource("A", 3));

        Assert.That(endless.HasFarewellBeat, Is.False);
        Assert.That(endless.HasBeatText, Is.False);
        Assert.That(endless.BeatCount, Is.GreaterThan(1000000));
    }

    // Everything from here down is about the asset in the project rather than the class.

    [Test]
    public void TheEndlessScenarioIsInTheGame()
    {
        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessAssetPath);
        Assert.That(endless, Is.Not.Null, EndlessAssetPath + " is missing.");

        ScenarioManager manager = AssetDatabase.LoadAssetAtPath<ScenarioManager>(ScenarioManagerPath);
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager.scenarios.Contains(endless), Is.True,
            "The chosen scenario is saved as an index into this list - endless cannot be picked while it is not on it.");

        Assert.That(endless.runSeed, Is.Not.Null, "With no seed setting every run is dealt the same order.");
        Assert.That(endless.levelSources.Contains(endless), Is.False, "Endless must not deal from itself.");
        Assert.That(endless.Pool.Count, Is.GreaterThan(0), "Nothing to play.");
    }

    // Records are stored as scenario|difficulty=score in one string, so a name carrying any of those
    // three characters would corrupt every record stored beside it.
    [Test]
    public void ItsNameCanBeStoredAsARecord()
    {
        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessAssetPath);
        Assert.That(endless, Is.Not.Null);

        Assert.That(endless.name.IndexOfAny(new[] { '|', ';', '=' }), Is.LessThan(0), endless.name);
    }

    // A scenario Leaderboards has never heard of posts its scores nowhere, and says so only in a
    // warning nobody reads on a player's machine.
    [Test]
    public void ItHasAPlaceOnTheLadder()
    {
        EndlessScenario endless = AssetDatabase.LoadAssetAtPath<EndlessScenario>(EndlessAssetPath);
        Assert.That(endless, Is.Not.Null);

        Assert.That(Leaderboards.NameFor(endless.name, "METAL"), Is.Not.Null.And.Not.Empty);
    }

    private DifficultySettings Difficulty(float baseSpeed, float maxSpeed)
    {
        DifficultySettings difficulty = ScriptableObject.CreateInstance<DifficultySettings>();
        difficulty.baseGameSpeed = baseSpeed;
        difficulty.maxSpeed = maxSpeed;
        built.Add(difficulty);

        return difficulty;
    }
}
