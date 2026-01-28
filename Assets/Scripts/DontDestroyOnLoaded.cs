using System;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyOnLoaded : MonoBehaviour
{
    public static event Action<MonoBehaviour> OnPromoted;

    // jedna lista per typ (uniemożliwia duplikaty dla tego samego targetu)
    private static Dictionary<Type, List<MonoBehaviour>> promotions = new();

    [Tooltip("Obiekt (komponent) który ma być promowany do DontDestroyOnLoad. Jeśli null, użyje tego komponentu.")]
    public MonoBehaviour target;

    private void Awake()
    {
        if (target == null) target = this;

        var go = target.gameObject;
        go.transform.SetParent(null);
        DontDestroyOnLoad(go);

        Type key = target.GetType();

        if (!promotions.ContainsKey(key))
            promotions[key] = new List<MonoBehaviour>();

        if (promotions[key].Contains(target))
        {
            Debug.LogWarning($"[DontDestroyOnLoaded] {key.Name} instance already exists. Skipping duplicate.");
            Destroy(go); // niszczymy duplikat GameObject
            return;
        }

        promotions[key].Add(target);
        OnPromoted?.Invoke(target);
        //Debug.Log($"[DontDestroyOnLoaded] Promoted {target.name} ({key.Name})");
    }

    private void OnDestroy()
    {
        Unregister(target);
    }

    public static void Unregister(MonoBehaviour instance)
    {
        if (instance == null) return;

        Type key = instance.GetType();
        if (!promotions.TryGetValue(key, out var list)) return;

        list.RemoveAll(x => x == null);

        if (list.Remove(instance) && list.Count == 0)
        {
            promotions.Remove(key);
            //Debug.Log($"[DontDestroyOnLoaded] Removed last instance of {key.Name}");
        }
    }

    public static T GetFirst<T>() where T : MonoBehaviour
    {
        if (TryGetFirst(out T first)) return first;
        return null;
    }

    public static bool TryGetFirst<T>(out T first) where T : MonoBehaviour
    {
        first = null;
        Type key = typeof(T);
        if (!promotions.TryGetValue(key, out var list) || list.Count == 0) return false;

        list.RemoveAll(x => x == null);
        if (list.Count == 0) { promotions.Remove(key); return false; }

        first = list[0] as T;
        return first != null;
    }

    public static List<T> GetAll<T>() where T : MonoBehaviour
    {
        Type key = typeof(T);
        if (!promotions.TryGetValue(key, out var list)) return new List<T>();

        list.RemoveAll(x => x == null);

        var result = new List<T>(list.Count);
        foreach (var item in list)
        {
            if (item is T t) result.Add(t);
        }
        return result;
    }
}
