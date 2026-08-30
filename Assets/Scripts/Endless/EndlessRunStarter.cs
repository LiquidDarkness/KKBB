using UnityEngine;

// The menu end of endless: what the button on the main menu calls before it hands the player over
// to the difficulty screen.
//
// It does what a scenario button does - names the scenario, and puts the beat and the score back to
// nothing there and then, rather than leaving it to the New Game the difficulty button calls. A
// player who backs out of the difficulty screen has still changed which scenario is chosen, and
// Continue would otherwise pick an endless run up on whichever beat the last story was left at, with
// that story's score still standing.
//
// The one thing it does that a scenario button cannot is deal the run a fresh seed, which is what
// makes this run's order of levels its own.
public class EndlessRunStarter : MonoBehaviour
{
    public ScenarioManager scenarioManager;

    [Tooltip("The endless scenario itself. It has to be in the scenarios list on the manager above, or the pick is refused and the player starts whatever was chosen last.")]
    public EndlessScenario endless;

    [Tooltip("currentLvl - which wave the run is on. Zeroed here, so the run starts at the first one.")]
    public TypeDistinguisher currentLvl;

    [Tooltip("scoreValue - the score carried in the save. Zeroed for the same reason.")]
    public TypeDistinguisher savedScore;

    public void StartNewRun()
    {
        if (scenarioManager == null || endless == null)
        {
            Debug.LogError($"[{nameof(EndlessRunStarter)}] Nothing to start - the scenario manager or the endless scenario is not assigned.", this);
            return;
        }

        scenarioManager.SetSettings(endless);

        Clear(currentLvl);
        Clear(savedScore);

        endless.RollNewSeed();
    }

    private static void Clear(TypeDistinguisher value)
    {
        if (value == null)
        {
            Debug.LogWarning($"[{nameof(EndlessRunStarter)}] A value to clear is not assigned - it will carry over from the run before.");
            return;
        }

        value.SetIntValue(0);
    }
}
