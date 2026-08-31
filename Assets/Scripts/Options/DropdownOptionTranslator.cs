using System.Collections.Generic;
using TMPro;
using UnityEngine;

// A dropdown keeps its options as plain strings on the dropdown itself, so a TranslationMediator -
// which writes one string into one label - cannot reach them. This does the mediator's job for a
// whole list: what was authored in the Inspector is the key, exactly like everywhere else, and the
// list is looked up again whenever the language changes.
//
// Not for a dropdown filled at runtime: ResolutionSelector writes its own options and they are
// numbers, which no language spells differently.
public class DropdownOptionTranslator : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    // The English text as authored, kept because the first translation overwrites it and the second
    // one would otherwise have nothing left to look up.
    private List<string> keys;

    private void OnEnable()
    {
        if (dropdown == null)
        {
            return;
        }

        if (keys == null)
        {
            keys = new List<string>(dropdown.options.Count);

            foreach (TMP_Dropdown.OptionData option in dropdown.options)
            {
                keys.Add(option.text);
            }
        }

        TranslationJSONDeserializer.OnTransaltionUpdated += Apply;
        Apply();
    }

    private void OnDisable()
    {
        TranslationJSONDeserializer.OnTransaltionUpdated -= Apply;
    }

    private void Apply()
    {
        if (dropdown == null || keys == null)
        {
            return;
        }

        int count = Mathf.Min(keys.Count, dropdown.options.Count);

        for (int i = 0; i < count; i++)
        {
            dropdown.options[i].text = TranslationLookup.Get(keys[i]);
        }

        // The caption is a copy of the chosen option, taken when it was chosen, so it keeps the old
        // language until it is told otherwise.
        dropdown.RefreshShownValue();
    }
}
