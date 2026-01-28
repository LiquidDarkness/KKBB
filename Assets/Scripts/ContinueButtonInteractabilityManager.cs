using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ContinueButtonInteractabilityManager : MonoBehaviour
{
    public Button loadButton;

    public void OnEnable()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.dat");

        if (!File.Exists(path))
        {
            loadButton.interactable = false;
        }
        else
        {
            loadButton.interactable = true;
        }
    }
}