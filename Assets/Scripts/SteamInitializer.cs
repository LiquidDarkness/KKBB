using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamInitializer : MonoBehaviour
{
    public ScriptableBool demoFlag;
    public ScriptableBool playtestFlag;
    public int demoID, fullID, playtestID; //1878110

    private void Start()
    {
        //skomentuj, żeby unablnąć aczki
        //PlayerPrefs.SetInt("storiesRead", storiesRead.);
        uint id;
        if (demoFlag.value)
        {
            id = (uint)demoID;
        }
        else if (playtestFlag.value)
        {
            id = (uint)playtestID;
        }
        else
        {
            id = (uint)fullID;
        }

        try
        {
            Steamworks.SteamClient.Init(id);
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
        Debug.Log("Init's working");

        //SceneManager.LoadScene(1);
    }
}
