using System;
using System.Collections.Generic;
using UnityEngine;

public class PaddleManager : MonoBehaviour
{
    [SerializeField] private List<PaddleMovement> paddlePrefabs;

    private PaddleMovement currentPaddle;
    private readonly Dictionary<Type, PaddleMovement> cache = new();

    public static event Action<PaddleMovement> OnPaddleChanged;

    private void Start()
    {
        // Spawn pierwszego paddla (np. pierwszego z listy)
        if (paddlePrefabs.Count > 0)
        {
            currentPaddle = Instantiate(paddlePrefabs[0], Vector3.zero, Quaternion.identity);
            cache[currentPaddle.GetType()] = currentPaddle;
            OnPaddleChanged?.Invoke(currentPaddle);
        }
    }

    public void SwitchTo<T>() where T : PaddleMovement
    {
        if (currentPaddle != null)
            currentPaddle.gameObject.SetActive(false);

        PaddleMovement newPaddle;

        if (cache.TryGetValue(typeof(T), out newPaddle))
        {
            newPaddle.transform.position = currentPaddle != null
                ? currentPaddle.transform.position
                : Vector3.zero;
            newPaddle.gameObject.SetActive(true);
        }
        else
        {
            var prefab = paddlePrefabs.Find(p => p is T);
            Vector3 pos = currentPaddle != null ? currentPaddle.transform.position : Vector3.zero;
            newPaddle = Instantiate(prefab, pos, Quaternion.identity);
            cache[typeof(T)] = newPaddle;
        }

        currentPaddle = newPaddle;
        OnPaddleChanged?.Invoke(newPaddle);
    }

    public PaddleMovement GetCurrentPaddle() => currentPaddle;
}
