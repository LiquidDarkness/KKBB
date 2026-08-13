using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamInitializer : MonoBehaviour
{
    public int demoID, fullID;

    private void Start()
    {
        //skomentuj, żeby unablnąć aczki
        //PlayerPrefs.SetInt("storiesRead", storiesRead.);

        // Driven by the DEMO_BUILD scripting define symbol, the same switch that gates which
        // scenarios are playable - so the build can't end up reporting the wrong app to Steam.
#if DEMO_BUILD
        uint id = (uint)demoID;
#else
        uint id = (uint)fullID;
#endif

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
