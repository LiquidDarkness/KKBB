using TMPro;
using UnityEngine;

// Makes a line of text bounce, one letter after another, the way the cat does. Put on a menu button
// it is what draws the eye to it without a second graphic having to be drawn for it.
//
// The letters are moved in the mesh rather than by scaling the object, on purpose: a scaled
// RectTransform is a thing a layout group can be told to measure, and a button that resized its
// neighbours sixty times a second would shake the whole menu. Nothing here touches the transform,
// the colours or the layout, so it sits happily on a button that is also being tinted by its own
// Button transitions, or coloured by RainbowEffect.
//
// Switched off - by the Menu animations option, or by Reduce motion through the ComponentEnabler
// beside it - the letters are put straight back where the layout had them.
[RequireComponent(typeof(TMP_Text))]
public class TextBounceEffect : MonoBehaviour
{
    [Tooltip("How high a letter rises, as a share of the font size. 0.08 is a nudge; 0.3 is a pogo stick.")]
    public float height = 0.09f;

    [Tooltip("Bounces per second.")]
    public float speed = 1.4f;

    [Tooltip("How far the wave is carried between one letter and the next, in turns. 0 makes the whole word hop as one.")]
    public float letterOffset = 0.12f;

    private TMP_Text text;

    // Where the letters belong when nothing is bouncing them. Read back from the mesh every time TMP
    // rebuilds it - a language change, a new font size from the text scale option - and never while
    // the wave is applied, which is what would let the letters climb away frame by frame.
    private Vector3[][] restingPlaces;
    private bool needsResting = true;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // TMP tells everyone when it has rebuilt a text, and a rebuild is the only thing that can
        // move a letter's resting place. Subscribed here and dropped in OnDisable, because this
        // component is switched on and off by the options.
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);

        needsResting = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);

        // Straight back to where the layout put them. Without this the letters stay wherever the
        // last frame of the wave left them, and switching the option off would leave a button
        // frozen mid-bounce.
        if (text != null)
        {
            text.ForceMeshUpdate();
        }
    }

    private void HandleTextChanged(Object changed)
    {
        if (changed == text)
        {
            needsResting = true;
        }
    }

    // Late, so the letters are moved after anything that might have rebuilt the text this frame.
    private void LateUpdate()
    {
        if (text == null || text.textInfo == null)
        {
            return;
        }

        TMP_TextInfo info = text.textInfo;

        if (needsResting)
        {
            // The mesh has just been rebuilt, so what it holds now is the resting layout.
            RememberRestingPlaces(info);
            needsResting = false;
        }

        if (restingPlaces == null)
        {
            return;
        }

        // Unscaled: the menu is drawn while Time.timeScale still holds whatever the last level was
        // played at, and after a few endless waves that is not one.
        float now = Time.unscaledTime;
        bool moved = false;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];

            if (!character.isVisible)
            {
                continue;
            }

            int mesh = character.materialReferenceIndex;
            int vertex = character.vertexIndex;

            if (mesh >= restingPlaces.Length || restingPlaces[mesh] == null || vertex + 3 >= restingPlaces[mesh].Length)
            {
                // The text was rebuilt between the resting places being read and now.
                needsResting = true;
                return;
            }

            // Half a bounce is a rest: a full sine spends as much time below the line as above it,
            // and letters dipping into the row beneath read as a wobble rather than a bounce.
            float turn = now * speed + i * letterOffset;
            float rise = Mathf.Abs(Mathf.Sin(turn * Mathf.PI)) * height * text.fontSize;
            var offset = new Vector3(0f, rise, 0f);

            Vector3[] vertices = info.meshInfo[mesh].vertices;
            Vector3[] resting = restingPlaces[mesh];

            vertices[vertex + 0] = resting[vertex + 0] + offset;
            vertices[vertex + 1] = resting[vertex + 1] + offset;
            vertices[vertex + 2] = resting[vertex + 2] + offset;
            vertices[vertex + 3] = resting[vertex + 3] + offset;

            moved = true;
        }

        if (!moved)
        {
            return;
        }

        for (int mesh = 0; mesh < info.meshInfo.Length; mesh++)
        {
            info.meshInfo[mesh].mesh.vertices = info.meshInfo[mesh].vertices;
            text.UpdateGeometry(info.meshInfo[mesh].mesh, mesh);
        }
    }

    private void RememberRestingPlaces(TMP_TextInfo info)
    {
        restingPlaces = new Vector3[info.meshInfo.Length][];

        for (int mesh = 0; mesh < info.meshInfo.Length; mesh++)
        {
            Vector3[] vertices = info.meshInfo[mesh].vertices;

            if (vertices == null)
            {
                continue;
            }

            restingPlaces[mesh] = (Vector3[])vertices.Clone();
        }
    }
}
