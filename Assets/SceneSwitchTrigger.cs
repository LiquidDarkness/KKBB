using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SceneSwitchTrigger : MonoBehaviour
{
    public UnityEvent OnSceneSwitched;
    public string expectedScene;

    void Awake()
    {
        SceneLoader.OnSceneChanged += HandleSceneChanged;
    }

    private void OnDestroy()
    {
        SceneLoader.OnSceneChanged -= HandleSceneChanged;
    }

    private void HandleSceneChanged(string loadedScene)
    {
        if (string.Equals(expectedScene, loadedScene))
        {
            OnSceneSwitched.Invoke();
        }
    }
}
