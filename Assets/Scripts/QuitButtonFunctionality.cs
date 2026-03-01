using UnityEngine;

public class QuitButtonFunctionality : MonoBehaviour
{
    public ScriptableBool playtestFlag;

    public void QuitGame()
    {
        if (playtestFlag.value)
        {
            Application.OpenURL("https://forms.gle/gy2EanqTfcZr3KC67");
        }
        Application.Quit();
        Debug.Log("Game quit.");
    }
}
