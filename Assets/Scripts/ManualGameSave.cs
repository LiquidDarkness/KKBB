using UnityEngine;

public class ManualGameSave : MonoBehaviour
{
    public void SaveGame()
    {
        SaveManager.Save();
    }

    public void LoadGame()
    {
        SaveManager.Load();
    }
}