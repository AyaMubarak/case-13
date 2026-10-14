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

        // ضبط Near Clip Plane تلقائياً إلى مسافة صغيرة جداً حتى لا تقص الكاميرا الجدران أو الأجسام القريبة
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.nearClipPlane = 0.05f;
        }

        // لا نقفل الماوس تلقائياً هنا، بل نتركه ظاهراً لشاشة اختيار الشخصية
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (CaseIntroCutscene.IsPlaying)
            return;

        HandleCameraInput();
        UpdateCamera();
    }

    private void HandleCameraInput()
    {
        // ==========================================
        // PC - Mouse
        // ==========================================

        bool invertY = PlayerPrefs.GetInt("Settings_InvertY", 0) == 1;
        float yMultiplier = invertY ? 1f : -1f;

        if (Mouse.current != null)
        {
            // إذا في Touch على الموبايل لا نستخدم الماوس
            if (Touchscreen.current == null)
            {
                Vector2 mouseDelta =
                    Mouse.current.delta.ReadValue();

                yaw += mouseDelta.x * mouseSensitivity;
                pitch += mouseDelta.y * mouseSensitivity * yMultiplier;
            }
        }

        // ==========================================
        // MOBILE - Touch
        // ==========================================

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.isPressed &&
                !(UnityEngine.EventSystems.EventSystem.current != null &&
                  UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue())))
            {
                Vector2 delta =
                    touch.delta.ReadValue();

                // تجاهل اللمسة الصغيرة
                if (delta.sqrMagnitude > 0.01f)
                {
                    yaw += delta.x * touchSensitivity;
                    pitch += delta.y * touchSensitivity * yMultiplier;
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

        // إذا ضغطت بالماوس داخل اللعبة يرجع القفل بشرط ألا يكون المؤشر فوق واجهة مستخدم (UI)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void UpdateCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // نقطة منشأ الرأس الأساسية
        Vector3 headOrigin = target.position + target.rotation * new Vector3(cameraOffset.x, 0, 0) + Vector3.up * cameraOffset.y;
        Vector3 forwardOffset = rotation * new Vector3(0, 0, cameraOffset.z);
        Vector3 desiredPosition = headOrigin + forwardOffset;

        // التحقق من الاصطدام بالجدران والعوائق لمنع الكاميرا من اختراق أي مجسم
        if (obstacleLayers == 0)
        {
            // استثناء طبقة اللاعب (Player) وطبقة الـ UI والـ Ignore Raycast تلقائياً
            obstacleLayers = ~LayerMask.GetMask("Ignore Raycast", "UI");
        }

        Vector3 direction = desiredPosition - headOrigin;
        float distance = direction.magnitude;

        if (distance > 0.001f)
        {
            float checkRadius = 0.12f;
            if (Physics.SphereCast(headOrigin, checkRadius, direction.normalized, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                // إذا وجدنا عائقاً أو جداراً، نوقف الكاميرا قبله مباشرة
                float safeDistance = Mathf.Max(0.01f, hit.distance - 0.05f);
                desiredPosition = headOrigin + direction.normalized * safeDistance;
            }
        }

        // فحص إضافي: إذا كان الرأس نفسه قريباً جداً من جدار أمامه
        if (Physics.SphereCast(desiredPosition, 0.15f, rotation * Vector3.forward, out RaycastHit forwardHit, 0.2f, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            desiredPosition -= (rotation * Vector3.forward) * (0.2f - forwardHit.distance);
        }

        // تطبيق الدوران والمكان فوراً
        transform.rotation = rotation;
        transform.position = desiredPosition;
        
        // إجبار اللاعب على الدوران ليتطابق مع اتجاه نظر الكاميرا يميناً ويساراً
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}