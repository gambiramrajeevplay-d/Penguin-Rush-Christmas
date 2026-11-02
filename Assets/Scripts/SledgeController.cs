using UnityEngine;
using UnityEngine.Events;

public enum VisualTiltTarget
{
    RootOnly,          // Rotate the visualRoot object (children follow it, orbiting the root pivot).
    ChildrenOnly,      // Rotate every child of visualRoot around its OWN pivot. Root stays still.
    RootAndChildren    // Both. Tilt stacks, so lower childTiltAmount if it looks too strong.
}

[RequireComponent(typeof(Rigidbody))]
public class SledgeController : MonoBehaviour
{
    [Header("Forward Movement (Z Axis)")]
    [SerializeField] private float forwardSpeed = 12f;
    [SerializeField] private float maxForwardSpeed = 20f;
    [SerializeField] private float acceleration = 5f;
    [Tooltip("How fast the applied forward speed changes when sliding starts / stops. Prevents speed snapping.")]
    [SerializeField] private float forwardSpeedChangeRate = 30f;

    [Header("Horizontal Movement (X Axis)")]
    [SerializeField] private float horizontalSpeed = 8f;
    [SerializeField] private float horizontalSmoothness = 8f;
    [SerializeField] private float horizontalLimit = 5f;

    [Header("Visual Tilt")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Optional second visual root. It gets exactly the same tilt / pitch behaviour as the first one.")]
    [SerializeField] private Transform secondVisualRoot;
    [Tooltip("What gets tilted / pitched: the root, each child of the root, or both.")]
    [SerializeField] private VisualTiltTarget tiltTarget = VisualTiltTarget.ChildrenOnly;
    [Tooltip("Strength of the tilt applied to the children (1 = same as maxTiltAngle).")]
    [Range(0f, 2f)]
    [SerializeField] private float childTiltAmount = 1f;
    [SerializeField] private float maxTiltAngle = 12f;
    [SerializeField] private float tiltSmoothness = 8f;
    [Tooltip("Also pitch the visual to match the slope (nose up / down).")]
    [SerializeField] private bool pitchVisualToSlope = true;
    [SerializeField] private float visualPitchSmoothness = 6f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float probeStartHeight = 0.3f;
    [SerializeField] private float probeRadius = 0.3f;
    [Tooltip("How far below the sledge ground is still 'seen' (used for slope reading and landing prep).")]
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float maxGroundAngle = 70f;
    [Tooltip("Max gap between sledge and ground that still counts as grounded (jump / snap). Keep this small.")]
    [SerializeField] private float groundedDistance = 0.4f;
    [Tooltip("If the sledge moves upward faster than the slope by this much, it is treated as airborne (launching off a ramp / jumping).")]
    [SerializeField] private float riseThreshold = 1.5f;

    [Header("Slope Sampling (smooth up / down curves)")]
    [Tooltip("Distance ahead (Z) used to read the upcoming slope.")]
    [SerializeField] private float lookAheadDistance = 3f;
    [Tooltip("0 = react to the slope right under you, 1 = react to the slope further ahead.")]
    [Range(0f, 1f)]
    [SerializeField] private float lookAheadWeight = 0.5f;
    [Tooltip("How quickly the slope angle follows the terrain. Lower = smoother curves.")]
    [SerializeField] private float normalSmoothness = 5f;
    [Tooltip("Height above the ground the slope sample rays start from.")]
    [SerializeField] private float raySampleHeight = 3f;

    [Header("Ground Adhesion")]
    [Tooltip("Maximum downward pull (speed) applied when the sledge is slightly above the ground.")]
    [SerializeField] private float groundStickSpeed = 3f;
    [Tooltip("Pull per meter of gap. The pull is 0 when touching the ground, so there is no pressing / jitter.")]
    [SerializeField] private float groundStickGain = 8f;
    [Tooltip("How quickly vertical velocity follows the slope while grounded.")]
    [SerializeField] private float slopeFollowSmoothness = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("A jump pressed slightly before landing still fires on landing.")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float jumpLockTime = 0.15f;

    [Header("Jump Feel")]
    [Tooltip("Extra gravity while falling. Makes jumps snappy instead of floaty. 1 = normal gravity.")]
    [SerializeField] private float fallGravityMultiplier = 1.6f;

    [Header("Obstacles")]
    [Tooltip("Layer(s) of obstacles. Objects on these layers get disabled when the sledge hits them.")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("List of particle effect prefabs. ALL of them spawn at the exact hit point, and every ParticleSystem inside each one (root + children) is played.")]
    [SerializeField] private GameObject[] obstacleHitParticles;
    [Tooltip("Seconds before the spawned particle objects are destroyed.")]
    [SerializeField] private float particleLifetime = 2f;
    [Tooltip("Sound played when an obstacle is hit.")]
    [SerializeField] private AudioClip obstacleHitSound;
    [Range(0f, 1f)]
    [SerializeField] private float obstacleHitVolume = 1f;
    [Tooltip("Optional. If empty, an AudioSource is created on the sledge automatically.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Hides every mesh (parent + children) of the sledge on hit. The GameObject itself stays active, so the effect and sound keep playing.")]
    [SerializeField] private bool hideSledgeOnHit = true;
    [Tooltip("Stops the sledge instantly at the hit point. If OFF, it slows to a stop over Fail Stop Duration.")]
    [SerializeField] private bool freezeOnHit = true;
    [Tooltip("Seconds the sledge takes to slow to a stop after hitting an obstacle (only used when Freeze On Hit is OFF).")]
    [SerializeField] private float failStopDuration = 0.6f;
    [Tooltip("Time passed to the GameManager before the Level Fail panel appears (when Freeze On Hit is ON).")]
    [SerializeField] private float failPanelDelay = 0.6f;
    [Tooltip("Fired when an obstacle is hit. Use it for camera shake, UI, etc.")]
    public UnityEvent onObstacleHit;

    [Header("Performance / Frame Rate")]
    [Tooltip("Sets Application.targetFrameRate and turns VSync off. Mobile builds default to 30 FPS unless this is set.")]
    [SerializeField] private bool setTargetFrameRate = true;
    [SerializeField] private int targetFrameRate = 60;

    [Header("Physics Rate")]
    [Tooltip("OFF by default: a faster physics step costs more CPU per frame, which hurts if the game is already running below 30 FPS. Turn on only once the frame rate is stable at 60.")]
    [SerializeField] private bool overridePhysicsRate = false;
    [SerializeField] private float physicsRate = 60f;

    [Header("Walls / Anti-Stuck")]
    [Tooltip("Gives the sledge colliders zero friction so they never grip fences, walls or banks.")]
    [SerializeField] private bool makeColliderSlippery = true;
    [Tooltip("Limits how violently the physics pushes the sledge out of a wall it is pressed into.")]
    [SerializeField] private float maxDepenetrationVelocity = 4f;
    [Tooltip("How long a wall contact is remembered after the last touch.")]
    [SerializeField] private float wallMemoryTime = 0.1f;
    [Tooltip("Counts as blocked when real forward speed is below this fraction of the wanted speed.")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float stuckSpeedRatio = 0.4f;
    [Tooltip("How long it must be blocked before the escape nudge starts.")]
    [SerializeField] private float stuckTime = 0.2f;
    [SerializeField] private float escapeSpeed = 5f;
    [SerializeField] private float escapeDuration = 0.4f;

    [Header("Slide")]
    [SerializeField] private float slideSpeedMultiplier = 1.2f;
    [SerializeField] private float normalDrag = 0.1f;
    [SerializeField] private float slideDrag = 0.05f;

    [Header("Controls")]
    [Tooltip("Master switch. When OFF the player ignores all input (steer, jump, slide). The sledge keeps moving forward unless it is stopping.")]
    [SerializeField] private bool canControl = true;
    [SerializeField] private bool allowWASD = true;

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private float currentSpeed;
    private float appliedForwardSpeed;
    private float horizontalInput;
    private float currentHorizontalVelocity;
    private float currentTilt;

    private bool slidePressed;
    private bool isStopping;
    private float stopDeceleration;
    private bool isGrounded;
    private bool isSliding;
    private bool hasFailed;
    private bool isFrozen;

    private float jumpBufferTimer;
    private float coyoteTimer;
    private float jumpLockTimer;

    private float groundGap;

    // Wall / stuck handling
    private Vector3 wallNormal;
    private float wallTimer;
    private float stuckTimer;
    private float escapeTimer;
    private float escapeDirection;

    // Smoothed slope pitch in radians (positive = uphill).
    private float smoothedPitch;
    private float visualPitchDeg;

    // Cached so the sledge can be hidden without disabling the GameObject.
    private Renderer[] sledgeRenderers;
    private Collider[] sledgeColliders;

    private readonly VisualRig firstRig = new VisualRig();
    private readonly VisualRig secondRig = new VisualRig();

    // Holds one visual root, its children and their starting rotations.
    private class VisualRig
    {
        private Transform root;
        private Quaternion rootStartRotation = Quaternion.identity;
        private Transform[] children = new Transform[0];
        private Quaternion[] childStartRotations = new Quaternion[0];

        public bool IsValid => root != null;

        public void Setup(Transform newRoot)
        {
            root = newRoot;
            rootStartRotation = root != null ? root.localRotation : Quaternion.identity;
            children = new Transform[0];
            childStartRotations = new Quaternion[0];
            RefreshChildren();
        }

        // Re-reads the children. Children we already know keep their original start rotation.
        public void RefreshChildren()
        {
            if (root == null)
            {
                children = new Transform[0];
                childStartRotations = new Quaternion[0];
                return;
            }

            int count = root.childCount;
            Transform[] newChildren = new Transform[count];
            Quaternion[] newStarts = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                Transform child = root.GetChild(i);
                newChildren[i] = child;

                int known = System.Array.IndexOf(children, child);
                newStarts[i] = known >= 0 ? childStartRotations[known] : child.localRotation;
            }

            children = newChildren;
            childStartRotations = newStarts;
        }

        public void Apply(
            Quaternion rootOffset,
            Quaternion childOffset,
            bool rotateRoot,
            bool rotateChildren)
        {
            if (root == null)
                return;

            root.localRotation = rotateRoot
                ? rootStartRotation * rootOffset
                : rootStartRotation;

            if (!rotateChildren)
                return;

            // Each child rotates around its own pivot, in the root's space.
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null)
                    continue;

                children[i].localRotation = childOffset * childStartRotations[i];
            }
        }
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        SetupAudioSource();

        if (setTargetFrameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }

        if (overridePhysicsRate && physicsRate > 1f)
            Time.fixedDeltaTime = 1f / physicsRate;

        rb.maxDepenetrationVelocity = maxDepenetrationVelocity;

        if (makeColliderSlippery)
            ApplySlipperyMaterial();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        currentSpeed = forwardSpeed;
        appliedForwardSpeed = forwardSpeed;

        firstRig.Setup(visualRoot);
        secondRig.Setup(secondVisualRoot);

        // Cache everything that gets hidden / switched off on a hit.
        sledgeRenderers = GetComponentsInChildren<Renderer>(true);
        sledgeColliders = GetComponentsInChildren<Collider>(true);
    }

    // Uses the assigned AudioSource, then one already on the sledge, or creates one.
    private void SetupAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D sound, same volume everywhere.
    }

    private void Update()
    {
        ReadInput();
    }

    // Runs after Update and after any Animator, so nothing overwrites the tilt.
    private void LateUpdate()
    {
        UpdateVisual();
    }

    // Call this if you add or remove children of the visual roots at runtime.
    public void RefreshVisualChildren()
    {
        firstRig.RefreshChildren();
        secondRig.RefreshChildren();

        sledgeRenderers = GetComponentsInChildren<Renderer>(true);
        sledgeColliders = GetComponentsInChildren<Collider>(true);
    }

    private void ReadInput()
    {
        // Control disabled: clear everything so nothing is left "held down".
        if (!canControl)
        {
            horizontalInput = 0f;
            slidePressed = false;
            jumpBufferTimer = 0f;
            return;
        }

        float input = 0f;

        if (Input.GetKey(KeyCode.LeftArrow)) input -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) input += 1f;

        if (allowWASD)
        {
            if (Input.GetKey(KeyCode.A)) input -= 1f;
            if (Input.GetKey(KeyCode.D)) input += 1f;
        }

        input = Mathf.Clamp(input, -1f, 1f);

        if (Mathf.Abs(input) < 0.01f)
            input = Input.GetAxisRaw("Horizontal");

        horizontalInput = input;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            jumpBufferTimer = jumpBufferTime;

        slidePressed = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
    }

    private void FixedUpdate()
    {
        // Frozen after a hit: the sledge stays exactly where it crashed.
        if (isFrozen)
            return;

        float dt = Time.fixedDeltaTime;

        jumpLockTimer = Mathf.Max(0f, jumpLockTimer - dt);
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

        wallTimer = Mathf.Max(0f, wallTimer - dt);
        escapeTimer = Mathf.Max(0f, escapeTimer - dt);

        Vector3 velocity = rb.velocity;

        UpdateStuckState(velocity.z, dt);

        CheckGround(dt, velocity.y);

        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);

        isSliding = slidePressed && isGrounded;
        rb.drag = isSliding ? slideDrag : normalDrag;

        if (isStopping)
        {
            // Finish line / level fail: slow down gradually until the sledge stands still.
            appliedForwardSpeed = Mathf.MoveTowards(
                appliedForwardSpeed,
                0f,
                stopDeceleration * dt
            );
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxForwardSpeed, acceleration * dt);

            float targetSpeed = currentSpeed;
            if (isSliding)
                targetSpeed *= slideSpeedMultiplier;

            // Smooth the applied speed so starting / stopping a slide never snaps.
            appliedForwardSpeed = Mathf.MoveTowards(
                appliedForwardSpeed,
                targetSpeed,
                forwardSpeedChangeRate * dt
            );
        }

        ApplyForward(ref velocity, dt);
        ApplyHorizontal(ref velocity, dt);
        ApplyWallSlide(ref velocity);

        // Extra downward pull while falling so jumps feel snappy.
        if (!isGrounded && rb.useGravity && velocity.y < 0f)
            velocity.y += Physics.gravity.y * (fallGravityMultiplier - 1f) * dt;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && jumpLockTimer <= 0f)
        {
            // Keep any upward slope velocity, then add the jump on top.
            velocity.y = Mathf.Max(velocity.y, 0f) + jumpForce;

            isGrounded = false;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpLockTimer = jumpLockTime;
        }

        // Single velocity write per physics step.
        rb.velocity = velocity;
    }

    private Vector3 GetProbeOrigin()
    {
        return groundCheck != null
            ? groundCheck.position + Vector3.up * probeStartHeight
            : transform.position + Vector3.up * probeStartHeight;
    }

    private bool ProbeGround(Vector3 origin, float distance, out RaycastHit hit)
    {
        return Physics.SphereCast(
            origin,
            probeRadius,
            Vector3.down,
            out hit,
            distance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    // Reads the exact ground height at a Z offset in front of the sledge.
    private bool SampleHeight(float zOffset, out float height)
    {
        Vector3 basePos = groundCheck != null ? groundCheck.position : transform.position;
        Vector3 origin = new Vector3(basePos.x, basePos.y + raySampleHeight, basePos.z + zOffset);

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            raySampleHeight + groundCheckDistance + 2f,
            groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            // Ignore walls / fence sides: they are not ground and would give a fake slope.
            if (Vector3.Angle(hit.normal, Vector3.up) <= maxGroundAngle)
            {
                height = hit.point.y;
                return true;
            }
        }

        height = 0f;
        return false;
    }

    private void CheckGround(float dt, float currentVerticalVelocity)
    {
        Vector3 origin = GetProbeOrigin();

        bool foundGround = ProbeGround(
            origin,
            groundCheckDistance + probeStartHeight,
            out RaycastHit currentHit
        );

        if (!foundGround ||
            Vector3.Angle(currentHit.normal, Vector3.up) > maxGroundAngle)
        {
            isGrounded = false;
            groundGap = float.MaxValue;

            // Relax slope slowly in the air so landing doesn't snap.
            smoothedPitch = Mathf.Lerp(smoothedPitch, 0f, 1f - Mathf.Exp(-1.5f * dt));
            return;
        }

        // Real gap between the sledge bottom and the ground.
        groundGap = Mathf.Max(0f, currentHit.distance + probeRadius - probeStartHeight);

        // Slope from height samples along Z: much smoother than mesh normals,
        // and it anticipates curves (crests and dips) before reaching them.
        float targetPitch;
        float h0, h1, h2;
        float half = lookAheadDistance * 0.5f;

        if (SampleHeight(0f, out h0) &&
            SampleHeight(half, out h1) &&
            SampleHeight(lookAheadDistance, out h2))
        {
            float nearPitch = Mathf.Atan2(h1 - h0, half);
            float farPitch = Mathf.Atan2(h2 - h1, half);
            targetPitch = Mathf.Lerp(nearPitch, farPitch, lookAheadWeight);
        }
        else
        {
            // Fallback: derive pitch from the surface normal.
            targetPitch = Mathf.Atan2(-currentHit.normal.z, currentHit.normal.y);
        }

        float maxPitch = maxGroundAngle * Mathf.Deg2Rad;
        targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

        float blend = 1f - Mathf.Exp(-normalSmoothness * dt);
        smoothedPitch = Mathf.Lerp(smoothedPitch, targetPitch, blend);

        // Grounded only when close to the ground AND not launching upward
        // faster than the slope (this is what stops the jump getting cancelled).
        float slopeVelocityY = Mathf.Tan(smoothedPitch) * appliedForwardSpeed;
        bool rising = currentVerticalVelocity > slopeVelocityY + riseThreshold;

        isGrounded =
            groundGap <= groundedDistance &&
            jumpLockTimer <= 0f &&
            !rising;
    }

    private void ApplyForward(ref Vector3 velocity, float dt)
    {
        // Constant Z speed: forward motion (and the camera locked to it) never
        // changes speed because of slopes or ground flicker.
        velocity.z = appliedForwardSpeed;

        if (!isGrounded)
            return; // Airborne: gravity / jump arc controls Y.

        // Follow the slope with Y velocity, and pull toward the ground only in
        // proportion to the gap (zero pull when touching = no pressing jitter).
        float slopeVelocityY = Mathf.Tan(smoothedPitch) * appliedForwardSpeed;
        float stick = Mathf.Clamp(groundGap * groundStickGain, 0f, groundStickSpeed);

        float targetY = slopeVelocityY - stick;
        float blend = 1f - Mathf.Exp(-slopeFollowSmoothness * dt);
        velocity.y = Mathf.Lerp(velocity.y, targetY, blend);
    }

    private void ApplyHorizontal(ref Vector3 velocity, float dt)
    {
        // While escaping a wall the nudge overrides the player's input briefly.
        float targetVelocity = escapeTimer > 0f
            ? escapeDirection * escapeSpeed
            : horizontalInput * horizontalSpeed;

        currentHorizontalVelocity = Mathf.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            horizontalSmoothness * horizontalSpeed * dt
        );

        // Keep position inside the limits by limiting velocity (no teleporting).
        float posX = rb.position.x;
        float minVx = (-horizontalLimit - posX) / dt;
        float maxVx = (horizontalLimit - posX) / dt;

        float clamped = Mathf.Clamp(currentHorizontalVelocity, minVx, maxVx);

        // Keep the internal value equal to what was actually applied.
        currentHorizontalVelocity = clamped;

        velocity.x = clamped;
    }

    private void ApplySlipperyMaterial()
    {
        PhysicMaterial slippery = new PhysicMaterial("SledgeSlippery")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Minimum
        };

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger)
                col.sharedMaterial = slippery;
        }
    }

    // Detects "pushing forward but not moving" against a wall, and nudges away.
    private void UpdateStuckState(float actualForwardSpeed, float dt)
    {
        bool blocked =
            wallTimer > 0f &&
            actualForwardSpeed < appliedForwardSpeed * stuckSpeedRatio;

        if (blocked)
        {
            stuckTimer += dt;

            // Don't keep building speed while pinned.
            currentSpeed = Mathf.MoveTowards(currentSpeed, forwardSpeed, 10f * dt);

            if (stuckTimer >= stuckTime && escapeTimer <= 0f)
            {
                // Away from the wall; if it is head-on, toward the track center.
                escapeDirection = Mathf.Abs(wallNormal.x) > 0.2f
                    ? Mathf.Sign(wallNormal.x)
                    : (rb.position.x > 0f ? -1f : 1f);

                escapeTimer = escapeDuration;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = Mathf.Max(0f, stuckTimer - dt * 2f);
        }
    }

    // Removes the part of the velocity that points into a wall, so the sledge
    // slides along it instead of pressing into it (that was the stuck + shake).
    private void ApplyWallSlide(ref Vector3 velocity)
    {
        if (wallTimer <= 0f)
            return;

        float into = Vector3.Dot(velocity, wallNormal);

        if (into < 0f)
        {
            velocity -= wallNormal * into;
            currentHorizontalVelocity = velocity.x;
        }
    }

    // ---------- Obstacles ----------

    private bool IsObstacle(GameObject other)
    {
        return (obstacleLayer.value & (1 << other.layer)) != 0;
    }

    // Spawns every prefab in the list at the hit point and plays ALL particle systems
    // inside each one (root + every child, even inactive ones).
    private void SpawnHitParticles(Vector3 hitPoint)
    {
        if (obstacleHitParticles == null)
            return;

        for (int i = 0; i < obstacleHitParticles.Length; i++)
        {
            GameObject prefab = obstacleHitParticles[i];
            if (prefab == null)
                continue;

            // Not parented to the sledge, so hiding the sledge can't affect it.
            GameObject fx = Instantiate(prefab, hitPoint, prefab.transform.rotation);
            fx.SetActive(true);

            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int s = 0; s < systems.Length; s++)
            {
                systems[s].gameObject.SetActive(true);
                systems[s].Play(false); // each system is played individually
            }

            Destroy(fx, particleLifetime);
        }
    }

    private void HitObstacle(GameObject obstacle, Vector3 hitPoint)
    {
        // Level already ended (finished or failed): nothing more to do.
        if (hasFailed)
            return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsFinished)
            return;

        hasFailed = true;

        // Disable the obstacle so it can't be hit twice.
        obstacle.SetActive(false);

        // 1) Effects first, exactly at the contact point.
        SpawnHitParticles(hitPoint);

        // 2) Sound (the sledge GameObject stays active, so the clip is never cut off).
        if (obstacleHitSound != null && audioSource != null)
            audioSource.PlayOneShot(obstacleHitSound, obstacleHitVolume);

        onObstacleHit?.Invoke();

        // 3) Take control away and hide / stop the sledge.
        canControl = false;

        if (hideSledgeOnHit)
            HideSledge();

        float panelDelay;

        if (freezeOnHit)
        {
            FreezeSledge();
            panelDelay = failPanelDelay;
        }
        else
        {
            StopSmoothly(failStopDuration);
            panelDelay = failStopDuration;
        }

        // 4) Show the Level Fail panel.
        if (gm != null)
            gm.OnPlayerFailed(panelDelay);
    }

    // Turns off every mesh of the sledge (parent + all children) and its colliders.
    // The GameObject stays active so scripts, audio and the effect keep running.
    private void HideSledge()
    {
        for (int i = 0; i < sledgeRenderers.Length; i++)
        {
            if (sledgeRenderers[i] != null)
                sledgeRenderers[i].enabled = false;
        }

        for (int i = 0; i < sledgeColliders.Length; i++)
        {
            if (sledgeColliders[i] != null)
                sledgeColliders[i].enabled = false;
        }
    }

    // Stops the sledge exactly where it crashed.
    private void FreezeSledge()
    {
        isFrozen = true;
        isStopping = true;
        appliedForwardSpeed = 0f;
        currentHorizontalVelocity = 0f;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    // Solid obstacle (non-trigger collider).
    private void OnCollisionEnter(Collision collision)
    {
        if (hasFailed)
            return;

        GameObject other = collision.collider.gameObject;

        if (IsObstacle(other))
        {
            // Exact point where the sledge touched the obstacle.
            Vector3 point = collision.contactCount > 0
                ? collision.GetContact(0).point
                : other.transform.position;

            HitObstacle(other, point);
            return; // Obstacles are not walls: skip the wall logic.
        }

        CollectWallContacts(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (hasFailed)
            return;

        if (IsObstacle(collision.collider.gameObject))
            return;

        CollectWallContacts(collision);
    }

    // Trigger obstacle (collider with "Is Trigger" ticked).
    private void OnTriggerEnter(Collider other)
    {
        if (hasFailed || !IsObstacle(other.gameObject))
            return;

        // Closest point on the obstacle to the sledge = the contact point.
        Vector3 point = other.ClosestPoint(transform.position);
        HitObstacle(other.gameObject, point);
    }

    private void CollectWallContacts(Collision collision)
    {
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;

            // Steep surface = wall / fence / bank (not the ground we ride on).
            if (n.y < 0.55f)
            {
                n.y = 0f;
                sum += n;
            }
        }

        if (sum.sqrMagnitude > 0.0001f)
        {
            wallNormal = sum.normalized;
            wallTimer = wallMemoryTime;
        }
    }

    private void UpdateVisual()
    {
        if (!firstRig.IsValid && !secondRig.IsValid)
            return;

        float normalizedVelocity = horizontalSpeed > 0.001f
            ? currentHorizontalVelocity / horizontalSpeed
            : 0f;

        float targetTilt = -normalizedVelocity * maxTiltAngle;

        currentTilt = Mathf.Lerp(
            currentTilt,
            targetTilt,
            1f - Mathf.Exp(-tiltSmoothness * Time.deltaTime)
        );

        float targetPitchDeg = pitchVisualToSlope
            ? -smoothedPitch * Mathf.Rad2Deg
            : 0f;

        visualPitchDeg = Mathf.Lerp(
            visualPitchDeg,
            targetPitchDeg,
            1f - Mathf.Exp(-visualPitchSmoothness * Time.deltaTime)
        );

        bool rotateRoot = tiltTarget != VisualTiltTarget.ChildrenOnly;
        bool rotateChildren = tiltTarget != VisualTiltTarget.RootOnly;

        Quaternion rootOffset = Quaternion.Euler(visualPitchDeg, 0f, currentTilt);

        Quaternion childOffset = Quaternion.Euler(
            visualPitchDeg * childTiltAmount,
            0f,
            currentTilt * childTiltAmount
        );

        // Both visual roots receive exactly the same tilt.
        firstRig.Apply(rootOffset, childOffset, rotateRoot, rotateChildren);
        secondRig.Apply(rootOffset, childOffset, rotateRoot, rotateChildren);
    }

    // ---------- Public control API ----------

    public bool CanControl
    {
        get => canControl;
        set => canControl = value;
    }

    public void SetControl(bool enabled)
    {
        canControl = enabled;
    }

    // Takes control away and slows the sledge to a stop over "duration" seconds.
    public void StopSmoothly(float duration)
    {
        canControl = false;
        isStopping = true;
        stopDeceleration = appliedForwardSpeed / Mathf.Max(0.05f, duration);
    }

    // Gives control back and lets the sledge accelerate again.
    public void ResumeMovement()
    {
        isStopping = false;
        canControl = true;
        currentSpeed = Mathf.Max(currentSpeed, forwardSpeed);
    }

    public bool HasStopped() => isStopping && appliedForwardSpeed <= 0.05f;

    public bool IsGrounded() => isGrounded;
    public bool IsSliding() => isSliding;
    public bool HasFailed() => hasFailed;
    public float GetCurrentSpeed() => currentSpeed;

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = GetProbeOrigin();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            origin + Vector3.down * (groundCheckDistance + probeStartHeight),
            probeRadius
        );

        Gizmos.color = Color.cyan;
        Vector3 basePos = groundCheck != null ? groundCheck.position : transform.position;
        for (int i = 0; i <= 2; i++)
        {
            float z = lookAheadDistance * 0.5f * i;
            Vector3 top = new Vector3(basePos.x, basePos.y + raySampleHeight, basePos.z + z);
            Gizmos.DrawLine(top, top + Vector3.down * (raySampleHeight + groundCheckDistance + 2f));
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(basePos, basePos + Vector3.down * groundedDistance);
    }
}