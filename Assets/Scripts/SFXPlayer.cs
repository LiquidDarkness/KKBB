using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioClip[] breakSounds;
    public AudioClip[] collisionSounds;
    public AudioSource sfxSource;
    private int collisionSound;
    private int breakSound;

    public void Awake()
    {
        Block.OnBlockHit += PlayCollisionClip;
        Block.OnBlockBroken += HandleBlockBroken;
    }

    private void HandleBlockBroken(Vector3 _)
    {
        PlayBreakingClip();
    }

    public void Start()
    {
        collisionSound = collisionSounds.Length;
        breakSound = breakSounds.Length;
    }

    public void PlayCollisionClip()
    {
        if (collisionSounds.Length > 0)
        {
            sfxSource.clip = collisionSounds[Random.Range(0, collisionSounds.Length)];
            sfxSource.Play();
        }
    }    
    
    public void PlayBreakingClip()
    {
        if (breakSounds.Length > 0)
        {
            sfxSource.clip = breakSounds[Random.Range(0, breakSounds.Length)];
            sfxSource.Play();
        }
    }

}
