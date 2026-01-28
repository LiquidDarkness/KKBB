using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectManager : MonoBehaviour
{
    public Block block;
    public DiffcultyManager diffcultyManager;

    public void Awake()
    {
        FillReference();
        SceneLoader.OnGameplayLoaded += FillReference;
    }

    public void FillReference()
    {
        block = gameObject.GetComponent<Block>();
        block.blockSparklesVFX = diffcultyManager.CurrentSettings.particleEffect;
    }
}
