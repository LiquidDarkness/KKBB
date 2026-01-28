using UnityEngine;

public class QuitButtonFunctionality : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game quit.");
    }
}
