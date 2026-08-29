using UnityEngine;

// The scene half of the score summary. The window and the tally live on GameSession, which outlives
// every scene; the block the player boinks to see them cannot, so it needs someone here to put it on
// screen at the right moment and to pass the boink along.
public class SummaryBoinkController : MonoBehaviour
{
    [Tooltip("Where the summary itself is found. It registers into this shared asset on GameSession, the same way the story manager does.")]
    public CoreReferences coreReferences;

    [Tooltip("The block in the right-hand slot, the one the continue block uses during play. Off until the scenario ends.")]
    public GameObject block;

    private void OnEnable()
    {
        StoryManager.OnScenarioFinished += Show;

        // The summary itself is never reloaded - it rides GameSession - so the scene coming up is
        // what has to clear the last run out of it. Here rather than in the summary because this
        // runs before any story beat is shown, and a scenario entered on its farewell beat works
        // its tally out during that very first frame.
        if (Summary != null)
        {
            Summary.Forget();
        }

        Toggle(false);
    }

    private void OnDisable()
    {
        StoryManager.OnScenarioFinished -= Show;
    }

    private ScenarioScoreSummary Summary => coreReferences != null ? coreReferences.scenarioScoreSummary : null;

    private void Show()
    {
        Toggle(true);
    }

    private void Toggle(bool shouldShow)
    {
        if (block != null)
        {
            block.SetActive(shouldShow);
        }
    }

    // Wired to the block's CollisionExposer: boinking it opens the window, boinking it again shuts
    // it, which is the same on-off the shop and the pause screen give the player.
    public void ToggleWindow()
    {
        ScenarioScoreSummary summary = Summary;

        if (summary == null)
        {
            Debug.LogWarning($"[{nameof(SummaryBoinkController)}] No summary registered in {nameof(CoreReferences)} - the block has nothing to open.", this);
            return;
        }

        summary.ToggleWindow();
    }
}
