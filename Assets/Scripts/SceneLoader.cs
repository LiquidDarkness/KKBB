using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    //TODO: fade out z obecnej sceny do kolejnej, pomiêdzy randomowy ekran ³adowania

    //public Image fadePanel; // Panele s³u¿¹ce do fade-out i fade-in
    //public Color fadeColor = Color.black; // Kolor fade-out/fade-in
    //public float fadeDuration = 1.0f; // Czas trwania fade-out/fade-in
    //public float delayBetweenFades = 1.0f;

    public static event Action OnGameplayLoaded;
    public static event Action OnMenuLoaded;
    public static event Action<string> OnSceneChanged;

    public CoreReferences coreReferences;

    public void LoadScene(string sceneName)
    {
        LoadingScreen loadingScreen = coreReferences.loadingScreen;
        loadingScreen.FadeToBlack(() =>
        {
            //Dzia³a jak event Action, ale nie ma potrzeby subskrybowania siê i odsubrybowania,
            //wydarzy siê jednorazowo, ale bêdzie dzia³a³o za ka¿dym wywo³anie LoadLevel z odpowiedni¹ zawartoœci¹.
            Debug.Log("Transitioning to: " + sceneName);
            // Always run on the persistent LoadingScreen, never on `this`: this SceneLoader
            // instance might live in the very scene about to be unloaded (e.g. a trigger placed
            // in Gameplay), in which case it would be destroyed mid-transition, silently
            // killing the coroutine before FadeToClear() runs.
            loadingScreen.StartCoroutine(TransitionSequence(sceneName, loadingScreen));
        });
    }

    IEnumerator TransitionSequence(string sceneName, LoadingScreen loadingScreen)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName,LoadSceneMode.Single);
        yield return null;

        // No pause may cross a scene boundary: the window or the death that took the lock
        // is gone with the scene it lived in, and an inherited lock leaves the fresh scene
        // frozen - a paddle that will not move being the first thing the player notices.
        PauseManager.ReleaseAll();

        switch (sceneName)
        {
            case "Gameplay":
                // Load before the event, not after: subscribers read PlayerPrefs
                // (ScoreManager pulls the saved score out of it), and on the first
                // gameplay load of the app SaveManager.Load is what puts the save file
                // into PlayerPrefs in the first place. Firing the event first handed
                // those subscribers the pre-load values.
                SaveManager.Load();
                OnGameplayLoaded?.Invoke();
                break;

            case "Menu":
                OnMenuLoaded?.Invoke();
                break;

            default:
                break;
        }

        OnSceneChanged?.Invoke(sceneName);
        loadingScreen.FadeToClear();
    }
}
