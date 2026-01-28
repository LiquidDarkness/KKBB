using UnityEngine;

public class InitSaveLoader : MonoBehaviour
{
    private void Start()
    {
        SaveManager.Load();
    }
}
