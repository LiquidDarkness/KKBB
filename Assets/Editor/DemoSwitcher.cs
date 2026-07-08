using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu]
public class DemoSwitcher : ScriptableObject
{
    public UnityEvent onDemoSet;
    public UnityEvent onFullSet;

    [ContextMenu(nameof(SetDemo))]
    public void SetDemo()
    {
        onDemoSet.Invoke();
    }

    [ContextMenu(nameof(SetFull))]
    public void SetFull()
    {
        onFullSet.Invoke();
    }
}
