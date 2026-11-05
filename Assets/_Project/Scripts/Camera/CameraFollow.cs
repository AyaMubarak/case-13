using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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

    // تتبع إصبع الكاميرا في النصف الأيمن من الشاشة فقط
    private int cameraTouchId = -1;
    private Vector2 lastTouchPosition;

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Snap camera immediately to player head so it never renders skybox at (0,0,0)
        UpdateCamera();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (CaseIntroCutscene.IsPlaying)
        {
            // Keep camera snapped to player head behind the video so it never lags or peeks at the skybox
            UpdateCamera();
            return;
        }

        HandleCameraInput();
        UpdateCamera();
    }

    private void HandleCameraInput()
    {
        bool invertY = PlayerPrefs.GetInt("Settings_InvertY", 0) == 1;
        float yMultiplier = invertY ? 1f : -1f;

        // ==========================================
        // PC - Mouse
        // ==========================================
        if (Mouse.current != null)
        {
            if (Touchscreen.current == null || !Touchscreen.current.wasUpdatedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    yaw += mouseDelta.x * mouseSensitivity;
                    pitch += mouseDelta.y * mouseSensitivity * yMultiplier;
                }
            }
        }

        // ==========================================
        // MOBILE - Touch (Right Half of Screen Only)
        // ==========================================
        HandleMobileTouchLook(yMultiplier);

        // منع الكاميرا من النظر للأرض/السماء بشكل مبالغ
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ESC على الكمبيوتر
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

        // قفل الماوس عند الضغط بالماوس داخل اللعبة (إذا لم يكن فوق UI)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleMobileTouchLook(float yMultiplier)
    {
        float halfScreenWidth = Screen.width * 0.5f;

        // دعم New Input System (Touchscreen)
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            bool foundActiveFinger = false;

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed)
                    continue;

                int fingerId = touch.touchId.ReadValue();
                Vector2 currentPos = touch.position.ReadValue();

                // إذا كنا نبحث عن إصبع جديد للكاميرا
                if (cameraTouchId == -1)
                {
                    // يجب أن يكون في النصف الأيمن من الشاشة فقط
                    if (currentPos.x >= halfScreenWidth)
                    {
                        // استثناء الشريط العلوي الأيمن المخصص لأزرار الواجهة (الحقيبة، ملف المتهمين، الإعدادات)
                        bool isTopRightHudZone = currentPos.x > Screen.width * 0.70f && currentPos.y > Screen.height * 0.80f;
                        if (isTopRightHudZone)
                        {
                            continue; // لا تحرك الكاميرا واترك اللمسة لأزرار الـ UI
                        }

                        // التأكد من أنه ليس فوق عنصر UI
                        if (!(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId)))
                        {
                            cameraTouchId = fingerId;
                            lastTouchPosition = currentPos;
                            foundActiveFinger = true;
                        }
                    }
                }
                else if (cameraTouchId == fingerId)
                {
                    // هذا هو الإصبع المخصص للكاميرا في النصف الأيمن
                    foundActiveFinger = true;
                    Vector2 delta = currentPos - lastTouchPosition;
                    lastTouchPosition = currentPos;

                    if (delta.sqrMagnitude > 0.01f)
                    {
                        yaw += delta.x * touchSensitivity;
                        pitch += delta.y * touchSensitivity * yMultiplier;
                    }
                    break;
                }
            }

            if (!foundActiveFinger)
            {
                cameraTouchId = -1;
            }
        }
        else
        {
            // Fallback في حال استخدام Input الكلاسيكي
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (cameraTouchId == -1)
                {
                    if (touch.phase == UnityEngine.TouchPhase.Began && touch.position.x >= halfScreenWidth)
                    {
                        bool isTopRightHudZone = touch.position.x > Screen.width * 0.70f && touch.position.y > Screen.height * 0.80f;
                        if (isTopRightHudZone)
                        {
                            continue;
                        }

                        if (!(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                        {
                            cameraTouchId = touch.fingerId;
                            lastTouchPosition = touch.position;
                        }
                    }
                }
                else if (touch.fingerId == cameraTouchId)
                {
                    if (touch.phase == UnityEngine.TouchPhase.Moved)
                    {
                        Vector2 delta = touch.deltaPosition;
                        yaw += delta.x * touchSensitivity;
                        pitch += delta.y * touchSensitivity * yMultiplier;
                    }
                    else if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                    {
                        cameraTouchId = -1;
                    }
                }
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
            obstacleLayers = ~LayerMask.GetMask("Ignore Raycast", "UI");
        }

        Vector3 direction = desiredPosition - headOrigin;
        float distance = direction.magnitude;

        if (distance > 0.001f)
        {
            float checkRadius = 0.12f;
            if (Physics.SphereCast(headOrigin, checkRadius, direction.normalized, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
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
