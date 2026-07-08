using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShopPurchaseHandler : MonoBehaviour
{
    [Header("References")]
    public GameObject purchaseWindow;
    public CanvasGroup canvasGroup;
    public Image spriteContainer;
    public Sprite boughtItem;

    [Header("Timing")]
    public float visibilityTime = 0.5f;   // czas pełnej widoczności
    public float fadeDuration = 0.3f;     // czas fade in/out

    private Coroutine currentRoutine;

    public void ShowPurchase()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        spriteContainer.sprite = boughtItem;

        // ustawiamy minimalną alpha od razu, żeby było widać UI
        canvasGroup.alpha = 0.05f;

        // Fade In
        yield return StartCoroutine(Fade(canvasGroup.alpha, 1f, fadeDuration));

        // Hold
        //yield return new WaitForSeconds(visibilityTime);

        // Fade Out
        yield return StartCoroutine(Fade(canvasGroup.alpha, 0f, fadeDuration));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.timeScale;

            float t = Mathf.Clamp01(elapsedTime / duration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}