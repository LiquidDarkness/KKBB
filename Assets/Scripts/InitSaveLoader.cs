using UnityEngine;

public class InitSaveLoader : MonoBehaviour
{
    // Awake, not Start. VolumeSetter sits on this same prefab and reads the saved level in its
    // own Start, and Unity gives no order between two Starts - so half the time the mixer was set
    // from whatever PlayerPrefs held before the save file was applied. That is the burst of music
    // at the default level followed by a drop to the level the player had actually chosen. Every
    // Awake runs before every Start, so this now always wins.
    private void Awake()
    {
        SaveManager.Load();
    }
}
