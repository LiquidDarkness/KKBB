using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    [MenuItem("Debug/Log pause locks")]
    static void DebugLocks()
    {
        foreach (var item in locks)
        {
            Debug.Log(item);
        }
    }

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
}
