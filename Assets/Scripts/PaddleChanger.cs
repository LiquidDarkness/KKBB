using System;
using System.Collections.Generic;
using UnityEngine;

public class PaddleChanger : MonoBehaviour, IDropReceiver
{
    public PaddleMovement replacingPaddle; // prefab do zmiany
    private static Dictionary<PaddleMovement, PaddleMovement> cachedPaddles = new();
    public static event Action<PaddleMovement> OnPaddleChanged;

    public void DigestDrop()
    {
        ChangePaddle();
    }

    public void ChangePaddle()
    {
        if (replacingPaddle == null)
        {
            Debug.LogWarning("New paddle is null.");
            return;
        }

        PaddleMovement paddleInstance;

        // Jeœli mamy ju¿ instancjê w cache, u¿ywamy jej
        if (cachedPaddles.ContainsKey(replacingPaddle))
        {
            paddleInstance = cachedPaddles[replacingPaddle];
        }
        else
        {
            // Inaczej instancjonujemy nowy obiekt i dodajemy do cache
            paddleInstance = Instantiate(replacingPaddle);
            cachedPaddles.Add(replacingPaddle, paddleInstance);
        }

        // Aktywujemy paddle i podnosimy event
        paddleInstance.gameObject.SetActive(true);
        OnPaddleChanged?.Invoke(paddleInstance);
    }
}
