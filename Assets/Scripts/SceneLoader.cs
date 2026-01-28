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

    public void LoadScene(string sceneName)
    {
        coreReferences.loadingScreen.FadeToBlack(() =>
        {
            //Dzia³a jak event Action, ale nie ma potrzeby subskrybowania siê i odsubrybowania,
            //wydarzy siê jednorazowo, ale bêdzie dzia³a³o za ka¿dym wywo³anie LoadLevel z odpowiedni¹ zawartoœci¹.
            Debug.Log("Transitioning to: " + sceneName);
            StartCoroutine(TransitionSequence(sceneName));
        });
    }

    IEnumerator TransitionSequence(string sceneName)
    {
        SceneManager.LoadScene(sceneName,LoadSceneMode.Single);
        yield return null;

        switch (sceneName)
        {
            case "Gameplay":
                OnGameplayLoaded.Invoke();
                SaveManager.Load();
                break;

            case "Menu":
                OnMenuLoaded.Invoke();
                break;

            default:
                break;
        }

        OnSceneChanged?.Invoke();
        coreReferences.loadingScreen.FadeToClear();
    }


    //IEnumerator Fade(Image panel, Color color, float startAlpha, float endAlpha, float duration)
    //{
    //    float elapsedTime = 0f;

    //    while (elapsedTime < duration)
    //    {
    //        // Lerp miêdzy startAlpha i endAlpha na podstawie czasu
    //        float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

    //        // Ustaw kolor panelu z now¹ wartoœci¹ alpha
    //        panel.color = new Color(color.r, color.g, color.b, alpha);

    //        // Zwiêksz czas trwania
    //        elapsedTime += Time.deltaTime;

    //        // Czekaj na nastêpn¹ klatkê
    //        yield return null;
    //    }

    //    // Upewnij siê, ¿e koñcowa wartoœæ alpha jest dok³adnie ustawiona
    //    panel.color = new Color(color.r, color.g, color.b, endAlpha);
    //}


    //public TypeDistinguisher currentLvl;
    //public GameObject loadscreenPrefab, loadscreenInstance;
    //public List<UnityEngine.Object> levels;
    //public Action loadAction;
    //public static event Action OnStorySceneLoaded;

    //public static bool IsMenu { get; internal set; } = true;

    //public static event Action OnGameLoaded;
    //public static event Action OnMenuLoaded;

    //private HashSet<string> toBeLoaded = new HashSet<string>();
    //private Action afterLoadingCallback;

    //public void Init()
    //{
    //    Debug.Log("SceneLoader Init called");
    //    Level.OnLevelCompleted += LoadNextScene;
    //}

    //public void LoadNextScene()
    //{
    //    Debug.Log("LoadNextScene called");
    //    GameSession.ballMovement.ReturnBallToPaddle();
    //    IsMenu = false;
    //    int currentLevelNumber = PlayerPrefs.GetInt(currentLvl.PrefsKey, 0);
    //    string currentSceneName = GetSceneName(currentLevelNumber);
    //    Debug.Log($"Current level number: {currentLevelNumber}");
    //    Debug.Log($"Current scene name: {currentSceneName}");
    //    var loadedScenes = SceneManager.GetAllScenes();

    //    // Log loaded scenes
    //    Debug.Log("Currently loaded scenes:" + string .Join(" , ", loadedScenes.Select(s => s.name)));
    //    foreach (var scene in loadedScenes)
    //    {
    //        Debug.Log($"Scene: {scene.name}, Loaded: {scene.isLoaded}");
    //    }

    //    bool isStoryLoaded = loadedScenes.Any(s => s.name == "Story");
    //    bool isCurrentLevelLoaded = loadedScenes.Any(s => s.name == currentSceneName);

    //    if (isStoryLoaded)
    //    {
    //        LoadNextLevel(currentLevelNumber);
    //    }
    //    else if (currentLevelNumber >= levels.Count)
    //    {
    //        Debug.Log("All levels completed, loading menu scene");
    //        LoadMenuScene();
    //    }
    //    else
    //    {
    //        Debug.Log("Loading Story scene");
    //        if (isCurrentLevelLoaded)
    //        {
    //            Debug.Log("Unloading scene: " + currentSceneName);
    //            SceneManager.UnloadSceneAsync(currentSceneName).completed += (AsyncOperation op) =>
    //            {
    //                SceneManager.LoadSceneAsync("Story", LoadSceneMode.Additive).completed += (AsyncOperation loadOp) =>
    //                {
    //                    GameSession.ballMovement.canBeLaunched = false;
    //                    OnStorySceneLoaded?.Invoke();
    //                    currentLevelNumber++;
    //                    PlayerPrefs.SetInt(currentLvl.PrefsKey, currentLevelNumber);
    //                    Debug.Log($"Next level number updated to: {currentLevelNumber}");
    //                };
    //            };
    //        }
    //        else
    //        {
    //            Debug.LogError($"Expected scene {currentSceneName} is not loaded.");
    //        }
    //    }
    //}

    //private void LoadNextLevel(int targetLevel)
    //{
    //    Debug.Log("Unloading Story and loading next level");
    //    SceneManager.UnloadSceneAsync("Story").completed += (AsyncOperation op) =>
    //    {
    //        GameSession.ballMovement.canBeLaunched = true;
    //        Debug.Log($"Loading next level: LVL{targetLevel:D2}");
    //        SceneManager.LoadSceneAsync($"LVL{targetLevel:D2}", LoadSceneMode.Additive).completed += (AsyncOperation loadOp) =>
    //        {
    //            PlayerPrefs.SetInt(currentLvl.PrefsKey, targetLevel);
    //            Debug.Log($"Next level number updated to: {targetLevel}");
    //        };
    //    };
    //}

    //private static string GetSceneName(int currentLevelNumber)
    //{
    //    return $"LVL{currentLevelNumber:D2}";
    //}


    //public void LoadStartScene()
    //{
    //    Debug.Log("LoadStartScene called");
    //    loadAction = LoadStartSceneCallback;
    //    UnloadMenu();
    //}

    //public void LoadStartSceneCallback()
    //{
    //    Debug.Log("LoadStartSceneCallback called");
    //    SceneManager.sceneLoaded += HandleSceneLoaded;
    //    toBeLoaded.Add("Story");
    //    afterLoadingCallback = () =>
    //    {
    //        IsMenu = false;
    //        Debug.Log("Story scene loaded, invoking OnGameLoaded");
    //        OnGameLoaded?.Invoke();
    //    };
    //    SceneManager.LoadSceneAsync("GamePlay", LoadSceneMode.Additive);
    //    SceneManager.LoadSceneAsync("Story", LoadSceneMode.Additive);
    //}

    //private void HandleSceneLoaded(Scene scene, LoadSceneMode loadingMode)
    //{
    //    Debug.Log($"Scene loaded: {scene.name}");
    //    if (toBeLoaded.Contains(scene.name))
    //    {
    //        toBeLoaded.Remove(scene.name);
    //    }
    //    if (toBeLoaded.Count == 0)
    //    {
    //        Debug.Log("All required scenes loaded");
    //        SceneManager.sceneLoaded -= HandleSceneLoaded;
    //        afterLoadingCallback.Invoke();
    //    }
    //}

    //public void LoadMenuScene()
    //{
    //    Debug.Log("LoadMenuScene called");
    //    OnMenuLoaded.Invoke();
    //    IsMenu = true;
    //    //SceneManager.LoadScene("Init");
    //    SceneManager.LoadScene("empty");
    //    SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
    //}

    //private void UnloadMenu()
    //{
    //    Debug.Log("UnloadMenu called");
    //    SceneManager.sceneUnloaded += HandleMenuUnloaded;
    //    if (SceneManager.GetSceneByName("Menu").isLoaded)
    //    {
    //        SceneManager.UnloadSceneAsync("Menu");
    //    }
    //}

    //private void HandleMenuUnloaded(Scene arg0)
    //{
    //    Debug.Log($"Menu scene unloaded: {arg0.name}");
    //    SceneManager.sceneUnloaded -= HandleMenuUnloaded;
    //    loadAction.Invoke();
    //}

    //public void LoadRecentLevel()
    //{
    //    Debug.Log("LoadRecentLevel called");
    //    loadAction = LoadRecentLevelCallback;
    //    UnloadMenu();
    //}

    //public void LoadRecentLevelCallback()
    //{
    //    Debug.Log("LoadRecentLevelCallback called");
    //    IsMenu = false;
    //    SceneManager.LoadScene("GamePlay", LoadSceneMode.Additive);
    //    SceneManager.LoadScene(GetSceneName(PlayerPrefs.GetInt(currentLvl.PrefsKey, 1)), LoadSceneMode.Additive);
    //    OnGameLoaded?.Invoke();
    //}

    //public void QuitGame()
    //{
    //    Debug.Log("QuitGame called");
    //    Application.Quit();
    //}
}



//TODO: ³adowanie zawartoœci gry po Storiesach w ramach max dwóch scen, nie po scenie na poziom.



//using UnityEngine;
//using System.Linq;
//using UnityEngine.SceneManagement;
//using System.Collections.Generic;
//using System;

//[CreateAssetMenu]
//public class SceneLoader : ScriptableObject
//{
//    public TypeDistinguisher currentLvl;
//    public GameObject loadscreenPrefab, loadscreenInstance;
//    public List<UnityEngine.Object> levels;
//    public Action loadAction;
//    public static event Action OnStorySceneLoaded;

//    public static bool IsMenu { get; internal set; } = true;

//    public static event Action OnGameLoaded;

//    private HashSet<string> toBeLoaded;
//    private Action afterLoadingCallback;

//    public void Init()
//    {
//        Debug.Log("SceneLoader Awake called");
//        Level.OnLevelCompleted += LoadNextScene;
//    }

//    public void LoadNextScene()
//    {
//        IsMenu = false;
//        int currentLevelNumber = PlayerPrefs.GetInt(currentLvl.PrefsKey, 1);
//        string currentSceneName = GetSceneName(currentLevelNumber);
//        var loadedScenes = SceneManager.GetAllScenes();

//        if (loadedScenes.Any(s => s.name == "Story"))
//        {
//            SceneManager.UnloadSceneAsync("Story");
//            SceneManager.LoadScene($"LVL{++currentLevelNumber:D2}", LoadSceneMode.Additive);
//            PlayerPrefs.SetInt(currentLvl.PrefsKey, currentLevelNumber);
//        }

//        else if (currentLevelNumber == loadedScenes.Length)
//        {
//            LoadMenuScene();
//        }

//        else
//        {
//            SceneManager.UnloadSceneAsync(currentSceneName);
//            SceneManager.LoadScene("Story", LoadSceneMode.Additive);
//            OnStorySceneLoaded.Invoke();
//        }
//    }

//    private static string GetSceneName(int currentLevelNumber)
//    {
//        return $"LVL{currentLevelNumber:D2}";
//    }


//    public void LoadStartScene()
//    {
//        loadAction = LoadRecentLevelCallback;
//        UnloadMenu();
//    }

//    public void LoadStartSceneCallback()
//    {
//        Debug.Log("Subscribed to HandleSceneLoaded");
//        SceneManager.sceneLoaded += HandleSceneLoaded;
//        toBeLoaded.Add("GamePlay");
//        toBeLoaded.Add("LVL01");
//        afterLoadingCallback = () => 
//        {
//            IsMenu = false;
//            Debug.Log(loadscreenInstance);
//            //Destroy(loadscreenInstance);
//            OnGameLoaded?.Invoke();
//        };
//        SceneManager.LoadSceneAsync("GamePlay", LoadSceneMode.Additive);
//        SceneManager.LoadSceneAsync("LVL01", LoadSceneMode.Additive);
//    }

//    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadingMode)
//    {
//        Debug.Log(scene.name);
//        if (toBeLoaded.Contains(scene.name))
//        {
//            toBeLoaded.Remove(scene.name);
//        }
//        if (toBeLoaded.Count == 0)
//        {
//            SceneManager.sceneLoaded -= HandleSceneLoaded;
//            afterLoadingCallback.Invoke();
//        }
//    }

//    public void LoadMenuScene()
//    {
//        IsMenu = true;
//        SceneManager.LoadScene("Menu");
//    }

//    private void UnloadMenu()
//    {
//        //loadscreenInstance = Instantiate(loadscreenPrefab);
//        //Debug.Log(loadscreenInstance);
//        //DontDestroyOnLoad(loadscreenInstance);
//        SceneManager.sceneUnloaded += HandleMenuUnloaded;
//        SceneManager.UnloadSceneAsync("Menu");
//    }

//    private void HandleMenuUnloaded(Scene arg0)
//    {
//        SceneManager.sceneUnloaded -= HandleMenuUnloaded;
//        Debug.Log(arg0.name);
//        loadAction.Invoke();
//    }

//    public void LoadRecentLevel()
//    {
//        loadAction = LoadRecentLevelCallback;
//        UnloadMenu();
//    }

//    public void LoadRecentLevelCallback()
//    {
//        IsMenu = false;
//        SceneManager.LoadScene("GamePlay", LoadSceneMode.Additive);
//        SceneManager.LoadScene(GetSceneName(PlayerPrefs.GetInt(currentLvl.PrefsKey, 1)), LoadSceneMode.Additive);
//        OnGameLoaded?.Invoke();
//    }

//    public void QuitGame()
//    {
//        Application.Quit();
//    }
//}
