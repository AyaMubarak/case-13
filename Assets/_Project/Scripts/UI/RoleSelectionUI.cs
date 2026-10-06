using UnityEngine;
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

    private void Start()
    {
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
            if (targetSpawn != null)
            {
                CharacterController cc = profile.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                profile.transform.position = targetSpawn.position;
                profile.transform.rotation = targetSpawn.rotation;

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
        }

        // 3. إخفاء لوحة اختيار الدور فقط، مع إبقاء الـ Canvas الرئيسي مفعلاً لظهور عناصر الـ HUD ورسالة [E]
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        gameObject.SetActive(false);

        // 4. قفل الماوس للبدء في تحريك الكاميرا والتحقيق
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Transform GetSpawnPointForRole(DetectiveRole role)
    {
        // تم التعديل لاختيار مكان عشوائي بدلاً من مكان مخصص للدور
        if (randomSpawnPoints != null && randomSpawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, randomSpawnPoints.Length);
            return randomSpawnPoints[randomIndex];
        }
        return null;
    }
}