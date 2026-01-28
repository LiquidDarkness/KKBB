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

    /*
    public List<GameObject> objectsInHierarchy;
    private int currentIndex = 0;

    void Update()
    {
        // SprawdŸ, czy obecnie aktywny obiekt zosta³ dezaktywowany
        if (!objectsInHierarchy[currentIndex].activeSelf)
        {
            // Dezaktywuj aktualny obiekt
            DeactivateObject(currentIndex);

            // Prze³¹cz siê na nastêpny obiekt w hierarchii
            currentIndex++;

            // SprawdŸ, czy jest wiêcej obiektów w hierarchii
            if (currentIndex >= objectsInHierarchy.Count)
            {
                currentIndex = 0; // Jeœli nie, prze³¹cz siê na pierwszy obiekt w hierarchii
            }

            // Aktywuj nastêpny obiekt w hierarchii
            ActivateObject(currentIndex);
        }
    }

    void DeactivateAllObjects()
    {
        foreach (GameObject obj in objectsInHierarchy)
        {
            DeactivateObject(obj);
        }
    }

    void ActivateObject(int index)
    {
        // Aktywuj obiekt o podanym indeksie
        ActivateObject(objectsInHierarchy[index]);
    }

    void ActivateObject(GameObject obj)
    {
        obj.SetActive(true);
    }

    void DeactivateObject(int index)
    {
        // Dezaktywuj obiekt o podanym indeksie
        DeactivateObject(objectsInHierarchy[index]);
    }

    void DeactivateObject(GameObject obj)
    {
        obj.SetActive(false);
    }
    */
}

