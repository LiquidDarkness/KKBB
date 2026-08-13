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
