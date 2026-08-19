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
        bool keyboardMoved = ReadKeyboardInput(out float keyboardPosition);
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
            SetPosition(keyboardPosition);
        }
        else
        {
            // Nothing held: the paddle stops where it got to instead of drifting on towards a
            // target it was still travelling to.
            desiredPositionX = currentPositionX;
        }
    }

    private bool ReadKeyboardInput(out float viewportPosition)
    {
        float v = Input.GetAxis("PaddleMovementNavigation");
        viewportPosition = (v + 1) / 2;
        return v != 0;
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
