using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ContinueButtonDisplayer : MonoBehaviour
{
    public Button continueButton;
    private string SaveFileName = "save.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    void Start()
    {
        if (File.Exists(SaveFilePath) && continueButton.interactable == true)
        {
            return;
        }
        else
        {
            continueButton.interactable = false;
        }
    }
}
