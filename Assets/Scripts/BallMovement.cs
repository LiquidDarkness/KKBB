using UnityEngine;

public class BallMovement : MonoBehaviour
{
    [Header("Config")]
    public float xPush = 2f;
    public float yPush = 15f;
    [Tooltip("Random nudge, in world units per second, added to the velocity on every bounce " +
        "so identical bounces stop repeating forever. 0 turns it off.")]
    public float randomFactor = 0.2f;
    [Tooltip("Speed the ball is held at when the cat in play does not name one of its own. Each cat can, in its entry on AvatarSwitcher.")]
    public float defaultSpeed = 10f;
    [Tooltip("Degrees per second the flight path is bent downwards. 0 turns it off.")]
    public float fakeGravityFactor;

    [Header("Arena")]
    [Tooltip("Names of the barriers the ball must never pass, or come to rest against. A name that matches nothing leaves that side of the arena unguarded, and says so once on load. The bottom is deliberately not guarded - that is where a life is lost.")]
    public string leftBarrierName = "Leftbarrier";
    public string rightBarrierName = "RightBarrier";
    public string upperBarrierName = "UpperBarrier";
    [Tooltip("How far back inside the arena the ball is placed when it has managed to touch or cross a barrier.")]
    public float barrierSkin = 0.05f;

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

    private Rigidbody2D myRigidBody2D;
    private AvatarSwitcher avatarSwitcher;
    private Transform lastMountPoint;

    private Collider2D ballCollider;
    private bool arenaResolved;
    private float arenaLeft = float.NegativeInfinity;
    private float arenaRight = float.PositiveInfinity;
    private float arenaTop = float.PositiveInfinity;

    private enum BarrierSide
    {
        Left,
        Right,
        Top,
    }

    // Every cat is the ball, so every cat gets to fly at its own speed; a cat that leaves its
    // entry at 0 flies at the default. Read live rather than resolved once, since the cat can be
    // swapped from the menu between levels.
    public float CurrentSpeed
    {
        get
        {
            float speedForThisCat = avatarSwitcher != null ? avatarSwitcher.CurrentBallSpeed : 0f;
            return speedForThisCat > 0f ? speedForThisCat : defaultSpeed;
        }
    }

    private float stallTimer;
    private float stallMinY;
    private float stallMaxY;

    private void Awake()
    {
        myRigidBody2D = GetComponent<Rigidbody2D>();
        avatarSwitcher = GetComponentInChildren<AvatarSwitcher>(true);
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
        SitUpright();
        HoldSpeed();
        ClampFlightAngle();
        KeepInsideArena();
        TrackStall();
    }

    // A cat waiting on the paddle sits up straight, and goes on sitting up straight. Ordering this
    // once inside LockBall was not enough: whatever physics does with a frozen body's pose between
    // that call and the next step is not something this side can see, and twice now the answer has
    // been a cat left leaning. So it is stated again every step for as long as she is waiting,
    // which costs one comparison and settles the question without having to win a race.
    private void SitUpright()
    {
        if (hasBeenLaunched)
        {
            return;
        }

        if (transform.localRotation != Quaternion.identity)
        {
            transform.localRotation = Quaternion.identity;
        }

        if (myRigidBody2D.angularVelocity != 0f)
        {
            myRigidBody2D.angularVelocity = 0f;
        }
    }

    // The barriers alone cannot promise the ball stays in the arena. They are a tenth of a unit
    // thick, the quicker cats cross more than twice that in one physics step, and a discrete
    // solver only ever looks at where a body ended up - a step that starts inside and ends
    // outside meets nothing on the way. Continuous detection on the rigidbody closes most of
    // that; this closes the rest, and does it without trusting the solver at all. Wherever the
    // ball has got to, it is put back inside and pointed away from the surface it reached, so it
    // can neither cross a barrier nor settle against one. Firing on a plain touch is intended:
    // the correction is then simply the bounce, done exactly.
    //
    // The floor is left out on purpose - that is where a life is lost, and the ball has to be
    // able to reach it.
    private void KeepInsideArena()
    {
        if (!hasBeenLaunched)
        {
            return;
        }

        if (!arenaResolved)
        {
            ResolveArena();
        }

        Collider2D shape = ResolveBallCollider();
        if (shape == null)
        {
            return;
        }

        Bounds ball = shape.bounds;
        Vector2 correction = Vector2.zero;
        Vector2 velocity = myRigidBody2D.velocity;

        if (ball.max.x > arenaRight)
        {
            correction.x = arenaRight - ball.max.x - barrierSkin;
            velocity.x = -Mathf.Abs(velocity.x);
        }
        else if (ball.min.x < arenaLeft)
        {
            correction.x = arenaLeft - ball.min.x + barrierSkin;
            velocity.x = Mathf.Abs(velocity.x);
        }

        if (ball.max.y > arenaTop)
        {
            correction.y = arenaTop - ball.max.y - barrierSkin;
            velocity.y = -Mathf.Abs(velocity.y);
        }

        if (correction == Vector2.zero)
        {
            return;
        }

        myRigidBody2D.position += correction;
        myRigidBody2D.velocity = velocity;
    }

    // The cat in play is whichever avatar is switched on, and its collider is the ball's shape -
    // so the check is against the real outline of the cat being played, not a guessed radius.
    private Collider2D ResolveBallCollider()
    {
        if (ballCollider != null && ballCollider.gameObject.activeInHierarchy)
        {
            return ballCollider;
        }

        // GetComponentInChildren skips the avatars that are switched off.
        ballCollider = GetComponentInChildren<Collider2D>();
        return ballCollider;
    }

    // Read once into three numbers rather than kept as live references to the colliders, and
    // that form is the whole point. Two things make a reference here dangerous. The Gameplay
    // scene holds more than one object called UpperBarrier - LevelBackground brings its own set,
    // at the same coordinates - and a Collider2D reports a zero-size bounds sitting at the origin
    // once it, or its object, is switched off. Held live, whichever copy the lookup happened to
    // land on could collapse to (0,0) later and turn the ceiling into an invisible floor across
    // the middle of the screen. Which copy that was depended on object order, which is not the
    // same in a build as in the editor, so it showed up in one and not the other.
    //
    // Numbers cannot go stale, collapsed colliders are refused outright, and of the candidates
    // the outermost wins - a duplicate can then only ever loosen the arena, never pinch it.
    // Resolved on the first physics step rather than in Awake, by when every collider is
    // certainly registered and its bounds are real.
    private void ResolveArena()
    {
        arenaResolved = true;
        arenaLeft = ResolveBarrier(leftBarrierName, BarrierSide.Left);
        arenaRight = ResolveBarrier(rightBarrierName, BarrierSide.Right);
        arenaTop = ResolveBarrier(upperBarrierName, BarrierSide.Top);
    }

    private float ResolveBarrier(string barrierName, BarrierSide side)
    {
        float unguarded = side == BarrierSide.Left ? float.NegativeInfinity : float.PositiveInfinity;

        if (string.IsNullOrEmpty(barrierName))
        {
            return unguarded;
        }

        bool found = false;
        float face = 0f;

        foreach (Collider2D candidate in FindObjectsOfType<Collider2D>())
        {
            if (candidate.name != barrierName)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;

            // A collapsed bounds means the collider is not really there - taking it at face value
            // is what put a barrier through the middle of the arena.
            if (bounds.size.sqrMagnitude < Mathf.Epsilon)
            {
                continue;
            }

            float inner = side == BarrierSide.Left ? bounds.max.x
                        : side == BarrierSide.Right ? bounds.min.x
                        : bounds.min.y;

            if (!found)
            {
                face = inner;
                found = true;
                continue;
            }

            face = side == BarrierSide.Left ? Mathf.Min(face, inner) : Mathf.Max(face, inner);
        }

        if (!found)
        {
            Debug.LogWarning($"[{nameof(BallMovement)}] no usable collider called '{barrierName}' - that side of the arena is unguarded.", this);
            return unguarded;
        }

        return face;
    }

    // Blocks and walls bounce the ball at a restitution of exactly 1, and a perfectly elastic
    // bounce is the one case the solver cannot hold to precisely - it errs upwards, so the ball
    // comes off a little faster than it went in. A floor and a ceiling left room for those gains
    // to stack up between them: the ball ratcheted from its launch speed to the ceiling over a
    // level and sat there, which is what read as speeding up at random. One exact speed leaves
    // nowhere to drift to. Direction stays entirely the solver's business; only the length of the
    // vector is ours.
    private void HoldSpeed()
    {
        if (!hasBeenLaunched)
        {
            return;
        }

        Vector2 velocity = myRigidBody2D.velocity;
        float speed = velocity.magnitude;

        // A ball stopped dead has no direction left to scale up, and scaling zero by anything is
        // still zero - it would sit there for the rest of the level. Send it at the paddle
        // instead, the same way a stall is broken.
        if (speed < Mathf.Epsilon)
        {
            KickOutOfStall();
            return;
        }

        myRigidBody2D.velocity = velocity / speed * CurrentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasBeenLaunched || randomFactor <= 0f)
        {
            return;
        }

        // Perfectly repeatable bounces are what lets the ball settle into an endless rally, so
        // every impact is knocked slightly off course. Only the direction may change: adding a
        // vector to the velocity also lengthens it, and since AdjustVelocity enforces a floor
        // and never a ceiling, that extra speed was never taken back off - the ball just kept
        // getting faster with every bounce.
        Vector2 velocity = myRigidBody2D.velocity;
        float speed = velocity.magnitude;
        if (speed < Mathf.Epsilon)
        {
            return;
        }

        Vector2 nudged = velocity + new Vector2(
            Random.Range(-randomFactor, randomFactor),
            Random.Range(-randomFactor, randomFactor)
        );

        myRigidBody2D.velocity = nudged.normalized * speed;
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
        float speed = CurrentSpeed;
        float signX = velocity.x < 0f ? -1f : 1f;
        float radians = stallEscapeAngle * Mathf.Deg2Rad;

        myRigidBody2D.velocity = new Vector2(
            signX * Mathf.Cos(radians),
            -Mathf.Sin(radians)
        ) * speed;
    }

    public void LaunchBall()
    {
        if (!canBeLaunched) return;

        transform.SetParent(null);
        myRigidBody2D.constraints = RigidbodyConstraints2D.None;
        myRigidBody2D.velocity = new Vector2(xPush, yPush).normalized * CurrentSpeed;
        hasBeenLaunched = true;
    }

    public void LockBall(Transform mountPoint)
    {
        // Everything stops before anything is moved. The body is frozen first so that nothing is
        // still being simulated while the pose below is written - zeroing the rotation and then
        // freezing left the two racing, and the spin the cat happened to be carrying could be put
        // back on the transform after it had been cleared. That is the cat hanging over the paddle
        // at an angle, which is what a player saw twice.
        myRigidBody2D.velocity = Vector2.zero;
        myRigidBody2D.angularVelocity = 0f;
        myRigidBody2D.constraints = RigidbodyConstraints2D.FreezeAll;

        transform.SetParent(mountPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        myRigidBody2D.rotation = 0f;
        lastMountPoint = mountPoint;
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
