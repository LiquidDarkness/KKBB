using UnityEngine;
using UnityEngine.UI;

public class RainbowAnimation : MonoBehaviour
{
    public float speed = 1f;
    public Image purchaseBg;

    private float hue;

    void Update()
    {
        hue += Time.deltaTime * speed;
        if (hue > 1f) hue -= 1f;

        Color color = Color.HSVToRGB(hue, 1f, 1f);
        purchaseBg.color = color;
    }
}