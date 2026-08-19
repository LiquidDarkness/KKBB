using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct Avatar
    {
        public GameObject[] elements;

        [Tooltip("How fast this cat flies. Left at 0 the cat uses BallMovement.defaultSpeed - only fill it in for a cat meant to feel different from the rest.")]
        public float ballSpeed;

        [Tooltip("The paws this cat holds a bomb with. Left empty she borrows the ones the bomb display was drawn with.")]
        public Sprite paws;
    }

    public List<Avatar> avatars;
    public TypeDistinguisher chosenAvatar;

    [Tooltip("The paw renderers on the bomb display. There is one pair, shared by every cat, so the chosen cat's paws are put onto it when she is switched in.")]
    public SpriteRenderer[] pawRenderers;

    // What the pair was drawn with, kept so a cat with no paws of her own falls back to it instead
    // of inheriting whichever cat held the bomb last.
    private Sprite drawnPaws;

    private void Awake()
    {
        foreach (SpriteRenderer paw in pawRenderers)
        {
            if (paw != null)
            {
                drawnPaws = paw.sprite;
                break;
            }
        }
    }

    // Called from Start, once the cat has been settled on. The renderers may well be switched off
    // at the time - the bomb display only shows while a bomb is being carried - and assigning a
    // sprite to an inactive renderer is fine.
    private void ApplyPaws(Avatar avatar)
    {
        Sprite wanted = avatar.paws != null ? avatar.paws : drawnPaws;

        if (wanted == null)
        {
            return;
        }

        foreach (SpriteRenderer paw in pawRenderers)
        {
            if (paw != null)
            {
                paw.sprite = wanted;
            }
        }
    }

    // The speed asked for by the cat currently in play, or 0 for "no opinion, use the default".
    // Resolved on every read rather than cached: the avatar is chosen from the menu, and this
    // component is not reloaded in between.
    public float CurrentBallSpeed
    {
        get
        {
            int index = chosenAvatar != null ? chosenAvatar.IntValue : 0;

            if (index < 0 || index >= avatars.Count)
            {
                return 0f;
            }

            return avatars[index].ballSpeed;
        }
    }

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

        ApplyPaws(avatars[chosenIndex]);
    }
}
