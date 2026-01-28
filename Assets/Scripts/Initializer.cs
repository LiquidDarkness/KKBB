using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Initializer : MonoBehaviour
{
    public UnityEvent onInit;

    // Start is called before the first frame update
    void Start()
    {
        onInit.Invoke();    
    }

    
}
