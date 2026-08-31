using TMPro;
using UnityEngine;

// A hint that names the key it is talking about. "Press 'P' to resume" was written into the label
// and stopped being true the moment the rebinding screen let Pause be moved somewhere else, so the
// sentence became a template with a slot in it and the key is asked for every time the hint goes up.
//
// Both halves are translation keys: the sentence, and the name of the key that lands in it. The key
// names that are words - "Space", "Left mouse" - read differently in every language and have their
// own entries; a bare "A" or "F5" has none and prints itself, which is what it would have said.
public class BoundKeyHint : MonoBehaviour
{
    [Tooltip("Which action the sentence is about. One of the names in Controls.")]
    public string action = Controls.Pause;

    [Tooltip("The label the sentence is written into.")]
    public TMP_Text label;

    [Tooltip("Translation key of the sentence, with {0} where the name of the bound key goes.")]
    public string template = "Press {0} to resume";

    [Tooltip("Optional. Translation key used when Escape does the same thing as the bound key, with {0} for the bound key and {1} for Escape. Left empty, Escape is never mentioned.")]
    public string alsoEscapeTemplate = "";

    private void OnEnable()
    {
        // Three things move under this sentence: the language, the binding, and the screen going
        // away and coming back with a different binding than it left with. The first two are static
        // events that outlive this object, which is why OnDisable drops them again; the third is
        // this OnEnable itself, since the pause screen is a window the manager switches off.
        TranslationJSONDeserializer.OnTransaltionUpdated += Refresh;
        Controls.OnBindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        TranslationJSONDeserializer.OnTransaltionUpdated -= Refresh;
        Controls.OnBindingsChanged -= Refresh;
    }

    public void Refresh()
    {
        if (label == null)
        {
            return;
        }

        KeyCode bound = Controls.Primary(action);

        // Escape is worth naming beside the key only while it is a second way of doing the same
        // thing. Bound to Escape itself, the action would otherwise be offered twice in one breath.
        bool alsoEscape = !string.IsNullOrEmpty(alsoEscapeTemplate) && bound != KeyCode.Escape;

        // An action can be left bound to nothing, because taking a key for one action clears it
        // from whoever held it before. "Press - to resume" is worse than saying nothing at all, so
        // a hint with no key left to name either falls back to Escape or goes quiet.
        if (bound == KeyCode.None)
        {
            label.text = alsoEscape ? Sentence(template, KeyCode.Escape) : string.Empty;
            return;
        }

        label.text = alsoEscape
            ? Sentence(alsoEscapeTemplate, bound, KeyCode.Escape)
            : Sentence(template, bound);
    }

    private static string Sentence(string templateKey, params KeyCode[] keys)
    {
        var names = new object[keys.Length];

        for (int i = 0; i < keys.Length; i++)
        {
            names[i] = TranslationLookup.Get(Controls.Describe(keys[i]));
        }

        return string.Format(TranslationLookup.Get(templateKey), names);
    }
}
