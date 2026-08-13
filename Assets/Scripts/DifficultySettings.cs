using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu]
public class DifficultySettings : ScriptableObject
{
    public int livesNumber;
    public float baseGameSpeed;
    public float maxSpeed;
    // Block Animator playback multiplier. Independent of baseGameSpeed/Time.timeScale, so
    // decorative block animations can be tuned without touching ball/paddle pacing.
    public float blockAnimationSpeed = 1f;
    public float chainDestructionProbability;
    // Damage multiplier applied to neighbors during chain block destruction (ChainDestroyer).
    // 0.15 is the highest sensible value.
    public float chainDamageMultiplier = 0.15f;
    public GameObject particleEffect;
    public List<DropSetting> drops;

    //public static event Action OnSettingStatusChanged;

    [Serializable]
    public struct DropSetting
    {
        public GameObject prefab;
        public int weight;
    }
}
