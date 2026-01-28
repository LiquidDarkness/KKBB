using UnityEngine;

[CreateAssetMenu]
public class CoreReferences : ScriptableObject
{
    public MusicSwitcher musicSwitcher;
    public LoadingScreen loadingScreen;
    public GameSession gameSession;
    public StoryManager storyManager;

    [ContextMenu("ValidateReferences")]
    public void ValidateReferences()
    {
        Debug.Assert(musicSwitcher != null);
        Debug.Assert(loadingScreen != null);
        Debug.Assert(gameSession != null);
        Debug.Assert(storyManager != null);
    }
}