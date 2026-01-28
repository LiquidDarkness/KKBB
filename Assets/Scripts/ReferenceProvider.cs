using UnityEngine;

public class ReferenceProvider : MonoBehaviour
{
    public MusicSwitcher musicSwitcher;
    public LoadingScreen loadingScreen;
    public GameSession gameSession;
    public StoryManager storyManager;

    public CoreReferences coreReferences;

    public bool injectMusicSwitcher;
    public bool injectLoadingScreen;
    public bool injectGameSession;
    public bool injectStoryManager;

    public void Awake()
    {
        if (injectMusicSwitcher)
        {
            coreReferences.musicSwitcher = musicSwitcher;
        }

        if (injectLoadingScreen)
        {
            coreReferences.loadingScreen = loadingScreen;
        }

        if (injectGameSession)
        {
            coreReferences.gameSession = gameSession;
        }

        if (injectStoryManager)
        {
            coreReferences.storyManager = storyManager;
        }
    }
}