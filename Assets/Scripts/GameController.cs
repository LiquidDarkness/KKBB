using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject window;
    public CoreReferences coreReferences;

    [SerializeField] private string sceneName;
    [SerializeField] private string menuSceneName;

    public void PurgeGameProgress()
    {
        Debug.Log("Purging");
        PersistentSettings.PurgePlayerPrefs();
        SaveManager.Save();
    }

    public void NewGame()
    {
        //PlayerPrefs.Save();
        //SaveManager.Save();
        coreReferences.sceneLoader.LoadScene(sceneName);
    }

    public void ContinueGame()
    {
        //SaveManager.Load();
        coreReferences.sceneLoader.LoadScene(sceneName);
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

        if (coreReferences.sceneLoader != null)
        {
            Debug.Log("coreReferences.sceneLoader istnieje, próbujemy za³adowaæ scenê: " + menuSceneName);
            coreReferences.sceneLoader.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("coreReferences.sceneLoader nie przypisany!");
            //SceneManager.LoadScene(menuSceneName);
        }
    }

}
