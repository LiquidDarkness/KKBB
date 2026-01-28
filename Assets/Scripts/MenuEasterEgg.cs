using UnityEngine;
using UnityEngine.UI;

public class MenuEasterEgg : MonoBehaviour
{
    public GameSession gameSession;
    public Button easterEgg;

    public void PlayTheEasterEgg()
    {
        easterEgg.interactable = false;
    }

}
