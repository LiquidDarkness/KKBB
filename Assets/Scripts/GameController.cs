using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject window;
    public CoreReferences coreReferences;

    [SerializeField] private string sceneName;
    [SerializeField] private string menuSceneName;

    [Header("Cleared when starting a New Game")]
    public TypeDistinguisher savedScore;
    public TypeDistinguisher currentLevel;
    public TypeDistinguisher health;

    public void PurgeGameProgress()
    {
        Debug.Log("Purging");
        PersistentSettings.PurgePlayerPrefs();
        SaveManager.Save();
    }

    public void NewGame()
    {
        ResetRunProgress();
        coreReferences.sceneLoader.LoadScene(sceneName);
    }

    // Score, story position and lives belong to a single playthrough. Without clearing them a
    // new game inherits whatever the last run ended on: SaveManager.Load runs only once per app
    // launch, so the old values simply stay in PlayerPrefs. Settings (volume, resolution,
    // autoscroll, chosen scenario and difficulty) are deliberately left alone.
    private void ResetRunProgress()
    {
        ClearValue(savedScore);
        ClearValue(currentLevel);
        ClearValue(health);
        SaveManager.Save();
    }

    private static void ClearValue(TypeDistinguisher value)
    {
        if (value == null)
        {
            Debug.LogWarning("GameController: a New Game reset target is not assigned - that value will carry over from the previous run.");
            return;
        }

        // Health treats 0 as uninitialised and refills from the difficulty on the next Initialize.
        value.SetIntValue(0);
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
