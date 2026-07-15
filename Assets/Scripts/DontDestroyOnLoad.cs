using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    public void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }
}