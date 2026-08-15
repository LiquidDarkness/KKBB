using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
// Guarded: the UnityEditor namespace does not exist in a player build, and an unconditional
// using here fails the build outright even though the only thing needing it is the menu item
// at the bottom of this file.
using UnityEditor;
#endif

public static class PauseManager
{
    static HashSet<string> locks = new();

    public static bool IsPaused => locks.Count > 0;

    public static event Action OnPause, OnUnpause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        locks.Clear();
    }

#if UNITY_EDITOR
    [MenuItem("Debug/Log pause locks")]
    static void DebugLocks()
    {
        foreach (var item in locks)
        {
            Debug.Log(item);
        }
    }
#endif

    public static void Pause(string locker)
    {
        locks.Add(locker);
        if (locks.Count == 1)
        {
            OnPause?.Invoke();
        }
    }

    public static void Unpause(string locker)
    {
        locks.Remove(locker);
        if (locks.Count == 0)
        {
            OnUnpause?.Invoke();
        }
    }

    // A pause belongs to whatever took it - an open window, the player dying - and none of those
    // outlive the scene they happened in. Nothing releases the lock PlayerHealth.OnDeath takes,
    // so without this a death left the lock standing for the rest of the run and the next
    // gameplay session started frozen solid.
    public static void ReleaseAll()
    {
        if (locks.Count == 0)
        {
            return;
        }

        locks.Clear();
        OnUnpause?.Invoke();
    }
}
