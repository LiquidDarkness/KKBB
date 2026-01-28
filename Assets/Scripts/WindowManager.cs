using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public GameSpeedManager speedManager;
    [System.Serializable]
    public class WindowToggle
    {
        public string windowName; // Nazwa okna, np. "Shop"
        public GameObject window; // Odwo³anie do okienka
        public string toggleKey; // Nazwa klawisza w Input Manager, np. "Tab"
    }

    public List<WindowToggle> windows = new List<WindowToggle>();

    private Dictionary<string, GameObject> windowDict = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Inicjalizacja s³ownika dla szybszego dostêpu do okienek po nazwie
        foreach (var windowToggle in windows)
        {
            windowDict[windowToggle.windowName] = windowToggle.window;
            if (windowToggle.window != null)
                windowToggle.window.SetActive(false); // Ustawienie okienek jako nieaktywne na starcie
        }
    }

    private void Update()
    {
        // Sprawdzenie, czy któryœ z klawiszy przypisanych do okienek zosta³ wciœniêty
        foreach (var windowToggle in windows)
        {
            if (Input.GetButtonDown(windowToggle.toggleKey))
            {
                ToggleWindow(windowToggle.windowName);
            }
        }
    }

    public void ToggleWindow(string windowName)
    {
        if (windowDict.TryGetValue(windowName, out GameObject window))
        {
            bool isActive = window.activeSelf;
            window.SetActive(!isActive); // Prze³¹czamy widocznoœæ okienka
            
            //TODO: nie odpauzowuje siê
            if (isActive)
            {
                speedManager.Unpause();
            }
            else
            {
                speedManager.Pause();
            }
        }
        else
        {
            Debug.LogWarning("Window not found: " + windowName);
        }
    }
}
