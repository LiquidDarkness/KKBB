using UnityEngine;
using UnityEngine.SceneManagement;

public class WindowButton : MonoBehaviour
{
    public GameSession gameSession;
    public GameObject targetWindow;
    public GameObject menuWindow;
    public GameObject shopWindow;
    public ObjectActivationManager objectActivation;

    public void Awake()
    {
        gameSession = FindObjectOfType<GameSession>();
        objectActivation = FindObjectOfType<ObjectActivationManager>();
    }

    public void Update()
    {
        PullMenu();
    }

    public void CloseWindow()
    {
        targetWindow.SetActive(false);
        gameSession.Unpause();
        objectActivation.ResetObject();
    }

    public void OpenWindow()
    {
        objectActivation.ChangeSelectedObject(targetWindow);
        gameSession.Pause();
        targetWindow.SetActive(true);
    }

    // Escape is not read here any more. It has one owner now - EscapeShortcut on GameSession - which
    // closes whichever window is on top and opens the options window when none is. Two readers meant
    // a single press both closed this window and toggled the pause overlay, and the branch below
    // walked into menuWindow and objectActivation, neither of which is set outside the menu scene.
    public void PullMenu()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            return;
        }

        // Guarded: shopWindow is left empty on every instance of this component in the project, and
        // WindowManager already opens the shop on the same key.
        if (Input.GetKeyDown(KeyCode.Tab) && shopWindow != null && gameSession != null)
        {
            shopWindow.SetActive(true);
            gameSession.Pause();
        }
    }
}
