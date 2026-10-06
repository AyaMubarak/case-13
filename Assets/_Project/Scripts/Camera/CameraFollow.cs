using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position (FPS)")]
    public Vector3 cameraOffset = new Vector3(0f, 1.6f, 0.15f); // أمام الوجه قليلاً لتجنب رؤية الرأس من الداخل
    public float followSpeed = 30f;

    [Header("PC Mouse")]
    [Range(0.01f, 0.3f)]
    public float mouseSensitivity = 0.08f;

    [Header("Mobile Touch")]
    public float touchSensitivity = 0.12f;

    [Header("Vertical Limits")]
    public float minPitch = -60f;
    public float maxPitch = 60f;

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
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // نقطة الرأس بناءً على الـ offset (منظور الشخص الأول)
        Vector3 desiredPosition = target.position + target.rotation * new Vector3(cameraOffset.x, 0, 0) + Vector3.up * cameraOffset.y + rotation * new Vector3(0, 0, cameraOffset.z);

        // تطبيق الدوران والمكان فوراً (لا نستخدم Lerp بطيء في الـ FPS لتجنب الدوار)
        transform.rotation = rotation;
        transform.position = desiredPosition;
        
        // إجبار اللاعب على الدوران ليتطابق مع اتجاه نظر الكاميرا يميناً ويساراً (لكي يراه الآخرون بشكل صحيح)
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}