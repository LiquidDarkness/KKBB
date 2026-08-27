using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

// What the run was worth, said once, on the screen that ends a scenario. Until now the score simply
// stopped being mentioned there: the drops had been counted all the way through and then nobody
// added them up.
//
// The tally is the points collected from drops, plus what is left of the lives, all of it worth
// more on a harder setting - which is the only thing that makes a METAL run comparable to an Easy
// one. The result is kept as that scenario best, and beating it is called out.
public class ScenarioScoreSummary : MonoBehaviour
{
    [Header("Where it is written")]
    public TextMeshProUGUI summary;

    [Header("What it reads")]
    public DiffcultyManager diffcultyManager;
    public ScenarioManager scenarioManager;

    [Tooltip("Where the best score of every scenario is kept, as name=score;name=score.")]
    public TypeDistinguisher bestScores;

    [Header("What a life is worth")]
    [Tooltip("Points for each life still in hand at the end. METAL hands out none, and pays for it with its multiplier instead.")]
    public int pointsPerRemainingLife = 250;

    private Breakdown last;
    private bool hasFinished;

    public struct Breakdown
    {
        public string Scenario;
        public string Difficulty;
        public int Collected;
        public int Lives;
        public int LivesReward;
        public float Multiplier;
        public int Total;
        public int Best;
        public bool IsRecord;
    }

    private void OnEnable()
    {
        StoryManager.OnScenarioFinished += HandleScenarioFinished;
        TranslationJSONDeserializer.OnTransaltionUpdated += Redraw;
        Hide();
    }

    private void OnDisable()
    {
        StoryManager.OnScenarioFinished -= HandleScenarioFinished;
        TranslationJSONDeserializer.OnTransaltionUpdated -= Redraw;
    }

    public Breakdown LastResult => last;

    // StoryManager fires this twice at the end of a scenario: once from Progress, when the last
    // beat turns the ending on, and again from GameEnding.Start as the ending object wakes up and
    // asks for the same thing. Counting twice would be harmless arithmetic but not a harmless
    // reading: the first tally stores the record, so the second one finds it already standing and
    // reports the run as having merely equalled it.
    private void HandleScenarioFinished()
    {
        if (hasFinished)
        {
            return;
        }

        last = Tally();
        hasFinished = true;
        Redraw();
    }

    public Breakdown Tally()
    {
        DifficultySettings difficulty = diffcultyManager != null ? diffcultyManager.CurrentSettings : null;
        StoryContainer scenario = scenarioManager != null ? scenarioManager.CurrentScenarioSettings : null;

        // A multiplier of zero would wipe the whole run out, and a negative one would owe the
        // player points, so anything that is not a real multiplier is treated as "as collected".
        float multiplier = difficulty != null && difficulty.scoreMultiplier > 0f ? difficulty.scoreMultiplier : 1f;

        // Death is declared at -1, so the last life is spent at 0 and a finished run can honestly
        // hold none - on METAL it never holds any.
        int lives = Mathf.Max(0, PlayerHealth.Health);
        int collected = Score.currentScore;
        int livesReward = lives * pointsPerRemainingLife;

        var breakdown = new Breakdown
        {
            Scenario = scenario != null ? scenario.name : string.Empty,
            Difficulty = difficulty != null ? difficulty.name : string.Empty,
            Collected = collected,
            Lives = lives,
            LivesReward = livesReward,
            Multiplier = multiplier,
            Total = Mathf.RoundToInt((collected + livesReward) * multiplier),
        };

        // Each scenario keeps a record per difficulty: a run on Easy has nothing to say about the
        // same scenario played on METAL, and only one of them would ever be worth beating if they
        // shared a number.
        breakdown.Best = ScenarioRecords.Best(bestScores, breakdown.Scenario, breakdown.Difficulty);
        breakdown.IsRecord = ScenarioRecords.TrySetBest(bestScores, breakdown.Scenario, breakdown.Difficulty, breakdown.Total);

        // Read before the write, so the line can say what the record was to beat; once beaten, the
        // new total is the record and there is nothing older worth showing.
        if (breakdown.IsRecord)
        {
            breakdown.Best = breakdown.Total;
        }

        return breakdown;
    }

    private void Hide()
    {
        hasFinished = false;

        if (summary != null)
        {
            summary.text = string.Empty;
        }
    }

    // Redrawn rather than rebuilt on a language change: the numbers were settled the moment the
    // scenario ended, and switching language in the options must not re-award the record.
    private void Redraw()
    {
        if (summary == null || !hasFinished)
        {
            return;
        }

        var text = new StringBuilder();
        text.Append(Line(Translate("Points collected"), last.Collected.ToString("N0", CultureInfo.CurrentCulture)));
        text.Append('\n');
        text.Append(Line($"{Translate("Lives left")} ({last.Lives})", last.LivesReward.ToString("N0", CultureInfo.CurrentCulture)));
        text.Append('\n');
        text.Append(Line(Translate("Difficulty"), "x" + last.Multiplier.ToString("0.##", CultureInfo.CurrentCulture)));
        text.Append('\n');
        text.Append(Line(Translate("Final score"), last.Total.ToString("N0", CultureInfo.CurrentCulture)));
        text.Append('\n');
        text.Append(last.IsRecord
            ? Translate("A new record on this difficulty!")
            : Line(Translate("Best on this difficulty"), last.Best.ToString("N0", CultureInfo.CurrentCulture)));

        summary.text = text.ToString();
    }

    private static string Line(string label, string value)
    {
        return $"{label}: {value}";
    }

    // The same fallback TranslationMediator uses: a key with no translation behind it shows as
    // itself, which reads as English and says plainly what is missing from the JSON.
    private static string Translate(string key)
    {
        if (TranslationJSONDeserializer.dataDictionary != null
            && TranslationJSONDeserializer.dataDictionary.TryGetValue(key, out string translated)
            && !string.IsNullOrEmpty(translated))
        {
            return translated;
        }

        return key;
    }
}
