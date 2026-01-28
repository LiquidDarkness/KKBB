using UnityEngine;
using UnityEngine.UI;

public class TVStaticFX : MonoBehaviour
{
    // In the Raw Image component set 'Material' to be 'Sprite-Default'
    // 'Texture' to a sprite with a 2D representation of the TV static effect.
    // The UV Rect 'W' and 'H' impact the level of detail - the bigger, the more detailed the effect.
    public RawImage target;
    public int size, width, height;
    // The sizes have to be bigger that 150  at least but less the resolution, otherwise it'll lag.
    Texture2D texture;

    public void Start()
    {
        size = width * height;
        texture = new(width, height);
        target.texture = texture;
    }

    void Update()
    {
        Color32[] colors = new Color32[size];
        for (int i = 0; i < size; i++)
        {
            byte random = (byte)Random.Range(0, byte.MaxValue + 1);
            colors[i] = new Color32(random, random, random, 255);
        }
        texture.SetPixels32(colors);
        texture.Apply();
    }
}
