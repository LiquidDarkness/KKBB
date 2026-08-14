using System.Collections.Generic;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    [Header("Config")]
    public float xPush = 2f;
    public float yPush = 15f;
    [Tooltip("Random nudge, in world units per second, added to the velocity on every bounce " +
        "so identical bounces stop repeating forever. 0 turns it off.")]
    public float randomFactor = 0.2f;
    public float minVelocity;
    [Tooltip("Degrees per second the flight path is bent downwards. 0 turns it off.")]
    public float fakeGravityFactor;
    [SerializeField] private float launchBallSpeed;
    public int velocitySamples = 10;

    [Header("Anti-stall")]
    [Tooltip("The ball is never allowed to fly within this many degrees of horizontal.")]
    public float minAngleFromHorizontal = 20f;
    [Tooltip("The ball is never allowed to fly within this many degrees of vertical.")]
    public float minAngleFromVertical = 8f;
    [Tooltip("How long the ball may stay inside a narrow horizontal band before it is kicked " +
        "out of it. 0 turns the watchdog off.")]
    public float stallTimeout = 4f;
    [Tooltip("Height of that band, in world units.")]
    public float stallHeightSpan = 1f;
    [Tooltip("Angle below horizontal the ball is sent at when it stalls.")]
    public float stallEscapeAngle = 55f;

    [Header("Runtime state")]
    public float currentVelocity;
    public bool hasBeenLaunched;
    public bool canBeLaunched;

    private readonly Queue<float> velocityAverage = new();
    private Rigidbody2D myRigidBody2D;
    private Transform lastMountPoint;

    private float stallTimer;
    private float stallMinY;
    private float stallMaxY;

    private void Awake()
    {
        myRigidBody2D = GetComponent<Rigidbody2D>();
        SetLaunchBool(string.Empty);
        SceneLoader.OnSceneChanged += SetLaunchBool;
        Level.OnLevelCompleted += ReLockBall;
    }

    private void OnDestroy()
    {
        SceneLoader.OnSceneChanged -= SetLaunchBool;
        Level.OnLevelCompleted -= ReLockBall;
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

        ClampFlightAngle();
        TrackStall();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasBeenLaunched || randomFactor <= 0f)
        {
            return;
        }

        // Perfectly repeatable bounces are what lets the ball settle into an endless rally, so
        // every impact is knocked slightly off course. AdjustVelocity puts the speed back.
        myRigidBody2D.velocity += new Vector2(
            Random.Range(-randomFactor, randomFactor),
            Random.Range(-randomFactor, randomFactor)
        );
    }

    private void Update()
    {
        ApplyFakeGravity();
        SetLaunchBool(string.Empty);
    }

    private void ApplyFakeGravity()
    {
        Vector3 velocity = myRigidBody2D.velocity;
        currentVelocity = velocity.magnitude;

        if (fakeGravityFactor <= 0f)
        {
            return;
        }

        // Scaled by deltaTime, so the curve of the flight does not depend on the frame rate.
        float step = fakeGravityFactor * Time.deltaTime;

        velocity = Quaternion.Euler(
            0, 0,
            velocity.x < 0 ? step : -step
        ) * velocity;

        myRigidBody2D.velocity = velocity;
    }

    // The ball must never settle into a flat left-right rally: with frictionless walls such a
    // path repeats forever and the level can never be finished. A random nudge only delays it,
    // so instead the direction is kept out of a forbidden cone around both axes. Speed and
    // quadrant are preserved, so this is invisible until the ball actually goes too flat.
    private void ClampFlightAngle()
    {
        if (!hasBeenLaunched)
        {
            return;
        }

        Vector2 velocity = myRigidBody2D.velocity;
        float speed = velocity.magnitude;
        if (speed < Mathf.Epsilon)
        {
            return;
        }

        float maxAngle = Mathf.Max(minAngleFromHorizontal, 90f - minAngleFromVertical);
        float angle = Mathf.Atan2(Mathf.Abs(velocity.y), Mathf.Abs(velocity.x)) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Clamp(angle, minAngleFromHorizontal, maxAngle);

        if (Mathf.Abs(clampedAngle - angle) < 0.01f)
        {
            return;
        }

        float signX = velocity.x < 0f ? -1f : 1f;
        // A dead-flat ball has no vertical direction to preserve, so send it down - towards the
        // paddle, where the player can do something about it.
        float signY = velocity.y > 0f ? 1f : -1f;
        float radians = clampedAngle * Mathf.Deg2Rad;

        myRigidBody2D.velocity = new Vector2(
            signX * Mathf.Cos(radians),
            signY * Mathf.Sin(radians)
        ) * speed;
    }

    // Geometry can trap the ball even at a legal angle - a flat corridor between two unbreakable
    // rows bounces it back and forth without ever letting it climb or fall out. If it spends
    // stallTimeout seconds inside a band only stallHeightSpan tall, it gets steered downwards,
    // which always resolves into either the paddle or a lost life.
    private void TrackStall()
    {
        if (!hasBeenLaunched || stallTimeout <= 0f)
        {
            ResetStallTracking();
            return;
        }

        float height = transform.position.y;
        stallMinY = Mathf.Min(stallMinY, height);
        stallMaxY = Mathf.Max(stallMaxY, height);
        stallTimer += Time.fixedDeltaTime;

        if (stallMaxY - stallMinY > stallHeightSpan)
        {
            ResetStallTracking();
            return;
        }

        if (stallTimer < stallTimeout)
        {
            return;
        }

        KickOutOfStall();
        ResetStallTracking();
    }

    private void ResetStallTracking()
    {
        stallTimer = 0f;
        stallMinY = transform.position.y;
        stallMaxY = transform.position.y;
    }

    private void KickOutOfStall()
    {
        Vector2 velocity = myRigidBody2D.velocity;
        float speed = Mathf.Max(velocity.magnitude, minVelocity);
        float signX = velocity.x < 0f ? -1f : 1f;
        float radians = stallEscapeAngle * Mathf.Deg2Rad;

        myRigidBody2D.velocity = new Vector2(
            signX * Mathf.Cos(radians),
            -Mathf.Sin(radians)
        ) * speed;
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

    public void SetLaunchBool(string _)
    {
        if (StoryManager.isStoryActive || PauseManager.IsPaused|| hasBeenLaunched)
        {
            canBeLaunched = false;
        }
        else
        {
            canBeLaunched = true;
        }
    }
}
