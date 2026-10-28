using UnityEngine;

public class SledgeCameraFollow : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Header("Look At")]
    [Tooltip("Camera always aims at the player (also while jumping).")]
    [SerializeField] private bool lookAtPlayer = true;
    [Tooltip("Point on / above the player the camera aims at.")]
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 2f);
    [Tooltip("Rotation follow speed. Higher = tighter. 0 = instant.")]
    [SerializeField] private float rotationSmoothness = 25f;

    [Header("Follow Settings")]
    [SerializeField] private bool useSmoothFollow = true;

    [Header("Horizontal (X)")]
    [Tooltip("1 = camera follows the sledge sideways fully, 0 = camera stays centered.")]
    [Range(0f, 1f)]
    [SerializeField] private float xFollowAmount = 1f;
    [Tooltip("0 = locked to the sledge (zero lag). Small values (0.03 - 0.08) add a touch of softness.")]
    [SerializeField] private float xSmoothTime = 0f;

    [Header("Vertical (Y)")]
    [Tooltip("Smoothing over small bumps. Keep low so jumps are not laggy.")]
    [SerializeField] private float ySmoothTime = 0.08f;
    [Tooltip("The camera is never allowed to fall further than this behind the player vertically. Keeps the player in frame while jumping.")]
    [SerializeField] private float maxVerticalLag = 0.6f;

    private Transform player;

    private float smoothX;
    private float smoothY;
    private float velX;
    private float velY;
    private bool initialized;

    private void Start()
    {
        FindPlayer();
        SnapToPlayer();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
            SnapToPlayer();
        }

        FollowPlayer();
        UpdateRotation();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
            player = playerObject.transform;
    }

    private void FollowPlayer()
    {
        // The player's Rigidbody is interpolated, so its transform is already
        // smooth in LateUpdate. Z and X are locked to it (no lag), Y is only
        // lightly smoothed and can never lag far behind during jumps.
        Vector3 playerPos = player.position;

        float targetX = offset.x + playerPos.x * xFollowAmount;
        float targetY = playerPos.y + offset.y;
        float targetZ = playerPos.z + offset.z;

        if (!useSmoothFollow || !initialized)
        {
            smoothX = targetX;
            smoothY = targetY;
            velX = 0f;
            velY = 0f;
            initialized = true;
        }
        else
        {
            if (xSmoothTime > 0.0001f)
                smoothX = Mathf.SmoothDamp(smoothX, targetX, ref velX, xSmoothTime);
            else
                smoothX = targetX;

            if (ySmoothTime > 0.0001f)
                smoothY = Mathf.SmoothDamp(smoothY, targetY, ref velY, ySmoothTime);
            else
                smoothY = targetY;

            // Hard limit on vertical lag so the player never leaves the frame.
            float minY = targetY - maxVerticalLag;
            float maxY = targetY + maxVerticalLag;

            if (smoothY < minY || smoothY > maxY)
            {
                smoothY = Mathf.Clamp(smoothY, minY, maxY);
                velY = 0f;
            }
        }

        transform.position = new Vector3(smoothX, smoothY, targetZ);
    }

    private void UpdateRotation()
    {
        if (!lookAtPlayer)
            return;

        Vector3 lookTarget = player.position + lookAtOffset;
        Vector3 direction = lookTarget - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (rotationSmoothness <= 0.0001f)
        {
            transform.rotation = targetRotation;
            return;
        }

        float blend = 1f - Mathf.Exp(-rotationSmoothness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
    }

    public void SnapToPlayer()
    {
        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        Vector3 p = player.position;
        smoothX = offset.x + p.x * xFollowAmount;
        smoothY = p.y + offset.y;
        velX = 0f;
        velY = 0f;
        initialized = true;
        transform.position = new Vector3(smoothX, smoothY, p.z + offset.z);

        if (lookAtPlayer)
        {
            Vector3 dir = (p + lookAtOffset) - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}