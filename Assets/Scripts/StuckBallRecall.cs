using UnityEngine;

// Catches a run that has stopped going anywhere. When no block has been broken for the number of
// seconds the player picked, the cat is put back on the paddle and waits to be launched again -
// it does not serve itself. Covers the ball wedged in a corner, the ball stopped dead, and the
// ball orbiting an empty stretch of the arena.
//
// Lives on GameSession (it must outlive every scene load), finds the ball whenever gameplay comes
// up, and does nothing at all while the setting is off.
public class StuckBallRecall : MonoBehaviour
{
    public TypeDistinguisher activeSetting;
    public TypeDistinguisher delaySetting;

    [Tooltip("Used while the delay setting is still unset - PlayerPrefs answers 0 for a key nobody has written yet.")]
    public float defaultDelay = 20f;

    [Tooltip("Range the delay is clamped to, and the range the options slider should use.")]
    public float minDelay = 5f;
    public float maxDelay = 60f;

    private BallMovement ball;
    private float idleTimer;

    private void Awake()
    {
        Block.OnBlockBroken += HandleBlockBroken;
        SceneLoader.OnGameplayLoaded += FindBall;
    }

    private void OnDestroy()
    {
        Block.OnBlockBroken -= HandleBlockBroken;
        SceneLoader.OnGameplayLoaded -= FindBall;
    }

    private void FindBall()
    {
        ball = FindObjectOfType<BallMovement>();
        idleTimer = 0f;
    }

    private void HandleBlockBroken(Vector3 _)
    {
        idleTimer = 0f;
    }

    private void Update()
    {
        if (activeSetting == null || !activeSetting.BoolValue)
        {
            idleTimer = 0f;
            return;
        }

        // Nothing here is the player's fault: a story beat is on screen, the shop is open, or the
        // cat is already sitting on the paddle waiting to be launched. Time spent in any of those
        // is not time spent stuck.
        if (PauseManager.IsPaused || StoryManager.isStoryActive)
        {
            return;
        }

        if (ball == null)
        {
            ball = FindObjectOfType<BallMovement>();
        }

        if (ball == null || !ball.hasBeenLaunched)
        {
            idleTimer = 0f;
            return;
        }

        // Unscaled: the difficulty settings drive Time.timeScale, and a run does not become less
        // stuck because the whole game is running at double speed.
        idleTimer += Time.unscaledDeltaTime;

        if (idleTimer < CurrentDelay)
        {
            return;
        }

        idleTimer = 0f;

        if (PlayerRig.instance != null)
        {
            PlayerRig.instance.LockToPaddle();
        }
    }

    public float CurrentDelay
    {
        get
        {
            float stored = delaySetting != null ? delaySetting.FloatValue : 0f;
            return stored > 0f ? Mathf.Clamp(stored, minDelay, maxDelay) : defaultDelay;
        }
    }
}
