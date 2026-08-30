using UnityEngine;

// Everything an endless run needs that a written scenario does not: what a point is worth this deep
// in, the life handed back every few waves, and the fact that death is the end of the run rather
// than an offer to try the level again.
//
// It lives in the Gameplay scene, like the story manager whose beats it listens to, and answers to
// the scenario that is actually being played - on anything but an endless one it puts the score
// multiplier back to one and then keeps quiet.
public class EndlessRunController : MonoBehaviour
{
    [Header("What is being played")]
    public ScenarioManager scenarioManager;

    [Tooltip("Which beat the player is on, which in an endless run is which wave.")]
    public TypeDistinguisher currentLvl;

    [Tooltip("Where the score summary is found. It rides GameSession, which outlives this scene, and registers itself into this shared asset.")]
    public CoreReferences coreReferences;

    private void OnEnable()
    {
        StoryManager.OnBeatShown += HandleBeatShown;
        PlayerHealth.OnDeath += HandleDeath;
        ContinuePurchase.OnContinued += HandleContinued;

        // A run that is not endless must not be left playing under the multiplier of one that was.
        // Nothing else puts it back: Score is static and outlives every scene, the way the score
        // itself does.
        if (!IsEndless)
        {
            Score.pointsMultiplier = 1f;
        }
    }

    private void OnDisable()
    {
        StoryManager.OnBeatShown -= HandleBeatShown;
        PlayerHealth.OnDeath -= HandleDeath;
        ContinuePurchase.OnContinued -= HandleContinued;

        Score.pointsMultiplier = 1f;
    }

    public EndlessScenario Endless =>
        scenarioManager != null ? scenarioManager.CurrentScenarioSettings as EndlessScenario : null;

    private bool IsEndless => Endless != null;

    // Counted from one, the way it is said on the card.
    public int Wave => currentLvl != null ? EndlessScenario.WaveOf(currentLvl.IntValue) : 1;

    private void HandleBeatShown(int beat)
    {
        EndlessScenario endless = Endless;

        if (endless == null)
        {
            Score.pointsMultiplier = 1f;
            return;
        }

        int wave = EndlessScenario.WaveOf(beat);

        Score.pointsMultiplier = endless.ScoreMultiplierForWave(wave);

        GrantLifeIfDue(endless, wave);
    }

    // The reward for staying alive, and the one thing that keeps a long run from being decided
    // entirely by the difficulty picked at the start. Deliberately without a ceiling: a player deep
    // enough into a run to have collected a pile of hearts has earned every one of them.
    private void GrantLifeIfDue(EndlessScenario endless, int wave)
    {
        if (!endless.GrantsLifeOnWave(wave))
        {
            return;
        }

        if (PlayerHealth.healthTD == null)
        {
            // PlayerRig has not initialised the health yet, and handing out a life before it does
            // would be written over by the difficulty the moment it does.
            return;
        }

        PlayerHealth.GainHealth();
    }

    // Death ends an endless run: there is no ending block to boink and no farewell beat to walk
    // into, so this is where the tally is taken and put on screen. The game over screen is coming
    // up underneath at the same moment - it answers the same signal - and the summary sits over it
    // until the player closes it.
    private void HandleDeath()
    {
        if (!IsEndless)
        {
            return;
        }

        ScenarioScoreSummary summary = coreReferences != null ? coreReferences.scenarioScoreSummary : null;

        if (summary == null)
        {
            Debug.LogWarning($"[{nameof(EndlessRunController)}] No summary registered in {nameof(CoreReferences)} - the run ends without saying what it was worth.", this);
            return;
        }

        // Score.currentScore rather than the saved one: the saved score is only written when a
        // level is completed, and this death happened part way through one.
        summary.FinishRun(Wave, Score.currentScore);
        summary.OpenWindow();
    }

    // A continue puts the same run back on its feet, so the tally taken at that death is no longer
    // the end of anything. Forgetting it takes the summary off the screen and arms it again for
    // the death that does finish the run.
    private void HandleContinued()
    {
        if (!IsEndless)
        {
            return;
        }

        ScenarioScoreSummary summary = coreReferences != null ? coreReferences.scenarioScoreSummary : null;

        if (summary != null)
        {
            summary.Forget();
        }
    }
}
