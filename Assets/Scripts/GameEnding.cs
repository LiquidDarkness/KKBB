using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEnding : MonoBehaviour
{
    public CoreReferences coreReferences;

    void Start()
    {
        coreReferences.storyManager.ShowEndingButton(true);
    }

    public void LoadMenu()
    {
        coreReferences.gameSession.gameObject.GetComponent<SceneLoader>().LoadScene("Menu");
    }
}
