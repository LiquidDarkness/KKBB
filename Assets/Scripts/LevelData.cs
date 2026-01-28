using UnityEngine;

[CreateAssetMenu]
public class LevelData : ScriptableObject
{
    public Sprite levelThumbnail;
    public Sprite background;

    public GameObject content;
    public AudioClip ambience;
}