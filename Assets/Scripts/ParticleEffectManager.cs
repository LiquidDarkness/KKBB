using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectManager : MonoBehaviour
{
    public Block block;
    public DiffcultyManager diffcultyManager;

    public void Awake()
    {
        SceneLoader.OnGameplayLoaded += FillReference;
    }

    public void OnDestroy()
    {
        SceneLoader.OnGameplayLoaded -= FillReference;
    }

    public void FillReference()
    {
        block.blockSparklesVFX = diffcultyManager.CurrentSettings.particleEffect;
    }
}
