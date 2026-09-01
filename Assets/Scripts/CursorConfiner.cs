using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the mouse inside the game window while there is a game to play. On two monitors the paddle
// is steered by pushing the mouse sideways, and pushing it far enough walked the cursor onto the
// other screen - where the next click landed on whatever was over there and the game lost focus
// mid-rally.
//
// Confined rather than locked: the cursor stays visible and still points at things, it simply
// cannot leave. It is let go whenever the player has something to click - a window is open, or the
// menu is up - and whenever the window is not the one being used, so alt-tab is never a fight.
//
// Lives on GameSession, which outlives every scene load.
public class CursorConfiner : MonoBehaviour
{
    [Tooltip("The scene the cursor is held in. Every other scene lets it go.")]
    public string gameplaySceneName = "Gameplay";

    private string currentScene;
    private bool windowFocused = true;

    private void OnEnable()
    {
        currentScene = SceneManager.GetActiveScene().name;
        SceneLoader.OnSceneChanged += HandleSceneChanged;
        PauseManager.OnPause += Apply;
        PauseManager.OnUnpause += Apply;
        Apply();
    }

    private void OnDisable()
    {
        SceneLoader.OnSceneChanged -= HandleSceneChanged;
        PauseManager.OnPause -= Apply;
        PauseManager.OnUnpause -= Apply;

        // Never leave a player confined by a component that is no longer running.
        Cursor.lockState = CursorLockMode.None;
    }

    private void HandleSceneChanged(string sceneName)
    {
        currentScene = sceneName;
        Apply();
    }

    private void OnApplicationFocus(bool focused)
    {
        windowFocused = focused;
        Apply();
    }

    // Unity drops the confine of its own accord on focus loss and on some window changes, so this
    // is asked again every frame rather than only when something we know about happens. Writing the
    // same value it already holds costs nothing.
    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        Cursor.lockState = ShouldConfine ? CursorLockMode.Confined : CursorLockMode.None;
    }

    private bool ShouldConfine
    {
        get
        {
            if (!windowFocused || !Application.isFocused)
            {
                return false;
            }

            // A pause means a window is up - the shop, the options, the game-over screen - and every
            // one of those is something the player clicks their way through.
            return currentScene == gameplaySceneName && !PauseManager.IsPaused;
        }
    }
}
