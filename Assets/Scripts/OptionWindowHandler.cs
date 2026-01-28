using UnityEngine;

public class OptionWindowHandler : MonoBehaviour
{
    public GameObject optionWindow;

    public void OpenWindow()
    {
        optionWindow.SetActive(true);
    }   
    
    public void CloseWindow()
    {
        optionWindow.SetActive(false);
    }
}