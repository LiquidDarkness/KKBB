using UnityEngine;

// Keeps something on screen quietly alive: a slow rise and fall with a little tilt to it, the pace
// of breathing rather than of an animation. Made for the menu art, where a picture that never moves
// at all reads as a screenshot of a menu.
//
// It moves the object rather than any mesh of its own, so it belongs on something nothing else is
// positioning - decoration, not anything inside a layout group. Whatever it was given is handed back
// the moment it is switched off, so the options can turn it off mid-sway without leaving the art
// crooked.
public class IdleSway : MonoBehaviour
{
    [Tooltip("How far it rises and falls, in the units its parent is laid out in.")]
    public float rise = 14f;

    [Tooltip("How far it tilts either way, in degrees. It turns about its own pivot, so where the pivot is decides whether this reads as a bob or as a swing.")]
    public float tilt = 1.6f;

    [Tooltip("Seconds for one whole breath, out and back.")]
    public float period = 5f;

    [Tooltip("Where in that breath it starts, in turns. Give two swaying things different values and they stop moving as one.")]
    public float offset;

    private Vector3 restingPosition;
    private Quaternion restingRotation;
    private bool remembered;

    private void OnEnable()
    {
        Remember();
    }

    private void OnDisable()
    {
        Settle();
    }

    // Read once, from wherever the object was placed. Everything after is measured from it, so the
    // sway can never wander off with the thing it is moving.
    private void Remember()
    {
        if (remembered)
        {
            return;
        }

        restingPosition = transform.localPosition;
        restingRotation = transform.localRotation;
        remembered = true;
    }

    private void Update()
    {
        if (!remembered || period <= 0f)
        {
            return;
        }

        // Unscaled, like everything else in the menu: Time.timeScale still holds whatever pace the
        // last level was played at.
        float turn = (Time.unscaledTime / period + offset) * Mathf.PI * 2f;

        transform.localPosition = restingPosition + new Vector3(0f, Mathf.Sin(turn) * rise, 0f);
        transform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, Mathf.Cos(turn) * tilt);
    }

    private void Settle()
    {
        if (!remembered)
        {
            return;
        }

        transform.localPosition = restingPosition;
        transform.localRotation = restingRotation;
    }
}
