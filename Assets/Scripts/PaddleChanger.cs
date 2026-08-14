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

        // Reuse the instance we already made for this prefab, but only if it is still alive:
        // the cache is static, so it outlives the scene while the instances in it do not, and a
        // stale entry would otherwise be handed out as a destroyed object after a scene reload.
        if (cachedPaddles.TryGetValue(paddleToUse, out paddleInstance) && paddleInstance != null)
        {
            // Nothing to do - the cached instance is reused as-is.
        }
        else
        {
            paddleInstance = Instantiate(paddleToUse);
            cachedPaddles[paddleToUse] = paddleInstance;
        }

        // Aktywujemy paddle i podnosimy event
        paddleInstance.gameObject.SetActive(true);
        OnPaddleChanged?.Invoke(paddleInstance);
    }
}
