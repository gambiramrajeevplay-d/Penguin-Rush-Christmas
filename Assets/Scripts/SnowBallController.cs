
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class SnowBallController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Rolling")]
    [SerializeField] private bool rollWhileMoving = true;

    private Rigidbody rb;
    private SphereCollider sphereCollider;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private float WorldRadius
    {
        get
        {
            Vector3 scale = transform.lossyScale;

            return sphereCollider.radius * Mathf.Max(
                scale.x,
                Mathf.Max(scale.y, scale.z)
            );
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Allow rotation so the snowball can roll naturally.
        rb.constraints &= ~RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        CheckGround();

        // Always move along negative Z.
        Vector3 velocity = rb.velocity;
        velocity.z = -moveSpeed;
        rb.velocity = velocity;

        if (isGrounded && rollWhileMoving)
        {
            // Correct rolling axis for movement along -Z.
            float angularSpeed = moveSpeed / Mathf.Max(WorldRadius, 0.01f);

            Vector3 rollingAxis = Vector3.Cross(
                Vector3.back,
                groundNormal
            ).normalized;

            rb.angularVelocity = rollingAxis * angularSpeed;
        }
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;

        isGrounded = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            WorldRadius + groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded)
            groundNormal = hit.normal;
        else
            groundNormal = Vector3.up;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}
