using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    public float desiredPositionX;
    public float paddleWidth;
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
        CalcuteBounds(out float minX, out float maxX);
        currentPositionX = desiredPositionX = (minX + maxX) / 2f;
        Move();
    }

    private void Update()
    {
        CalculateScreenSize();
        ReadKeyboardInput();
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

    public void Move()
    {
        CalcuteBounds(out float minX, out float maxX);
        var xPosition = Mathf.Clamp(desiredPositionX, minX, maxX);
        currentPositionX = Mathf.MoveTowards(currentPositionX, xPosition, paddleSpeed * Time.deltaTime);
        transform.position = new Vector2(currentPositionX - (screenWidthInUnits / 2), transform.position.y);
    }

    public void SetPosition(float viewportPosition)
    {
        desiredPositionX = viewportPosition * screenWidthInUnits;
    }
}
