using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The one dropdown in the options window that is not a setting with a list of words in front of it.
//
// It is filled from the languages the game actually has rather than from entries typed into the
// Inspector, so a language added to LanguageSelector shows up here on its own; the entries are the
// languages' own names and are the one list in this window that must never be translated - a player
// hunting for Polish looks for "Polski", not for whatever English calls it.
//
// It writes through LanguageSelector rather than straight into the setting, because storing the
// choice is only half of it: the other half is telling everything on screen to redraw itself.
public class LanguageDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public LanguageSelector selector;

    private void OnEnable()
    {
        Fill();
    }

    private void Fill()
    {
        if (dropdown == null || selector == null || selector.Count == 0)
        {
            return;
        }

        var names = new List<string>(selector.Count);

        foreach (LanguageSelector.Language language in selector.languages)
        {
            names.Add(string.IsNullOrEmpty(language.displayName) ? language.id : language.displayName);
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(names);

        // Without notifying: putting the stored choice back on screen is not the player choosing it
        // again, and firing here would write it back and reload the dictionary every time the
        // window opens.
        dropdown.SetValueWithoutNotify(LanguageSelector.CurrentIndex);
        dropdown.RefreshShownValue();
    }

    // Wired to the dropdown's onValueChanged.
    public void Choose(int index)
    {
        LanguageSelector.Select(index);
    }
}
