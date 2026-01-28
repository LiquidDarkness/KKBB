using UnityEngine;
using UnityEngine.UI;

public class ResumeAfterDeath : MonoBehaviour
{
    public Button resumeButton;
    public Button buyingButton;

    public void Update()
    {
        if (PlayerHealth.Health != 0)
        {
            resumeButton.gameObject.SetActive(true);
        }

        resumeButton.gameObject.SetActive(false);
    }
}
