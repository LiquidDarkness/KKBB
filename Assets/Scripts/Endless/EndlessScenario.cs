using System.Collections.Generic;
using UnityEngine;

// A scenario with no story to tell and no end to reach: level after level, drawn from everything
// the game has, each wave a little faster and each point worth a little more than the one before.
//
// It is a StoryContainer like the others on purpose, so that nothing outside has to learn a second
// way of starting a run. It sits in ScenarioManager.scenarios, the chosen index points at it, the
// beat number and the score are saved under the same settings - which is what lets Continue from
// the main menu pick an endless run up exactly where it was left. What it has not got is a fixed
// list of beats, so the beats are answered one at a time instead of being read out of the stories
// array, which stays empty here.
//
// The order the levels come in is dealt from a seed rather than written down: a run only has to
// remember its seed and which wave it is on for the whole sequence to be reproducible, wave 400
// included, on a save file that gains a single number.
[CreateAssetMenu(fileName = "Endless", menuName = "Custom/EndlessScenario")]
public class EndlessScenario : StoryContainer
{
    [Header("Where the levels come from")]
    [Tooltip("Scenarios whose levels are dealt out here. Their farewell beats are skipped - those carry no content and must never be loaded - and a level named twice is only taken once. In a demo build the scenarios locked out of the demo are left out as well, so endless can never leak full-version levels.")]
    public List<StoryContainer> levelSources = new List<StoryContainer>();

    [Header("The run")]
    [Tooltip("The seed this run was dealt from. Rolled when a run starts and saved with the rest of the progress, so the same wave always brings the same level back.")]
    public TypeDistinguisher runSeed;

    [Header("How it tightens")]
    [Tooltip("Waves it takes to reach the fastest this mode ever gets. The first wave runs at the difficulty's own pace.")]
    public int wavesToTopSpeed = 15;

    [Tooltip("How far towards the difficulty's maxSpeed the ramp is allowed to climb, 0 to 1. Deliberately short of all the way: maxSpeed is the ceiling a speed-up drop may push Kitty to, and a mode that sat on it would make those drops worth nothing.")]
    [Range(0f, 1f)]
    public float speedRampShare = 0.5f;

    [Tooltip("Added to the points multiplier by every wave survived. The first wave pays as collected.")]
    public float scoreMultiplierPerWave = 0.1f;

    [Tooltip("As high as that multiplier ever goes.")]
    public float maxScoreMultiplier = 3f;

    [Tooltip("A life handed back every this many waves. 0 hands none out at all, and there is no ceiling on what a long run can pile up - surviving is what pays for them.")]
    public int extraLifeEveryWaves = 5;

    // Built once and kept, as long as there is anything in it: it is asked for on every beat, and
    // nothing about it can change while the game is running - the sources are assets and the demo
    // gate is decided at compile time.
    private List<LevelData> pool;

    // The permutation of the last cycle asked about, so a wave shown, loaded and asked after does
    // not re-shuffle the whole pool three times over.
    private List<LevelData> dealtCycle;
    private int dealtCycleNumber = -1;
    private int dealtCycleSeed;

    // Whether the empty pool has already been explained. Said once, and said again only after one
    // has been built successfully in between.
    private bool hasComplained;

    // The wave the player is on, counted from one, out of the beat number the save file keeps.
    public static int WaveOf(int beat)
    {
        return beat + 1;
    }

    // Never the farewell beat, never the end of a list: there is always another wave, and
    // StoryManager reads this to work out that it can never be on an ending.
    public override int BeatCount => int.MaxValue;

    public override bool HasFarewellBeat => false;

    // Nothing in the translation files says anything about wave 37. EndlessBeatCard writes the card
    // instead, and this is what tells StoryManager to leave the story text alone for it.
    public override bool HasBeatText => false;

    public override Story BeatAt(int beat)
    {
        LevelData level = LevelForBeat(beat);

        if (level == null)
        {
            return null;
        }

        // The story beats of a written scenario carry their own backdrop; here the level's own is
        // the only one there is, and it is what MainManager puts up anyway.
        return new Story
        {
            backgroundSprite = level.background,
            level = level,
        };
    }

    public LevelData LevelForBeat(int beat)
    {
        List<LevelData> levels = Pool;

        if (levels.Count == 0)
        {
            // Quiet on purpose: building the pool has just said, in full, what was wrong with it,
            // and a second line per wave would only bury it.
            return null;
        }

        if (beat < 0)
        {
            beat = 0;
        }

        int cycle = beat / levels.Count;
        int slot = beat % levels.Count;

        return Deal(cycle)[slot];
    }

    public List<LevelData> Pool
    {
        get
        {
            // An empty pool is never kept. It is built the first time a wave asks for a level, which
            // is early enough that anything not yet in place - an asset still being imported, a
            // reference set by an editor script in the same frame - would otherwise be remembered as
            // "there are no levels" for the rest of the session, and every wave after it would fail
            // for a reason that had already gone away.
            if (pool == null || pool.Count == 0)
            {
                BuildPool();
            }

            return pool;
        }
    }

    // Every level of every scenario that may be played in this build, each of them once. Ending
    // beats are recognised by having no content to instantiate, which is exactly what makes them
    // unplayable - the same thing LevelLoader complains about when something routes it at one.
    private void BuildPool()
    {
        pool = new List<LevelData>();
        var complaint = new System.Text.StringBuilder();

        foreach (StoryContainer source in levelSources)
        {
            if (source == null)
            {
                complaint.Append("\n  <missing asset> - a slot in levelSources points at nothing.");
                continue;
            }

            if (source == this)
            {
                continue;
            }

#if DEMO_BUILD
            // The same rule DemoContentGate applies to the menu buttons, applied to the pool: what
            // the demo may not start, the demo may not be dealt either.
            if (!source.availableInDemo)
            {
                complaint.Append($"\n  {source.name} - left out of a demo build, since it is not availableInDemo.");
                continue;
            }
#endif

            int taken = 0;

            foreach (Story beat in source.stories)
            {
                if (beat == null || beat.level == null || beat.level.content == null)
                {
                    continue;
                }

                if (!pool.Contains(beat.level))
                {
                    pool.Add(beat.level);
                    taken++;
                }
            }

            complaint.Append($"\n  {source.name} - {taken} of {source.stories.Length} beats had a level with content to play.");
        }

        if (pool.Count > 0)
        {
            hasComplained = false;
            return;
        }

        if (hasComplained)
        {
            // The pool is rebuilt on every wave while it is empty, and one line per attempt would
            // bury the one that says what is wrong.
            return;
        }

        // Said in full rather than as "no levels": every way this can happen is a different thing to
        // go and fix, and which one it is cannot be told apart from the outside.
        Debug.LogError($"[{nameof(EndlessScenario)}] {name} was dealt nothing out of {levelSources.Count} sources:{complaint}", this);
        hasComplained = true;
    }

    // One cycle is the whole pool, shuffled. Wave by wave the player is handed the next entry of
    // the cycle they are in, and when it runs out the next cycle is shuffled from a seed derived
    // from the same run - so no level comes up twice before every other one has, and the order is
    // still different every run.
    private List<LevelData> Deal(int cycle)
    {
        int seed = Seed;

        if (dealtCycleNumber == cycle && dealtCycleSeed == seed && dealtCycle != null)
        {
            return dealtCycle;
        }

        uint state = StateFor(seed, cycle);
        List<LevelData> order = Shuffled(ref state);

        AvoidRepeatingAcrossCycles(order, cycle, ref state);

        dealtCycle = order;
        dealtCycleNumber = cycle;
        dealtCycleSeed = seed;

        return dealtCycle;
    }

    // The one place a shuffle can still hand the same level out twice in a row is the seam between
    // two cycles, and back to back is precisely where a repeat is noticed. The clash is swapped out
    // deterministically, so the sequence stays the one the seed describes.
    private void AvoidRepeatingAcrossCycles(List<LevelData> order, int cycle, ref uint state)
    {
        if (cycle <= 0 || order.Count < 2)
        {
            return;
        }

        LevelData lastOfPrevious = LastOfCycle(cycle - 1);

        if (lastOfPrevious == null || order[0] != lastOfPrevious)
        {
            return;
        }

        // Anywhere but the last slot. What the next cycle checks its own seam against is this
        // cycle's last entry, and that is worked out from the seed alone - leaving the last one
        // where the shuffle put it is what keeps the two answers the same. A pool of two has
        // nowhere else to put it, and no game has a pool of two.
        int swapWith = order.Count > 2
            ? 1 + (int)(NextRandom(ref state) % (uint)(order.Count - 2))
            : 1;

        LevelData held = order[0];
        order[0] = order[swapWith];
        order[swapWith] = held;
    }

    // Worked out from the seed rather than by asking Deal for the previous cycle: Deal is what
    // called this, and handing it back its own half-built state - or letting it recurse a cycle at
    // a time back to zero - is a trap not worth setting. The seam correction above leaves the last
    // entry alone, so the shuffle on its own is the whole answer.
    private LevelData LastOfCycle(int cycle)
    {
        if (Pool.Count == 0)
        {
            return null;
        }

        uint state = StateFor(Seed, cycle);
        List<LevelData> order = Shuffled(ref state);

        return order[order.Count - 1];
    }

    // Fisher-Yates on our own generator rather than on UnityEngine.Random: this has to deal the
    // same order on every machine and in every session, and the shared generator is neither ours to
    // reseed nor promised to keep its sequence between Unity versions.
    private List<LevelData> Shuffled(ref uint state)
    {
        var order = new List<LevelData>(Pool);

        for (int i = order.Count - 1; i > 0; i--)
        {
            int swapWith = (int)(NextRandom(ref state) % (uint)(i + 1));
            LevelData held = order[i];
            order[i] = order[swapWith];
            order[swapWith] = held;
        }

        return order;
    }

    // What the ramp is measured from. A seed of zero is what PlayerPrefs answers for a run that
    // never rolled one - an endless game started by some other road than the menu button - and any
    // fixed number will do in its place, as long as it is the same one every time it is asked for.
    public int Seed
    {
        get
        {
            int stored = runSeed != null ? runSeed.IntValue : 0;
            return stored != 0 ? stored : 1;
        }
    }

    // Rolled when a run begins, and saved: a new run is a new order, and the run that is saved
    // keeps the order it was dealt.
    public void RollNewSeed()
    {
        if (runSeed == null)
        {
            Debug.LogWarning($"[{nameof(EndlessScenario)}] {name} has no runSeed setting - every run will be dealt the same order.", this);
            return;
        }

        runSeed.SetIntValue(Random.Range(1, int.MaxValue));
        SaveManager.Save();

        // The cycle held from the run before was dealt from the old seed, and every wave of the new
        // run would otherwise be read out of it until the pool ran out.
        dealtCycle = null;
        dealtCycleNumber = -1;
    }

    // The pace the wave is played at. baseGameSpeed is where the chosen difficulty starts, maxSpeed
    // its ceiling, and the ramp climbs part of the way between them over the first waves. Clamping
    // is left to GameSpeedManager, which does it against the same difficulty for every other reason
    // the speed can change.
    public float SpeedForWave(DifficultySettings difficulty, int wave)
    {
        if (difficulty == null)
        {
            return 1f;
        }

        float headroom = Mathf.Max(0f, difficulty.maxSpeed - difficulty.baseGameSpeed) * Mathf.Clamp01(speedRampShare);

        return difficulty.baseGameSpeed + headroom * RampProgress(wave);
    }

    // Nothing on the first wave, everything by wavesToTopSpeed, and straight between them: a curve
    // would be prettier and impossible for a player to feel the shape of.
    private float RampProgress(int wave)
    {
        if (wavesToTopSpeed <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((wave - 1) / (float)wavesToTopSpeed);
    }

    // What a point is worth this deep in. The first wave pays as collected, and every wave after
    // that pays more, up to a ceiling - without one, a long enough run would leave every other run
    // on the board unreachable.
    public float ScoreMultiplierForWave(int wave)
    {
        float multiplier = 1f + scoreMultiplierPerWave * Mathf.Max(0, wave - 1);

        return Mathf.Clamp(multiplier, 1f, Mathf.Max(1f, maxScoreMultiplier));
    }

    // Every so many waves, and never on the first: the run has to have been survived for a while
    // before it hands anything back.
    public bool GrantsLifeOnWave(int wave)
    {
        return extraLifeEveryWaves > 0 && wave > 1 && (wave - 1) % extraLifeEveryWaves == 0;
    }

    // xorshift32. Written out here rather than taken from System.Random because the sequence has to
    // be the same on every runtime the game is built for - a save file carries the seed, not the
    // order, and the order has to come back the same wherever it is read.
    private static uint NextRandom(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;

        return state;
    }

    private static uint StateFor(int seed, int cycle)
    {
        unchecked
        {
            uint state = (uint)seed * 2654435761u + (uint)cycle * 2246822519u;

            // Zero is the one state xorshift cannot leave, so it is the one state it may not start
            // in - it would deal the same level for the whole cycle.
            return state == 0u ? 1u : state;
        }
    }
}
