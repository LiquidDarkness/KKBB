using UnityEngine;
using System;

[Serializable]
public class StoryContainer : ScriptableObject
{
    public Story[] stories;

    [Tooltip("Playable in demo builds? Off by default, so a newly added scenario stays locked in the demo until it is deliberately opened up.")]
    public bool availableInDemo;
}
