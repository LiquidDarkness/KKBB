using UnityEngine;

public class ConfirmationWindow : MonoBehaviour
{
    public GameObject confirmationWindow;

    public void OpenConfirmationWindow()
    {
        confirmationWindow.SetActive(true);
    }
}
