using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InitIniator : MonoBehaviour
{
    public UnityEvent init; 

    // Start is called before the first frame update
    void Start()
    {
        init.Invoke();
    }
}
