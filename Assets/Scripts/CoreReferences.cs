using UnityEngine;

[CreateAssetMenu]
public class CoreReferences : ScriptableObject
{
    public MusicSwitcher musicSwitcher;
    public LoadingScreen loadingScreen;
    public GameSession gameSession;
    public StoryManager storyManager;
    public SceneLoader sceneLoader;

    // Lives on GameSession, so anything in the Gameplay scene - the block that opens it, above all
    // - reaches it the same way GameEnding reaches the story manager.
    public ScenarioScoreSummary scenarioScoreSummary;

    [ContextMenu("ValidateReferences")]
    public void ValidateReferences()
    {
        Debug.Assert(musicSwitcher != null);
        Debug.Assert(loadingScreen != null);
        Debug.Assert(gameSession != null);
        Debug.Assert(storyManager != null);
        Debug.Assert(sceneLoader != null);
        Debug.Assert(scenarioScoreSummary != null);
    }
}