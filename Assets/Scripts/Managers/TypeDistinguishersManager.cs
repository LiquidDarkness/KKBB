using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TypeDistinguishersManager : MonoBehaviour
{
    public List<TypeDistinguisher> typeDistinguishers;
    private static TypeDistinguishersManager instance;

    public static TypeDistinguishersManager Instance => instance;

    private void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        if (typeDistinguishers.Count == 0)
        {
            typeDistinguishers = Resources.LoadAll<TypeDistinguisher>("TypeDistinguishers").ToList();
        }

        foreach (TypeDistinguisher item in typeDistinguishers)
        {
            if (!item.purgable)
                PersistentSettings.PreservePlayerPref(item);
        }

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }

        if (!SaveManager.HasLoaded)
        {
            SaveManager.Load();
        }
    }
}
