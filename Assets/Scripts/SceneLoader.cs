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
    public static event Action OnSceneChanged;

    public CoreReferences coreReferences;

    private GameObject tempRunner;
    public void LoadScene(string sceneName)
    {
        coreReferences.loadingScreen.FadeToBlack(() =>
        {
            //Dzia³a jak event Action, ale nie ma potrzeby subskrybowania siê i odsubrybowania,
            //wydarzy siê jednorazowo, ale bêdzie dzia³a³o za ka¿dym wywo³anie LoadLevel z odpowiedni¹ zawartoœci¹.
            Debug.Log("Transitioning to: " + sceneName);
            IEnumerator routine = TransitionSequence(sceneName);
            Debug.Log($"Routine exists: {routine != null}");
            if (this.isActiveAndEnabled)
            {
                StartCoroutine(routine);
            }
            else
            {
                StartRemoteRoutine(routine);
            }
        });
    }

    private void StartRemoteRoutine(IEnumerator routine)
    {
        tempRunner = new GameObject();
        DontDestroyOnLoad(tempRunner);
        tempRunner.AddComponent<DummyBehaviour>().StartCoroutine(routine);
    }

    class DummyBehaviour : MonoBehaviour {}

    IEnumerator TransitionSequence(string sceneName)
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName,LoadSceneMode.Single);
        yield return null;

        switch (sceneName)
        {
            case "Gameplay":
                Debug.Log("Calling OnGameplayLoaded: " + OnGameplayLoaded != null);
                OnGameplayLoaded?.Invoke();
                Debug.Log("Loading saved game.");
                SaveManager.Load();
                break;

            case "Menu":
                OnMenuLoaded.Invoke();
                break;

            default:
                break;
        }

        Debug.Log("Calling OnSceneChanged: " + OnSceneChanged != null);
        OnSceneChanged?.Invoke();
        coreReferences.loadingScreen.FadeToClear();

        if (tempRunner != null)
        {
            Destroy(tempRunner);
        }
    }
}