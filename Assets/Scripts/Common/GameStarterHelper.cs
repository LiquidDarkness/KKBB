using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarterHelper : MonoBehaviour
{
    public static bool firstLaunch = true;
    [Header ("Works in editor ONLY!")]
    public bool forceInitialScene;

    void Start()
    {
#if !UNITY_EDITOR

    return;

#endif

        if (!forceInitialScene)
        {
            return;
        }
        if (!firstLaunch)
        {
            return;
        }
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            return;
        }
        SceneManager.LoadScene(0, LoadSceneMode.Single);
        firstLaunch = false;
    }
}
