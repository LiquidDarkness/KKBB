using System.Collections.Generic;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    [Header("Config")]
    public float xPush = 2f;
    public float yPush = 15f;
    public float randomFactor = 0.2f;
    public float minVelocity;
    public float fakeGravityFactor;
    [SerializeField] private float launchBallSpeed;
    public int velocitySamples = 10;

    [Header("Runtime state")]
    public float currentVelocity;
    public bool hasBeenLaunched;
    public bool canBeLaunched;

    private readonly Queue<float> velocityAverage = new();
    private Rigidbody2D myRigidBody2D;
    private Transform lastMountPoint;

    private void Awake()
    {
        myRigidBody2D = GetComponent<Rigidbody2D>();
        SetLaunchBool();
        SceneLoader.OnSceneChanged += SetLaunchBool;
        Level.OnLevelCompleted += ReLockBall;
    }

    private void OnDestroy()
    {
        SceneLoader.OnSceneChanged -= SetLaunchBool;
    }

    private void FixedUpdate()
    {
        if (velocityAverage.Count > velocitySamples)
        {
            velocityAverage.Dequeue();
            velocityAverage.Enqueue(myRigidBody2D.velocity.magnitude);
            AdjustVelocity();
        }
        else
        {
            velocityAverage.Enqueue(myRigidBody2D.velocity.magnitude);
        }
    }

    private void Update()
    {
        ApplyFakeGravity();
        SetLaunchBool();

        if (canBeLaunched && Input.GetButton("LaunchBall"))
        {
            LaunchBall();
        }
    }

    private void ApplyFakeGravity()
    {
        Vector3 velocity = myRigidBody2D.velocity;
        currentVelocity = velocity.magnitude;

        velocity = Quaternion.Euler(
            0, 0,
            velocity.x < 0 ? fakeGravityFactor : -fakeGravityFactor
        ) * velocity;

        myRigidBody2D.velocity = velocity;
    }

    private void AdjustVelocity()
    {
        float sum = 0;
        foreach (float velocity in velocityAverage)
        {
            sum += velocity;
        }

        sum /= velocityAverage.Count;

        if (sum < minVelocity)
        {
            myRigidBody2D.velocity = myRigidBody2D.velocity.normalized * minVelocity;
        }
    }

    public void LaunchBall()
    {
        if (!canBeLaunched) return;

        transform.SetParent(null);
        myRigidBody2D.constraints = RigidbodyConstraints2D.None;
        myRigidBody2D.velocity = new Vector2(xPush, yPush).normalized * launchBallSpeed;
        hasBeenLaunched = true;
    }

    public void LockBall(Transform mountPoint)
    {
        transform.SetParent(mountPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Nie resetujemy velocity, jeśli kulka już leci
        if (!hasBeenLaunched)
            myRigidBody2D.velocity = Vector2.zero;

        myRigidBody2D.constraints = RigidbodyConstraints2D.FreezeAll;
        lastMountPoint = mountPoint;

        // Tylko jeśli kulka nie była w ruchu
        hasBeenLaunched = false;
    }


    public void ReLockBall()
    {
        if (lastMountPoint != null)
        {
            LockBall(lastMountPoint);
        }
    }

    public void SetLaunchBool()
    {
        if (StoryManager.isStoryActive || GameSpeedManager.isGamePaused || hasBeenLaunched)
        {
            canBeLaunched = false;
        }
        else
        {
            canBeLaunched = true;
        }
    }
}
