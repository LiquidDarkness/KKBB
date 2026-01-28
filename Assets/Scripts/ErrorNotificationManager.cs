using System.Collections;
using TMPro;
using UnityEngine;

public class ErrorNotificationManager : MonoBehaviour
{
    public TextMeshProUGUI errorNotification;
    public GameObject errorBG;
    public AudioSource buzzer;
    public float displayTime = 3f;

    public void Awake()
    {
        ScoreManager.OnPurchaseAttempt += DisplayErrorNotification;
    }

    public void DisplayErrorNotification(bool possible)
    {
        buzzer.Play();
        errorBG.SetActive(!possible);
        string error = "Unable to proceed with the request. Insufficient resources.";
        errorNotification.text = error;
        StartCoroutine(HideErrorBGAfterDelay());
    }

    public void DisplayCustomError(string error)
    {
        buzzer.Play();
        errorBG.SetActive(true);
        errorNotification.text = error;
        if (!gameObject.active)
        {
            return;
        }
        StartCoroutine(HideErrorBGAfterDelay());
    }

    private IEnumerator HideErrorBGAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);

        errorBG.SetActive(false);
    }
}
