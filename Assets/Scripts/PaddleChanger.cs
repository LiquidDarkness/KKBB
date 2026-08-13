using System;
using System.Collections.Generic;
using UnityEngine;

public class PaddleChanger : MonoBehaviour, IDropReceiver
{
    public PaddleMovement replacingPaddle; // prefab do zmiany
    private static Dictionary<PaddleMovement, PaddleMovement> cachedPaddles = new();
    public static event Action<PaddleMovement> OnPaddleChanged;

    public void DigestDrop(DropReceiver _)
    {
        ChangePaddle(replacingPaddle);
    }

    public void ChangePaddle()
    {
        ChangePaddle(replacingPaddle);
    }

    public void ChangePaddle(PaddleMovement paddleToUse)
    {
        if (paddleToUse == null)
        {
            Debug.LogWarning("New paddle is null.");
            return;
        }

        PaddleMovement paddleInstance;

        // Jeœli mamy ju¿ instancjê w cache, u¿ywamy jej
        if (cachedPaddles.ContainsKey(paddleToUse))
        {
            paddleInstance = cachedPaddles[paddleToUse];
        }
        else
        {
            // Inaczej instancjonujemy nowy obiekt i dodajemy do cache
            paddleInstance = Instantiate(paddleToUse);
            cachedPaddles.Add(paddleToUse, paddleInstance);
        }

        // Aktywujemy paddle i podnosimy event
        paddleInstance.gameObject.SetActive(true);
        OnPaddleChanged?.Invoke(paddleInstance);
    }
}
