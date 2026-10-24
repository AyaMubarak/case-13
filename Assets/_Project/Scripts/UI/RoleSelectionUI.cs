using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class RoleSelectionUI : MonoBehaviour
{
    [Header("UI Canvas / Panel")]
    public GameObject selectionPanel;

    [Header("Input Fields")]
    public TMP_InputField codenameInput;

    [Header("Gender Buttons")]
    public Button maleBtn;
    public Button femaleBtn;

    [Header("Role Buttons")]
    public Button fieldDetectiveBtn;
    public Button forensicsBtn;
    public Button digitalAnalystBtn;
    public Button interrogatorBtn;

    [Header("HUD Badge (Optional)")]
    public TextMeshProUGUI hudRoleBadgeText;

    [Header("Random Spawn Points (أماكن البداية العشوائية)")]
    public Transform[] randomSpawnPoints;

    private DetectiveGender selectedGender = DetectiveGender.Female;
    private DetectiveRole selectedRole = DetectiveRole.ForensicsExpert;

    [Header("Auto Deploy (اللوبي / تيست محلي)")]
    [Tooltip("ON: skip this panel; deploy the player immediately using the lobby role (or PlayerProfile test override).")]
    public bool autoDeployFromLobby = true;

    private void Start()
    {
        ConfigureTabletPresentation();

        if (autoDeployFromLobby)
        {
            PlayerProfile p = PlayerProfile.LocalPlayer;
            if (p == null) p = FindAnyObjectByType<PlayerProfile>(FindObjectsInactive.Include);

            if (p != null)
            {
                if (!p.useLocalTestOverride) p.LoadFromLobby();
                selectedGender = p.gender;
                Debug.Log($"[RoleSelectionUI] Auto deploy: {p.currentRole} / {p.gender}");
                SelectRoleAndDeploy(p.currentRole);
                return;
            }
            Debug.LogWarning("[RoleSelectionUI] PlayerProfile not found, showing manual panel.");
        }

        // إظهار اللوحة والماوس في بداية المشهد
        OpenPanel();

        // ربط أزرار الجنس
        if (maleBtn != null) maleBtn.onClick.AddListener(() => SetGender(DetectiveGender.Male));
        if (femaleBtn != null) femaleBtn.onClick.AddListener(() => SetGender(DetectiveGender.Female));

        // ربط أزرار الأدوار
        if (fieldDetectiveBtn != null) fieldDetectiveBtn.onClick.AddListener(() => SelectRoleAndDeploy(DetectiveRole.FieldDetective));
        if (forensicsBtn != null) forensicsBtn.onClick.AddListener(() => SelectRoleAndDeploy(DetectiveRole.ForensicsExpert));
        if (digitalAnalystBtn != null) digitalAnalystBtn.onClick.AddListener(() => SelectRoleAndDeploy(DetectiveRole.DigitalAnalyst));
        if (interrogatorBtn != null) interrogatorBtn.onClick.AddListener(() => SelectRoleAndDeploy(DetectiveRole.Interrogator));

        // إبراز الاختيار الافتراضي الأولي
        HighlightGenderButtons();
    }

    public void OpenPanel()
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        else gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // إخفاء واجهة اللعب وأزرار الموبايل أثناء اختيار الشخصية
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(false);
            if (UIManager.Instance.inGameHUD != null) UIManager.Instance.inGameHUD.SetActive(false);
        }
    }

    public void SetGender(DetectiveGender gender)
    {
        selectedGender = gender;
        HighlightGenderButtons();
        Debug.Log($"[IDENTITY] Gender selected: {gender}");
    }

    private void HighlightGenderButtons()
    {
        if (femaleBtn != null) femaleBtn.transform.localScale = (selectedGender == DetectiveGender.Female) ? Vector3.one * 1.15f : Vector3.one;
        if (maleBtn != null) maleBtn.transform.localScale = (selectedGender == DetectiveGender.Male) ? Vector3.one * 1.15f : Vector3.one;
    }

    public static event System.Action OnPlayerSetupComplete;

    public void SelectRoleAndDeploy(DetectiveRole role)
    {
        selectedRole = role;

        OnPlayerSetupComplete?.Invoke();

        // 1. البحث عن اللاعب (حتى لو كان معطلاً في بداية المشهد) وتطبيق الدور والجنس عليه
        PlayerProfile profile = PlayerProfile.LocalPlayer;
        if (profile == null)
        {
            profile = FindAnyObjectByType<PlayerProfile>(FindObjectsInactive.Include);
        }

        if (profile != null)
        {
            profile.currentRole = selectedRole;
            profile.gender = selectedGender;

            if (codenameInput != null && !string.IsNullOrEmpty(codenameInput.text))
            {
                profile.agentCodename = codenameInput.text;
            }

            // نقل اللاعب إلى مكانه المخصص حسب الدور قبل تفعيله
            Transform targetSpawn = GetSpawnPointForRole(selectedRole);
            if (TryGetSafeSpawnPosition(profile, targetSpawn, out Vector3 spawnPosition))
            {
                CharacterController cc = profile.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                profile.transform.position = spawnPosition;
                if (targetSpawn != null) profile.transform.rotation = targetSpawn.rotation;

                if (cc != null) cc.enabled = true;
            }

            // تفعيل كائن اللاعب وإظهاره فوراً في المشهد
            profile.gameObject.SetActive(true);

            // 2. تحديث الموديل البصري فوراً داخل مركز الشرطة
            CharacterVisualSelector visualSelector = profile.GetComponent<CharacterVisualSelector>();
            if (visualSelector != null)
            {
                visualSelector.ApplySelectedModel();
            }
        }

        if (hudRoleBadgeText != null)
        {
            hudRoleBadgeText.text = selectedRole.ToString().ToUpper();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"DEPLOYED AS // {selectedRole.ToString().ToUpper()}");
            
            // إظهار واجهة اللعب وأزرار الموبايل بعد الانتهاء من الاختيار
            UIManager.Instance.SetMobileControlsActive(true);
            if (UIManager.Instance.inGameHUD != null) UIManager.Instance.inGameHUD.SetActive(true);
        }

        // 3. إخفاء لوحة اختيار الدور فقط، مع إبقاء الـ Canvas الرئيسي مفعلاً لظهور عناصر الـ HUD ورسالة [E]
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        // لا نعطّل هذا الكائن إذا كان يحتوي اللاعب أو الجويستيك أو الكاميرا (كان يكسر الحركة)
        bool holdsGameplay =
            GetComponentInChildren<PlayerMovement>(true) != null ||
            GetComponentInChildren<MobileJoystick>(true) != null ||
            GetComponentInChildren<CameraFollow>(true) != null ||
            GetComponent<PlayerProfile>() != null;

        if (!holdsGameplay)
            gameObject.SetActive(false);
        else
            enabled = false;


        // 4. قفل الماوس للبدء في تحريك الكاميرا والتحقيق
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Transform GetSpawnPointForRole(DetectiveRole role)
    {
        if (randomSpawnPoints != null && randomSpawnPoints.Length > 0)
        {
            int spawnIndex = Mathf.Abs((int)role) % randomSpawnPoints.Length;
            return randomSpawnPoints[spawnIndex];
        }
        return null;
    }

    private void ConfigureTabletPresentation()
    {
        ConfigureInput(codenameInput);
        ConfigureButtonLabel(maleBtn, true);
        ConfigureButtonLabel(femaleBtn, true);
        ConfigureButtonLabel(fieldDetectiveBtn, false);
        ConfigureButtonLabel(forensicsBtn, false);
        ConfigureButtonLabel(digitalAnalystBtn, false);
        ConfigureButtonLabel(interrogatorBtn, false);
    }

    private static void ConfigureInput(TMP_InputField input)
    {
        if (input == null) return;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 16;
    }

    private static void ConfigureButtonLabel(Button button, bool isShortLabel)
    {
        if (button == null) return;

        foreach (TextMeshProUGUI text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            ConfigureTextElement(text, isShortLabel ? 16f : 13f, isShortLabel ? 34f : 25f,
                isShortLabel ? TextWrappingModes.NoWrap : TextWrappingModes.Normal);

            if (text.transform.parent != button.transform) continue;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.32f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private static void ConfigureTextElement(TMP_Text text, float minSize, float maxSize, TextWrappingModes wrapping)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = wrapping;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static bool TryGetSafeSpawnPosition(PlayerProfile profile, Transform targetSpawn, out Vector3 safePosition)
    {
        Vector3 origin = targetSpawn != null ? targetSpawn.position : profile.transform.position;
        safePosition = origin;

        CapsuleCollider capsule = profile.GetComponent<CapsuleCollider>();
        bool wasEnabled = capsule != null && capsule.enabled;
        if (capsule != null) capsule.enabled = false;

        const int samplesPerRing = 8;
        const float ringSpacing = 1.25f;
        bool foundPosition = false;

        for (int ring = 0; ring < 5 && !foundPosition; ring++)
        {
            int samples = ring == 0 ? 1 : samplesPerRing;
            for (int sample = 0; sample < samples; sample++)
            {
                float angle = sample * (360f / samples);
                Vector3 offset = ring == 0 ? Vector3.zero : Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (ring * ringSpacing);
                Vector3 candidate = origin + offset;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas)) continue;
                if (!IsSpawnClear(navHit.position, capsule)) continue;

                safePosition = navHit.position;
                foundPosition = true;
                break;
            }
        }

        if (capsule != null) capsule.enabled = wasEnabled;
        return foundPosition;
    }

    private static bool IsSpawnClear(Vector3 position, CapsuleCollider capsule)
    {
        float radius = capsule != null ? capsule.radius : 0.35f;
        float height = capsule != null ? Mathf.Max(capsule.height, radius * 2f) : 1.8f;
        Vector3 center = position + (capsule != null ? capsule.center : Vector3.up * (height * 0.5f));
        float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 top = center + Vector3.up * halfSegment;
        Vector3 bottom = center - Vector3.up * halfSegment;

        return !Physics.CheckCapsule(top, bottom, radius, ~0, QueryTriggerInteraction.Ignore);
    }
}
