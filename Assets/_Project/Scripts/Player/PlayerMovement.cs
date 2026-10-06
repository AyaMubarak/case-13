using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4.5f;
    public float rotationSpeed = 14f;

    [Header("Collision Settings")]
    public float collisionSkin = 0.03f;

    [Tooltip("Layers that the player cannot pass through.")]
    public LayerMask collisionLayers = ~0;

    [Header("Mobile References")]
    public MobileJoystick mobileJoystick;

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Camera cam;

    private Vector2 moveInput;

    private void Awake()
    {
        // Get Rigidbody safely
        if (!TryGetComponent<Rigidbody>(out rb))
        {
            Debug.LogError("PlayerMovement: Rigidbody is missing!");
            enabled = false;
            return;
        }

        // Get Capsule Collider safely
        if (!TryGetComponent<CapsuleCollider>(out playerCollider))
        {
            Debug.LogError("PlayerMovement: Capsule Collider is missing!");
            enabled = false;
            return;
        }

        // Camera
        cam = Camera.main;

        if (cam == null)
        {
            cam = FindAnyObjectByType<Camera>();
        }

        // Rigidbody settings
        rb.freezeRotation = true;

        // Better collision detection for moving player
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Player collider must physically collide
        playerCollider.isTrigger = false;
        playerCollider.enabled = true;
    }

    private void Update()
    {
        // ==========================================
        // MOBILE JOYSTICK
        // ==========================================

        if (mobileJoystick != null &&
            mobileJoystick.Input.magnitude > 0.05f)
        {
            moveInput = mobileJoystick.Input;
            return;
        }

        // ==========================================
        // KEYBOARD
        // ==========================================

        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
            {
                y += 1f;
            }

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
            {
                y -= 1f;
            }

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            moveInput = new Vector2(x, y).normalized;
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || playerCollider == null)
            return;

        if (moveInput.sqrMagnitude < 0.001f)
            return;

        // ==========================================
        // CAMERA RELATIVE MOVEMENT
        // ==========================================

        Vector3 camFwd = cam != null
            ? cam.transform.forward
            : Vector3.forward;

        Vector3 camRight = cam != null
            ? cam.transform.right
            : Vector3.right;

        camFwd.y = 0f;
        camRight.y = 0f;

        camFwd.Normalize();
        camRight.Normalize();

        Vector3 desiredDir =
            (camFwd * moveInput.y +
             camRight * moveInput.x).normalized;

        if (desiredDir.sqrMagnitude < 0.001f)
            return;

        float wantedDistance =
            moveSpeed * Time.fixedDeltaTime;

        // ==========================================
        // CHECK COLLISION BEFORE MOVING
        // ==========================================

        float allowedDistance =
            GetAllowedMovementDistance(
                desiredDir,
                wantedDistance
            );

        // ==========================================
        // MOVE ONLY THE ALLOWED DISTANCE
        // ==========================================

        if (allowedDistance > 0f)
        {
            Vector3 nextPosition =
                rb.position +
                desiredDir * allowedDistance;

            rb.MovePosition(nextPosition);
        }

        // ==========================================
        // ROTATE PLAYER
        // ==========================================

        Quaternion targetRotation =
            Quaternion.LookRotation(
                desiredDir,
                Vector3.up
            );

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            )
        );
    }

    // ============================================================
    // CHECK COLLISION BEFORE PLAYER MOVES
    // ============================================================

    private float GetAllowedMovementDistance(
        Vector3 direction,
        float wantedDistance)
    {
        // Player collider center in world space
        Vector3 center =
            transform.TransformPoint(
                playerCollider.center
            );

        // Calculate world-space radius
        float radius =
            playerCollider.radius *
            Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.z
            );

        // Calculate world-space height
        float height =
            playerCollider.height *
            transform.lossyScale.y;

        // Make sure height is at least diameter
        height =
            Mathf.Max(
                height,
                radius * 2f
            );

        float halfHeight =
            (height * 0.5f) - radius;

        // Top and bottom of player's capsule
        Vector3 point1 =
            center +
            transform.up * halfHeight;

        Vector3 point2 =
            center -
            transform.up * halfHeight;

        // ==========================================
        // CAPSULE CAST
        // ==========================================

        if (Physics.CapsuleCast(
            point1,
            point2,
            radius,
            direction,
            out RaycastHit hit,
            wantedDistance + collisionSkin,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            // Ignore player's own collider
            if (hit.collider.transform.root != transform.root)
            {
                float safeDistance =
                    hit.distance - collisionSkin;

                return Mathf.Max(
                    0f,
                    safeDistance
                );
            }
        }

        // Nothing in front of player
        return wantedDistance;
    }
}