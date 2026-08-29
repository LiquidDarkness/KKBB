using System.Globalization;
using System.Threading.Tasks;
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

    [Tooltip("The window the summary sits in. Off until the player boinks the block for it, and it takes a pause lock under its own name while it is up - the name EscapeWindow gives back when Escape closes it.")]
    public GameObject window;

    [Tooltip("Registers this summary for the Gameplay scene to reach - the block that opens the window lives there and this lives on GameSession, which outlives it.")]
    public CoreReferences coreReferences;

    [Header("What it reads")]
    public DiffcultyManager diffcultyManager;
    public ScenarioManager scenarioManager;

    [Tooltip("Where the best score of every scenario is kept, as name=score;name=score.")]
    public TypeDistinguisher bestScores;

    [Tooltip("The running score and the lives left, read from what is saved rather than from Score.currentScore and PlayerHealth. A scenario can be walked into on its farewell beat, and the tally then runs before ScoreManager has read the save back into either of them.")]
    public TypeDistinguisher collectedScore;

    public TypeDistinguisher remainingLives;

    [Header("What a life is worth")]
    [Tooltip("Points for each life still in hand at the end. METAL hands out none, and pays for it with its multiplier instead.")]
    public int pointsPerRemainingLife = 750;

    [Header("Steam")]
    [Tooltip("How many places above and below the player to show once the score is on the ladder.")]
    public int neighboursAbove = 2;

    public int neighboursBelow = 2;

    private Breakdown last;
    private bool hasFinished;

    // Filled in later than everything else: posting a score and reading the ladder back is a round
    // trip to Steam, and the window is drawn long before the answer arrives.
    private string standings = string.Empty;

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

    private void Awake()
    {
        // This component rides GameSession, which outlives every scene; the Gameplay scene reaches
        // it through the same shared asset GameEnding uses to find the story manager.
        if (coreReferences != null)
        {
            coreReferences.scenarioScoreSummary = this;
        }
    }

    private void OnEnable()
    {
        StoryManager.OnScenarioFinished += HandleScenarioFinished;
        // Not OnGameplayLoaded: that fires after the scene has already started, and a scenario
        // entered on its farewell beat has worked its summary out by then - the wipe would land on
        // top of it. SummaryBoinkController clears this from the scene instead, before any beat is
        // shown. Leaving for the menu is safe to catch here, there being nothing left to undo.
        SceneLoader.OnMenuLoaded += Hide;
        TranslationJSONDeserializer.OnTransaltionUpdated += Redraw;
        Hide();
    }

    private void OnDisable()
    {
        StoryManager.OnScenarioFinished -= HandleScenarioFinished;
        SceneLoader.OnMenuLoaded -= Hide;
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
        standings = string.Empty;
        Redraw();
        PostToSteam(last);
    }

    // Fire and forget on purpose: the run is already counted, saved and recorded on this machine, so
    // the ladder is a nicety that redraws the window if and when it answers.
    private async void PostToSteam(Breakdown result)
    {
        int[] details = { result.Collected, result.Lives, Mathf.RoundToInt(result.Multiplier * 100f) };
        Steamworks.Data.LeaderboardEntry[] entries = await Leaderboards.PostAndReadAsync(
            result.Scenario, result.Difficulty, result.Total, details, neighboursAbove, neighboursBelow);

        // Away for as long as Steam took to answer: the scenario may have been left behind, and a
        // summary that has been forgotten must not be redrawn with a ladder from the run before.
        if (this == null || !hasFinished || !Equals(result.Scenario, last.Scenario) || result.Total != last.Total)
        {
            return;
        }

        standings = Standings(entries);
        Redraw();
    }

    private string Standings(Steamworks.Data.LeaderboardEntry[] entries)
    {
        if (entries == null || entries.Length == 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        text.Append("\n\n");
        text.Append($"<size=110%><align=center>{Translate("Steam leaderboard")}</align></size>");

        ulong self = Steamworks.SteamClient.IsValid ? Steamworks.SteamClient.SteamId.Value : 0;

        foreach (Steamworks.Data.LeaderboardEntry entry in entries)
        {
            string who = $"{entry.GlobalRank}. {NameOf(entry.User)}";
            string score = Number(entry.Score);

            // The player's own line stands out from the neighbours, the same way the final score
            // stands out from what went into it.
            if (self != 0 && entry.User.Id.Value == self)
            {
                who = Bold(who);
                score = Bold(score);
            }

            text.Append('\n');
            text.Append(Row(who, score));
        }

        return text.ToString();
    }

    // Called by the block through its CollisionExposer, and by a close button inside the window.
    [ContextMenu(nameof(ToggleWindow))]
    public void ToggleWindow()
    {
        if (window == null)
        {
            return;
        }

        if (window.activeSelf)
        {
            CloseWindow();
        }
        else
        {
            OpenWindow();
        }
    }

    public void OpenWindow()
    {
        if (window == null || window.activeSelf)
        {
            return;
        }

        window.SetActive(true);
        // Locked under the window's own name: EscapeWindow, closing a window nobody else claims,
        // gives the lock back under exactly that name.
        PauseManager.Pause(window.name);
    }

    public void CloseWindow()
    {
        if (window == null || !window.activeSelf)
        {
            return;
        }

        window.SetActive(false);
        PauseManager.Unpause(window.name);
    }

    // Steam answers with a name only when it has one to hand, and asking at all throws outright
    // when there is no client behind it. A player whose name has not arrived is still worth a line -
    // it is the rank and the score the ladder is about - and this runs inside a redraw, where an
    // exception would leave the window holding half a summary.
    private static string NameOf(Steamworks.Friend user)
    {
        try
        {
            string name = user.Name;
            return string.IsNullOrEmpty(name) ? "..." : name;
        }
        catch (System.Exception)
        {
            return "...";
        }
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
        int lives = Mathf.Max(0, remainingLives != null ? remainingLives.IntValue : PlayerHealth.Health);
        int collected = collectedScore != null ? collectedScore.IntValue : Score.currentScore;
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

    // Back to how a scenario starts: no window, nothing written, no tally standing. Called from the
    // scene as it comes up, so a summary cannot be left over from the run before.
    public void Forget()
    {
        Hide();
    }

    private void Hide()
    {
        hasFinished = false;

        standings = string.Empty;

        if (summary != null)
        {
            summary.text = string.Empty;
        }

        if (window != null)
        {
            // Straight off rather than through CloseWindow: there is no pause of ours to give back
            // when the scene has only just started, and PauseManager was cleared by the load.
            window.SetActive(false);
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
        text.Append(Header());
        text.Append("\n\n");
        text.Append(Row(Translate("Points collected"), Number(last.Collected)));
        text.Append('\n');
        text.Append(Row($"{Translate("Lives left")} ({last.Lives})", Number(last.LivesReward)));
        text.Append('\n');
        text.Append(Row(Translate("Difficulty"), "x" + last.Multiplier.ToString("0.##", CultureInfo.CurrentCulture)));
        text.Append('\n');
        text.Append(Row(Bold(Translate("Final score")), Bold(Number(last.Total))));
        text.Append(standings);

        summary.text = text.ToString();
    }

    // What there is to beat, said at the top, or the fact that it has just been beaten.
    private string Header()
    {
        string line = last.IsRecord
            ? Translate("A new record on this difficulty!")
            : $"{Translate("Best on this difficulty")}: {Number(last.Best)}";

        return $"<size=125%><align=center>{line}</align></size>";
    }

    // Words to the left, number to the right, dots strung between them. The dots are counted rather
    // than guessed at: TMP measures both halves in the font actually in use, so the column lines up
    // whatever the language says and whatever size the player has set the text to.
    private string Row(string label, string value)
    {
        const string Gap = " ";
        float available = summary.rectTransform.rect.width;
        float dotWidth = summary.GetPreferredValues(".").x;
        float used = summary.GetPreferredValues(label + Gap + Gap + value).x;

        // A rect that has not been laid out yet measures zero, which would ask for a nonsense number
        // of dots; a short fixed run is the honest answer until there is a width to fill.
        int dots = available > 1f && dotWidth > 0f
            ? Mathf.FloorToInt((available - used) / dotWidth)
            : 3;

        return label + Gap + new string('.', Mathf.Clamp(dots, 2, 200)) + Gap + value;
    }

    private static string Bold(string text)
    {
        return $"<b>{text}</b>";
    }

    private static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
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
