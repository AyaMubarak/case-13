using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, -2.8f);
    public float followSpeed = 15f;

    [Header("PC Mouse")]
    [Range(0.01f, 0.3f)]
    public float mouseSensitivity = 0.08f;

    [Header("Mobile Touch")]
    public float touchSensitivity = 0.12f;

    [Header("Vertical Limits")]
    public float minPitch = -10f;
    public float maxPitch = 35f;

    [Header("Camera Collision")]
    public LayerMask obstacleLayers;
    public float collisionOffset = 0.25f;
    public float playerClearance = 0.5f;

    private float yaw;
    private float pitch = 10f;

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Player not found!");
            return;
        }

        yaw = target.eulerAngles.y;
        pitch = 10f;

        // لا نقفل الماوس تلقائياً هنا، بل نتركه ظاهراً لشاشة اختيار الشخصية
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleCameraInput();
        UpdateCamera();
    }

    private void HandleCameraInput()
    {
        // ==========================================
        // PC - Mouse
        // ==========================================

        if (Mouse.current != null)
        {
            // إذا في Touch على الموبايل لا نستخدم الماوس
            if (Touchscreen.current == null)
            {
                Vector2 mouseDelta =
                    Mouse.current.delta.ReadValue();

                yaw += mouseDelta.x * mouseSensitivity;
                pitch -= mouseDelta.y * mouseSensitivity;
            }
        }

        // ==========================================
        // MOBILE - Touch
        // ==========================================

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.isPressed)
            {
                Vector2 delta =
                    touch.delta.ReadValue();

                // تجاهل اللمسة الصغيرة
                if (delta.sqrMagnitude > 0.01f)
                {
                    yaw += delta.x * touchSensitivity;
                    pitch -= delta.y * touchSensitivity;
                }
            }
        }

        // منع الكاميرا من النظر للأرض/السماء بشكل مبالغ
        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        // ESC على الكمبيوتر
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // إذا ضغطت بالماوس داخل اللعبة يرجع القفل
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateCamera()
    {
        Quaternion rotation =
            Quaternion.Euler(pitch, yaw, 0f);

        // نقطة دوران الكاميرا
        Vector3 pivotPoint =
            target.position + Vector3.up * 1.3f;

        // المكان الطبيعي خلف اللاعب
        Vector3 desiredPosition =
            pivotPoint +
            rotation *
            new Vector3(
                cameraOffset.x,
                cameraOffset.y - 1.3f,
                cameraOffset.z
            );

        // ==========================================
        // Camera Collision
        // ==========================================

        Vector3 direction =
            desiredPosition - pivotPoint;

        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            direction.Normalize();

            Vector3 rayStart =
                pivotPoint +
                direction * playerClearance;

            float rayDistance =
                Mathf.Max(
                    0f,
                    distance - playerClearance
                );

            if (Physics.SphereCast(
                rayStart,
                0.25f,
                direction,
                out RaycastHit hit,
                rayDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(target))
                {
                    desiredPosition =
                        hit.point +
                        hit.normal *
                        collisionOffset;
                }
            }
        }

        // ==========================================
        // Apply
        // ==========================================

        transform.rotation = rotation;

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );
    }
}