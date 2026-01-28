using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteChanger : MonoBehaviour
{
    public List<Image> slots;
    public List<Sprite> sprites;
    public float refreshInterval;

    public void Start()
    {
        StartCoroutine(ChangeSprites());
    }

    public int RandomIndex()
    {
        int result = Random.RandomRange(0, sprites.Count);
        return result;
    }

    public IEnumerator ChangeSprites()
    {
        while (true)
        {
            foreach (Image image in slots)
            {
                image.sprite = sprites[RandomIndex()];
                yield return new WaitForSeconds(refreshInterval);
            }
        }
    }
}
