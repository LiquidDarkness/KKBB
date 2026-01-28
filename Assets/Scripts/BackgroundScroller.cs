using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    [SerializeField] private float x, y;

    public void Update()
    {
        rawImage.uvRect = new(rawImage.uvRect.position + new Vector2(x, y) * Time.unscaledDeltaTime, rawImage.uvRect.size);
    }
}
