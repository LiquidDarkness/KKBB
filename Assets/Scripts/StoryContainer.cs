using UnityEngine;
using System;

[Serializable]
public class StoryContainer : ScriptableObject
{
    public Story[] stories;

    [Tooltip("Playable in demo builds? Off by default, so a newly added scenario stays locked in the demo until it is deliberately opened up.")]
    public bool availableInDemo;

    // Everything below is asked of the scenario rather than read out of the array above, so that a
    // scenario which does not keep its beats in a list - the endless one works them out as it goes -
    // can still be started, progressed and finished by exactly the same code. A written scenario
    // answers all of it out of stories, and nothing changes for it.

    public virtual int BeatCount => stories != null ? stories.Length : 0;

    public virtual Story BeatAt(int beat)
    {
        return stories != null && beat >= 0 && beat < stories.Length ? stories[beat] : null;
    }

    // Whether the last beat is the farewell: the one paired with a placeholder level that is never
    // played, and reaching which is what ends the scenario.
    public virtual bool HasFarewellBeat => true;

    // Whether the beats have text of their own in the translation files, keyed by scenario name and
    // beat number. A scenario that says no writes its own card instead.
    public virtual bool HasBeatText => true;
}
