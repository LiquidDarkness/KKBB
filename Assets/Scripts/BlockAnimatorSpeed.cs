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

    // Read by name rather than through a serialized field: this component sits on seventeen
    // formation prefabs, and every animated formation added later would need the field filled in
    // too. See OptionSettings for why that trade is worth it here and nowhere else.
    private TypeDistinguisher reduceMotion;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        reduceMotion = OptionSettings.Find(OptionSettings.ReduceMotion);
        ApplySpeed(difficultySettings.CurrentSettings);
        if (PauseManager.IsPaused)
        {
            animator.speed = 0f;
        }

        DiffcultyManager.OnSettingsChanged += ApplySpeed;
        PauseManager.OnPause += HandlePause;
        PauseManager.OnUnpause += HandleUnpause;

        if (reduceMotion != null)
        {
            reduceMotion.OnValueChanged += ApplyCurrentSpeed;
        }
    }

    private void OnDestroy()
    {
        DiffcultyManager.OnSettingsChanged -= ApplySpeed;
        PauseManager.OnPause -= HandlePause;
        PauseManager.OnUnpause -= HandleUnpause;

        if (reduceMotion != null)
        {
            reduceMotion.OnValueChanged -= ApplyCurrentSpeed;
        }
    }

    // Zero while the player has motion turned down: the block keeps its artwork and stops moving.
    private float TargetSpeed => OptionSettings.MotionReduced ? 0f : cachedSpeed;

    private void ApplyCurrentSpeed()
    {
        if (!PauseManager.IsPaused)
        {
            animator.speed = TargetSpeed;
        }
    }

    private void ApplySpeed(DifficultySettings settings)
    {
        cachedSpeed = settings.blockAnimationSpeed;
        // Don't stomp the freeze if a difficulty change happens while paused (e.g. from the
        // Options window) - the cached value still applies once HandleUnpause runs.
        ApplyCurrentSpeed();
    }

    private void HandlePause()
    {
        animator.speed = 0f;
    }

    private void HandleUnpause()
    {
        animator.speed = TargetSpeed;
    }
}
