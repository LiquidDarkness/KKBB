using UnityEngine;

// Ties this GameObject's Animator playback speed to DifficultySettings.blockAnimationSpeed.
// Attach directly to whichever GameObject actually carries the Animator - in formation
// prefabs the animated object is often a decorative sibling/child, not the Block-scripted
// GameObject itself, so this can't just be folded into Block.cs.
[RequireComponent(typeof(Animator))]
public class BlockAnimatorSpeed : MonoBehaviour
{
    public DiffcultyManager difficultySettings;

    private Animator animator;
    private float cachedSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ApplySpeed(difficultySettings.CurrentSettings);
        if (PauseManager.IsPaused)
        {
            animator.speed = 0f;
        }

        DiffcultyManager.OnSettingsChanged += ApplySpeed;
        PauseManager.OnPause += HandlePause;
        PauseManager.OnUnpause += HandleUnpause;
    }

    private void OnDestroy()
    {
        DiffcultyManager.OnSettingsChanged -= ApplySpeed;
        PauseManager.OnPause -= HandlePause;
        PauseManager.OnUnpause -= HandleUnpause;
    }

    private void ApplySpeed(DifficultySettings settings)
    {
        cachedSpeed = settings.blockAnimationSpeed;
        // Don't stomp the freeze if a difficulty change happens while paused (e.g. from the
        // Options window) - the cached value still applies once HandleUnpause runs.
        if (!PauseManager.IsPaused)
        {
            animator.speed = cachedSpeed;
        }
    }

    private void HandlePause()
    {
        animator.speed = 0f;
    }

    private void HandleUnpause()
    {
        animator.speed = cachedSpeed;
    }
}
