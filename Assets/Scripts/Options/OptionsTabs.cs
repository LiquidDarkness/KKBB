using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Turns the row of category buttons at the top of the options window into real tabs: one group of
// settings is on screen at a time, and the buttons wire themselves up from the list below, so a
// new tab costs one entry here and nothing in the Inspector's event fields.
public class OptionsTabs : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public Button button;
        public GameObject group;
    }

    public List<Tab> tabs = new List<Tab>();

    [Tooltip("Tab shown every time the window opens. Out of range falls back to the first one.")]
    public int defaultTab = 0;

    [Tooltip("The open tab's button is switched off, so it reads as pressed and keyboard navigation skips over it.")]
    public bool disableActiveTabButton = true;

    [Tooltip("Optional. The scroll view holding the groups - rewound to the top on every switch, so a tab never opens half scrolled.")]
    public ScrollRect scrollView;

    [Tooltip("Move keyboard/gamepad focus onto the first control of the tab that was just opened. Off by default, so arrow keys keep walking the tab row until the player goes down into it.")]
    public bool focusFirstControlOnSwitch = false;

    private int activeTab = -1;

    // Wired here and not in Awake on purpose: the options window is switched off and on again for
    // every visit, and OnEnable is the only hook that runs on the second visit too. Runtime
    // listeners are dropped first so a second visit does not stack a second copy of each; the
    // click sound wired in the Inspector is a persistent listener and survives RemoveAllListeners.
    private void OnEnable()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            Button button = tabs[i].button;

            if (button == null)
            {
                Debug.LogWarning($"{nameof(OptionsTabs)}: tab {i} has no button assigned.", this);
                continue;
            }

            int index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTab(index));
        }

        activeTab = -1;
        ShowTab(tabs.Count > 0 && defaultTab >= 0 && defaultTab < tabs.Count ? defaultTab : 0);
    }

    public void ShowTab(int index)
    {
        if (index < 0 || index >= tabs.Count || index == activeTab)
        {
            return;
        }

        activeTab = index;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = i == index;

            if (tabs[i].group != null)
            {
                tabs[i].group.SetActive(isActive);
            }

            if (tabs[i].button != null && disableActiveTabButton)
            {
                tabs[i].button.interactable = !isActive;
            }
        }

        if (scrollView != null)
        {
            scrollView.verticalNormalizedPosition = 1f;
        }

        if (focusFirstControlOnSwitch)
        {
            FocusFirstControl(tabs[index].group);
        }
    }

    // Called by the buttons only when focusFirstControlOnSwitch is on. A tab whose group holds no
    // interactive control at all leaves the current selection where it is rather than clearing it,
    // which would strand a keyboard player with nothing selected.
    private void FocusFirstControl(GameObject group)
    {
        if (group == null || EventSystem.current == null)
        {
            return;
        }

        foreach (Selectable candidate in group.GetComponentsInChildren<Selectable>())
        {
            if (candidate.IsInteractable() && candidate.navigation.mode != Navigation.Mode.None)
            {
                EventSystem.current.SetSelectedGameObject(candidate.gameObject);
                return;
            }
        }
    }
}
