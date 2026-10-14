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

    [Header("Local Test (تيست محلي)")]
    [Tooltip("ON: moves directly every frame with collision checks. Guaranteed to move.")]
    public bool forceSimpleMove = true;

    [Header("Ground Snap (تثبيت على الأرض)")]
    public bool snapToGround = true;
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayers = ~0;

    private void Update()
    {
        if (CaseIntroCutscene.IsPlaying || EvidenceCutsceneManager.IsQuizOrVideoActive)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadInput();

        if (forceSimpleMove && moveInput.sqrMagnitude >= 0.001f)
        {
            Vector3 fwd = cam != null ? cam.transform.forward : transform.forward;
            Vector3 right = cam != null ? cam.transform.right : transform.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();

            Vector3 dir = (fwd * moveInput.y + right * moveInput.x).normalized;
            float wantedDistance = moveSpeed * Time.deltaTime;

            // التحقق من الاصطدام مسبقاً لمنع صعود اللاعب على الطاولات والمجسمات أو اختراقها
            float allowedDistance = GetAllowedMovementDistance(dir, wantedDistance);

            if (allowedDistance > 0.0001f)
            {
                Vector3 delta = dir * allowedDistance;
                if (rb != null) rb.position += delta;
                transform.position += delta;
            }
        }

        // تثبيت اللاعب على الأرض مباشرة لمنع الطفو، مع منع تسلقه فوق الأجسام المرتفعة
        SnapPlayerToFloor();
    }

    private void SnapPlayerToFloor()
    {
        if (!snapToGround || playerCollider == null) return;

        // منشأ الفحص من منتصف اللاعب متجهاً للأسفل
        Vector3 origin = transform.position + Vector3.up * (playerCollider.height * 0.5f);
        float castDistance = (playerCollider.height * 0.5f) + groundCheckDistance;

        int mask = groundLayers == 0 ? ~LayerMask.GetMask("Ignore Raycast", "UI") : (int)groundLayers;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance, mask, QueryTriggerInteraction.Ignore))
        {
            // تجنب الاصطدام بنفس اللاعب
            if (hit.collider.transform.root != transform.root)
            {
                float targetY = hit.point.y;
                float diff = targetY - transform.position.y;

                // أقصى ارتفاع مسموح للصعود هو عتبة درجة صغيرة جداً (15 سم)
                // إذا كان الارتفاع أعلى من ذلك (مثل طاولة أو كرسي أو مكتب)، يمنع الصعود فوقها تماماً
                float maxStepHeight = 0.15f;
                if (diff > maxStepHeight)
                {
                    return; // جسم مرتفع (طاولة/مكتب/عائق)، لا نصعد فوقه!
                }

                // إذا كان نزولاً أو استقراراً على الأرضية
                if (Mathf.Abs(diff) > 0.01f)
                {
                    Vector3 groundedPos = transform.position;
                    groundedPos.y = targetY;
                    transform.position = groundedPos;
                    if (rb != null) rb.position = groundedPos;
                }
            }
        }
    }

    private void ReadInput()
    {
        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        }

        if (Keyboard.current == null && mobileJoystick == null)
        {
            Debug.LogWarning("PlayerMovement: Keyboard.current is null. Set Project Settings > Player > Active Input Handling to 'Input System Package' or 'Both'.");
        }

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

#if ENABLE_LEGACY_INPUT_MANAGER
            float lx = UnityEngine.Input.GetAxisRaw("Horizontal");
            float ly = UnityEngine.Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(lx, ly).normalized;
#endif
        }
    }

    private void FixedUpdate()
    {
        if (forceSimpleMove || rb == null || playerCollider == null)
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
        // ROTATION IS HANDLED BY CAMERA IN FPS
        // ==========================================
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