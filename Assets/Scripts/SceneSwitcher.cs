using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneName;

    public void Switch()
    {
        SceneManager.LoadScene(sceneName);
    }
}
