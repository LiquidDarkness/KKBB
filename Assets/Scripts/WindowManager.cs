using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    const string PAUSE_LOCK = nameof(WindowManager);

    [System.Serializable]
    public class WindowToggle
    {
        public string WindowName => window.name; // Nazwa okna, np. "Shop"
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
            windowDict[windowToggle.WindowName] = windowToggle.window;
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
                ToggleWindow(windowToggle.WindowName);
            }
        }
    }

    public void ToggleWindow(GameObject window)
    {
        ToggleWindow(window.name);
    }

    public void ToggleWindow(string windowName)
    {
        if (windowDict.TryGetValue(windowName, out GameObject window))
        {
            bool isActive = !window.activeSelf; // Prze³¹czamy widocznoœæ okienka
            Debug.Log("Toggled window: " + window.name);
            window.SetActive(isActive); 
            
            if (isActive)
            {
                PauseManager.Pause(windowName);
            }
            else
            {
                PauseManager.Unpause(windowName);
            }
        }
        else
        {
            Debug.LogWarning("Window not found: " + windowName);
        }
    }
}
