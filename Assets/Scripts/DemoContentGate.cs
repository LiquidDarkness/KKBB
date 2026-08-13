using UnityEngine;
using UnityEngine.UI;

// Locks a scenario's menu button in demo builds unless its StoryContainer is flagged
// availableInDemo. The decision lives in the scenario asset rather than here, so a newly added
// scenario is locked in the demo by default - just drop this component on its button and point
// it at the StoryContainer, and nothing else has to be remembered.
//
// Demo builds are produced by adding the DEMO_BUILD scripting define symbol (Project Settings >
// Player > Other Settings > Scripting Define Symbols). Everything else in the project is
// identical between demo and full, so there is nothing to port between them.
[RequireComponent(typeof(Button))]
public class DemoContentGate : MonoBehaviour
{
    [Tooltip("The scenario this button starts. Its availableInDemo flag decides whether the button works in a demo build.")]
    public StoryContainer scenario;

    [Tooltip("Optional. Shown only while the button is locked - e.g. a 'full version only' badge.")]
    public GameObject lockedIndicator;

    private void Awake()
    {
        bool locked = IsLocked();

        GetComponent<Button>().interactable = !locked;

        if (lockedIndicator != null)
        {
            lockedIndicator.SetActive(locked);
        }
    }

    private bool IsLocked()
    {
#if DEMO_BUILD
        if (scenario == null)
        {
            Debug.LogWarning($"{nameof(DemoContentGate)} on '{name}' has no scenario assigned - locking it, since an unlocked button here would leak full-version content into the demo.", this);
            return true;
        }

        return !scenario.availableInDemo;
#else
        return false;
#endif
    }
}
