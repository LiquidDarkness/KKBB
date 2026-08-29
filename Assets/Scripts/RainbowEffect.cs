using System.Collections;
using TMPro;
using UnityEngine;

public class RainbowEffect : MonoBehaviour
{
    // ¯eby dzia³a³o, w komponencie TextMeshProUGUI nale¿y zaznaczyæ 'Override tags'

    public TextMeshProUGUI textMeshComp;
    public float refreshSpeed;

    [Tooltip("Colour only the digits and leave the words alone. The score summary wants its numbers to dance without turning the labels into a fruit salad.")]
    public bool onlyDigits;
    private int count = 0;  // Zmienna œledz¹ca przesuniêcie kolorów

    // Started here rather than in Start, and stopped again when the component is switched off:
    // disabling a MonoBehaviour does not stop its coroutines, so turning the rainbow off in the
    // options used to leave it running until the scene was reloaded.
    private void OnEnable()
    {
        StartCoroutine(ColorRainbow());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator ColorRainbow()
    {
        while (true)
        {
            for (int i = 0; i < textMeshComp.textInfo.characterCount; ++i)
            {
                if (!textMeshComp.textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                if (onlyDigits && !char.IsDigit(textMeshComp.textInfo.characterInfo[i].character))
                {
                    continue;
                }

                // Oblicz wartoœæ hex koloru têczy dla ka¿dej litery
                string hexcolor = Rainbow(textMeshComp.textInfo.characterCount * 5, i + count);
                Color32 myColor32 = HexToColor(hexcolor);

                int meshIndex = textMeshComp.textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textMeshComp.textInfo.characterInfo[i].vertexIndex;
                Color32[] vertexColors = textMeshComp.textInfo.meshInfo[meshIndex].colors32;

                vertexColors[vertexIndex + 0] = myColor32;
                vertexColors[vertexIndex + 1] = myColor32;
                vertexColors[vertexIndex + 2] = myColor32;
                vertexColors[vertexIndex + 3] = myColor32;
            }

            count++;  // Aktualizuj count, aby zmieniaæ kolory z czasem
            textMeshComp.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            yield return new WaitForSecondsRealtime(refreshSpeed);
        }
    }

    public static string Rainbow(int numOfSteps, int step)
    {
        float r = 0.0f, g = 0.0f, b = 0.0f;
        float h = (float)step / numOfSteps;
        int i = (int)(h * 6);
        float f = h * 6.0f - i;
        float q = 1 - f;

        switch (i % 6)
        {
            case 0: r = 1; g = f; b = 0; break;
            case 1: r = q; g = 1; b = 0; break;
            case 2: r = 0; g = 1; b = f; break;
            case 3: r = 0; g = q; b = 1; break;
            case 4: r = f; g = 0; b = 1; break;
            case 5: r = 1; g = 0; b = q; break;
        }

        return "#" + ((int)(r * 255)).ToString("X2") + ((int)(g * 255)).ToString("X2") + ((int)(b * 255)).ToString("X2");
    }

    // Funkcja konwertuj¹ca kod hex na Color32
    private Color32 HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white;  // Domyœlny kolor, jeœli konwersja siê nie uda
    }
}
