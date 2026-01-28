using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu]
public class DifficultySettings : ScriptableObject
{
    public float baseGameSpeed;
    public float maxSpeed;
    public float chainDestructionProbability;
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
