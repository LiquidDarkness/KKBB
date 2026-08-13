using System.Collections.Generic;
using UnityEngine;

public class ObjectActivationManager : MonoBehaviour
{
    public GameObject selectedObject; // Referencja do wybranego obiektu
    public List<GameObject> objectsToActivate;

    void Start()
    {
        DeactivateAllObjects();
        selectedObject.SetActive(true);
    }

    internal void ResetObject()
    {
        selectedObject = objectsToActivate[0];
        selectedObject.SetActive(true);
    }

    void DeactivateAllObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            obj.SetActive(false); // Dezaktywuj obiekt
        }
    }

    public void ChangeSelectedObject(GameObject objectToSelect)
    {
        DeactivateAllObjects();
        selectedObject = objectToSelect;
    }

}

