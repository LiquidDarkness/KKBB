using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    public float desiredPositionX;
    public float paddleWidth;
    public bool navigateByMouse;
    public Transform mountPoint;
    public float paddleSpeed;

    private float screenWidthInUnits;
    private float cameraSize;
    private Camera targetCamera;

    public float currentPositionX;

    private void Start()
    {
        targetCamera = Camera.main;
        paddleWidth = GetComponent<Collider2D>().bounds.size.x;
        CalculateScreenSize();
        SetPosition(0.5f);
        currentPositionX = desiredPositionX;
        Move();
    }

    private void Update()
    {
        CalculateScreenSize();
        SetXPosition();
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

    private void SetXPosition()
    {
        if (navigateByMouse)
        {
            CalculatePaddleFromMouse();
        }
        else
        {
            CalculatePaddleFromKeyboard();
        }
    }

    private void CalculatePaddleFromMouse()
    {
        ReadMouseInput();
        Move();
    }

    private void CalculatePaddleFromKeyboard()
    {
        ReadKeyboardInput();
        Move();
    }

    private void ReadKeyboardInput()
    {
        float v = Input.GetAxis("PaddleMovementNavigation");
        if (v == 0)
        {
            desiredPositionX = currentPositionX;
            return;
        }

        SetPosition((v + 1) / 2);
    }

    private void ReadMouseInput()
    {
        float screenHeightInUnits = targetCamera.orthographicSize * 2;
        screenWidthInUnits = screenHeightInUnits * targetCamera.aspect;
        float mousePosition = targetCamera.ScreenToViewportPoint(Input.mousePosition).x;
        desiredPositionX = mousePosition * screenWidthInUnits;
    }

    public void Move()
    {
        float minX = paddleWidth / 2;
        float maxX = screenWidthInUnits - (paddleWidth / 2);
        var xPosition = Mathf.Clamp(desiredPositionX, minX, maxX);
        currentPositionX = Mathf.MoveTowards(currentPositionX, xPosition, paddleSpeed * Time.deltaTime);
        transform.position = new Vector2(currentPositionX - (screenWidthInUnits / 2), transform.position.y);
    }

    public void SetPosition(float viewportPosition)
    {
        desiredPositionX = viewportPosition * screenWidthInUnits;
    }
}
