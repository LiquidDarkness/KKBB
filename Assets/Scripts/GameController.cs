using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public SceneLoader sceneLoader;
    public GameObject window;
    public CoreReferences coreReferences;

    [SerializeField] private string sceneName;
    [SerializeField] private string menuSceneName;

    public void Awake()
    {
        //SaveManager.Load();
        if (sceneLoader == null)
        {
            sceneLoader = FindObjectOfType<SceneLoader>();
        }
    }

    public void PurgeGameProgress()
    {
        PersistentSettings.PurgePlayerPrefs();
        SaveManager.Save();
    }

    public void NewGame()
    {
        //PlayerPrefs.Save();
        //SaveManager.Save();
        sceneLoader.LoadScene(sceneName);
    }

    public void ContinueGame()
    {
        //SaveManager.Load();
        sceneLoader.LoadScene(sceneName);
    }

    public void OpenCreditsWindow()
    {
        window.SetActive(true);
    }

    public void CloseCreditsWindow()
    {
        window.SetActive(false);
    }

    public void ReturnToMenu()
    {
        Debug.Log("ReturnToMenu wywo³ane!");

        if (sceneLoader != null)
        {
            Debug.Log("SceneLoader istnieje, próbujemy za³adowaæ scenê: " + menuSceneName);
            sceneLoader.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("SceneLoader nie przypisany!");
            //SceneManager.LoadScene(menuSceneName);
        }
    }

}
