using System;
using UnityEngine;

public class SignalEmmiter : MonoBehaviour
{
    public static event Action<string> OnSignalled;
    public string signal;
        
    public void Start()
    {
        OnSignalled?.Invoke(signal);
    }
}