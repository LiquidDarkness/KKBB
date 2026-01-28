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

    public void PullMenu()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindow();
            }
        }

        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (objectActivation.selectedObject == menuWindow)
                {
                    CloseWindow();
                }

                if (objectActivation.selectedObject != targetWindow)
                {
                    CloseWindow();
                }

                menuWindow.SetActive(true);
                gameSession.Pause();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                shopWindow.SetActive(true);
                gameSession.Pause();
            }
        }
    }
}
