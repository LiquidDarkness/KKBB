using UnityEngine;
using UnityEngine.UI;

// Puts every key back to what the game shipped with, and redraws the buttons so the screen says so.
public class KeyBindingReset : MonoBehaviour
{
    public Button button;

    private void OnEnable()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ResetAll);
    }

    private void ResetAll()
    {
        Controls.ResetToDefaults();
        KeyBindingButton.RefreshAll();
    }
}
