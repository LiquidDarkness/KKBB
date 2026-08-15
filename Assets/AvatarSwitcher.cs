using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct Avatar
    {
        public GameObject[] elements;
    }

    public List<Avatar> avatars;
    public TypeDistinguisher chosenAvatar;

    public void Start()
    {
        int chosenIndex = chosenAvatar.IntValue;

        // The chosen index is saved in PlayerPrefs and outlives any edit to this list, so a
        // stale one would throw here and take the whole level start down with it.
        if (chosenIndex < 0 || chosenIndex >= avatars.Count)
        {
            Debug.LogWarning($"[{nameof(AvatarSwitcher)}] Saved avatar {chosenIndex} is outside avatars (0-{avatars.Count - 1}). Falling back to the first one.", this);
            chosenIndex = 0;
            chosenAvatar.SetIntValue(chosenIndex);
        }

        if (avatars.Count == 0)
        {
            return;
        }

        foreach (Avatar avatar in avatars)
        {
            foreach (GameObject element in avatar.elements)
            {
                element.SetActive(false);
            }
        }
        foreach (GameObject element in avatars[chosenIndex].elements)
        {
            element.SetActive(true);
        }
    }
}
