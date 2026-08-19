using UnityEngine;
using UnityEngine.SceneManagement;

// Hides something while a given scene is the one being played - the main menu button in the
// options window has nothing to do while the player is already in the menu.
//
// Checked in OnEnable rather than once at startup: the options window lives on GameSession and
// survives every scene load, so which scene it is open over changes over the run.
public class HideInScene : MonoBehaviour
{
    public GameObject target;

    [Tooltip("Scene the target is hidden in.")]
    public string sceneName = "Menu";

    private void OnEnable()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(HideInScene)}: nothing assigned to hide.", this);
            return;
        }

        target.SetActive(SceneManager.GetActiveScene().name != sceneName);
    }
}
