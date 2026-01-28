using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image loadingImage;
    [SerializeField] private float fadeoutSpeed;

    private bool isBusy;

    public bool IsBusy { get => isBusy; }

    public void FadeToClear(Action callback = null)
    {
        StartCoroutine(FadeoutSquare(Color.black, Color.clear, callback));
    }

    public void FadeToBlack(Action callback = null)
    {
        //Debug.Log("Fading to black. " + gameObject.activeInHierarchy);
        StartCoroutine(FadeoutSquare(Color.clear, Color.black, callback));
    }

    private IEnumerator FadeoutSquare(Color fromFade, Color toFade, Action callback)
    {
        isBusy = true;

        float fadeAmount = 0;
        loadingImage.color = fromFade;

        while (fadeAmount <= 1)
        {
            yield return null;
            fadeAmount += (fadeoutSpeed * Time.unscaledDeltaTime);

            loadingImage.color = Color.Lerp(fromFade, toFade, fadeAmount);
        }
        loadingImage.color = toFade;

        isBusy = false;

        callback?.Invoke();
    }

    [ContextMenu("TestFade")]
    public void TestFade()
    {
        FadeToBlack(
            () => {
                Debug.Log("FadeOut done.");
                FadeToClear(
                    () =>
                    {
                        Debug.Log("FadeIn done.");
                    }
                    );
            }
            );
    }
}