using System.Collections;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

[CreateAssetMenu]
public class PresetApplicator : ScriptableObject
{
    public ScriptableObject target;
    public Preset presetDemo;
    public Preset presetFull;

    public void SetDemo()
    {
        presetDemo.ApplyTo(target);
    }

    public void SetFull()
    {
        presetFull.ApplyTo(target);
    }
}