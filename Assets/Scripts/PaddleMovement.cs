using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    // Stored as a plain int in the options so it can ride a TMP_Dropdown straight into
    // TypeDistinguisher.SetIntValue. The order here is the order of the dropdown entries.
    public enum ControlMode
    {
        LastUsed = 0,
        Keyboard = 1,
        Mouse = 2,
    }

    public float desiredPositionX;
    public float paddleWidth;
    public Transform mountPoint;
    public float paddleSpeed;

    [Header("Control")]
    [Tooltip("INT setting: 0 = whichever device was used last, 1 = keyboard/gamepad only, 2 = mouse only. An unset key reads as 0, which is the intended default.")]
    public TypeDistinguisher controlModeSetting;

    [Tooltip("How far the mouse has to move in one frame before it takes the paddle back in 'last used' mode. Keeps a resting hand or a nudged desk from stealing it mid-rally.")]
    public float mouseTakeoverThreshold = 0.15f;

    [Tooltip("The paddle sits under the cursor with no catching up to do. This is what mouse control is for - a mouse names a spot rather than pushing towards one, and travelling to it makes the paddle feel slower than the keyboard.")]
    public bool mouseFollowsInstantly = true;

    [Tooltip("Paddle speed used while the mouse is steering and it does not follow instantly. 0 keeps the shared paddleSpeed.")]
    public float mousePaddleSpeed = 0f;

    [Header("Keyboard")]
    [Tooltip("How fast the keyboard steer winds up to full speed, per second. This was the Input Manager axis sensitivity, kept at the value it had.")]
    public float keySensitivity = 3f;

    [Tooltip("How fast it winds back down to a standstill once the keys are let go - the paddle coasts for that long. This was the axis gravity.")]
    public float keyGravity = 3f;

    private float keyAxis;
    private float screenWidthInUnits;
    private float cameraSize;
    private Camera targetCamera;
    private bool mouseSteering;

    public float currentPositionX;

    private void Awake()
    {
        // Awake (not Start): this must be ready the instant the paddle is instantiated. When a
        // drop swaps paddles, PlayerRig.HandlePaddleChanged positions the new paddle by calling
        // Move() in the same frame it's created - Start() would still be pending at that point,
        // leaving screenWidthInUnits/paddleWidth at 0 and placing the paddle in the wrong spot.
        targetCamera = Camera.main;
        paddleWidth = GetComponent<Collider2D>().bounds.size.x;
        CalculateScreenSize();
        CalcuteBounds(out float minX, out float maxX);
        currentPositionX = desiredPositionX = (minX + maxX) / 2f;
        mouseSteering = CurrentMode == ControlMode.Mouse;
        Move();
    }

    private void Update()
    {
        CalculateScreenSize();

        // Nothing steers while the game is paused. Every other path here travels towards its target
        // by Time.deltaTime and so stops of its own accord at timeScale 0, but the mouse sets the
        // position outright - which is how the paddle went wandering after the cursor while the
        // options window was open, and the ball was away before the player had looked up.
        if (PauseManager.IsPaused)
        {
            return;
        }

        // A beat on screen holds the paddle still until it has been read to its last line. The
        // paddle is how the block that carries on is reached, so letting it move sooner is letting
        // the player leave a beat they have not seen - which is exactly what they did.
        if (StoryManager.isStoryActive && !StoryTextScrollSetup.TextRead)
        {
            return;
        }

        ReadInput();
        Move();
    }

    private void CalculateScreenSize()
    {
        if (targetCamera.orthographicSize != cameraSize)
        {
            float screenHeightInUnits = targetCamera.orthographicSize * 2;
            screenWidthInUnits = screenHeightInUnits * targetCamera.aspect;
            cameraSize = targetCamera.orthographicSize;
        }
    }

    public ControlMode CurrentMode
    {
        get
        {
            if (controlModeSetting == null)
            {
                return ControlMode.LastUsed;
            }

            int stored = controlModeSetting.IntValue;
            return stored >= (int)ControlMode.LastUsed && stored <= (int)ControlMode.Mouse
                ? (ControlMode)stored
                : ControlMode.LastUsed;
        }
    }

    // Both devices are read every frame, whatever the mode: in 'last used' the paddle follows the
    // one that moved most recently and neither can lock the other out, and in a fixed mode the
    // other device's reading is simply thrown away.
    private void ReadInput()
    {
        bool keyboardMoved = UpdateKeySteer();
        bool mouseMoved = Mathf.Abs(Input.GetAxis("Mouse X")) > mouseTakeoverThreshold;

        switch (CurrentMode)
        {
            case ControlMode.Keyboard:
                mouseSteering = false;
                break;

            case ControlMode.Mouse:
                mouseSteering = true;
                break;

            default:
                if (keyboardMoved)
                {
                    mouseSteering = false;
                }
                else if (mouseMoved)
                {
                    mouseSteering = true;
                }
                break;
        }

        if (mouseSteering)
        {
            SetPosition(MouseViewportX());
            return;
        }

        if (keyboardMoved)
        {
            StepKeySteer();
        }
        else
        {
            // Nothing held: the paddle stops where it got to instead of drifting on towards a
            // target it was still travelling to.
            desiredPositionX = currentPositionX;
        }
    }

    // The Input Manager axis this used to read cannot be rebound while the game is running, so the
    // ramp it gave for free is done here instead - the same wind-up, wind-down and direction snap,
    // driven by whichever keys the player has chosen. A gamepad stick skips the ramp; it has its
    // own idea of how hard it is being pushed and does not need one invented for it.
    private bool UpdateKeySteer()
    {
        float stick = Controls.MoveAxis();

        // A stick says how far, not only which way, so it steers straight instead of through the
        // wind-up the keys need - half a push is half the speed. Letting go drops through to the
        // branch below and coasts to a stop the same way, so the two read as one control.
        if (stick != 0f)
        {
            keyAxis = Mathf.Clamp(stick, -1f, 1f);
            return true;
        }

        float target = (Controls.Held(Controls.MoveRight) ? 1f : 0f) - (Controls.Held(Controls.MoveLeft) ? 1f : 0f);

        if (target != 0f)
        {
            // Snap, as the axis had: turning around starts from the middle instead of coasting
            // through it.
            if (keyAxis != 0f && Mathf.Sign(keyAxis) != Mathf.Sign(target))
            {
                keyAxis = 0f;
            }

            keyAxis = Mathf.MoveTowards(keyAxis, target, keySensitivity * Time.deltaTime);
        }
        else
        {
            keyAxis = Mathf.MoveTowards(keyAxis, 0f, keyGravity * Time.deltaTime);
        }

        return keyAxis != 0f;
    }

    // The keyboard steers by speed: how long a key is held decides how far the paddle travels, and
    // letting go coasts it to a stop wherever it stands.
    //
    // The axis used to be read as a position instead - full right meaning the right-hand edge of
    // the screen. That looks the same while a key is held down and is wrong the moment it is let
    // go: winding the axis back down to zero walked the target back to the middle of the screen,
    // and the paddle dutifully drove off the edge it had just been parked against. Pressing
    // towards an edge the paddle was already resting on had the same fault in reverse - the target
    // started at the middle and pulled it inwards before catching up.
    private void StepKeySteer()
    {
        CalcuteBounds(out float minX, out float maxX);

        currentPositionX = Mathf.Clamp(currentPositionX + (keyAxis * paddleSpeed * Time.deltaTime), minX, maxX);

        // Move() runs straight after this and travels towards desiredPositionX. The step above is
        // the whole of the movement, so the target is the spot it just reached.
        desiredPositionX = currentPositionX;
    }

    private float MouseViewportX()
    {
        if (Screen.width <= 0)
        {
            return 0.5f;
        }

        return Mathf.Clamp01(Input.mousePosition.x / Screen.width);
    }

    public void Move()
    {
        CalcuteBounds(out float minX, out float maxX);
        var xPosition = Mathf.Clamp(desiredPositionX, minX, maxX);

        if (mouseSteering && mouseFollowsInstantly)
        {
            currentPositionX = xPosition;
        }
        else
        {
            float speed = mouseSteering && mousePaddleSpeed > 0f ? mousePaddleSpeed : paddleSpeed;
            currentPositionX = Mathf.MoveTowards(currentPositionX, xPosition, speed * Time.deltaTime);
        }
        transform.position = new Vector2(currentPositionX - (screenWidthInUnits / 2), transform.position.y);
    }

    internal void MoveToForcedPosition(float viewportPosition)
    {
        desiredPositionX = viewportPosition * screenWidthInUnits;
        CalcuteBounds(out float minX, out float maxX);
        currentPositionX = Mathf.Clamp(desiredPositionX, minX, maxX);
        transform.position = new Vector2(currentPositionX - (screenWidthInUnits / 2), transform.position.y);
    }

    private void CalcuteBounds(out float minX, out float maxX)
    {
        minX = paddleWidth / 2;
        maxX = screenWidthInUnits - (paddleWidth / 2);
    }

    public void SetPosition(float viewportPosition)
    {
        desiredPositionX = viewportPosition * screenWidthInUnits;
    }
}
