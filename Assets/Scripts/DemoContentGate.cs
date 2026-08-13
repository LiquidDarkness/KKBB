using UnityEngine;
using UnityEngine.UI;

// Disables (but keeps visible) this GameObject's Button in demo builds - the scenario stays
// on screen so players can see how much more content the full version has, it just can't be
// clicked. Add the DEMO_BUILD scripting define symbol (Project Settings > Player > Other
// Settings > Scripting Define Symbols) before building the demo version, and remove it for the
// full version - everything else in the project stays identical between the two builds, so
// there is nothing to port between them.
[RequireComponent(typeof(Button))]
public class DemoContentGate : MonoBehaviour
{
    private void Awake()
    {
#if DEMO_BUILD
        GetComponent<Button>().interactable = false;
#endif
    }
}
