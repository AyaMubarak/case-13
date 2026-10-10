using UnityEngine;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Highlights (إطارات التحديد)")]
    [SerializeField] private RectTransform genderHighlight;
    [SerializeField] private RectTransform roleHighlight;

    [Header("Gender Buttons Targets")]
    [SerializeField] private RectTransform maleBtnRect;
    [SerializeField] private RectTransform femaleBtnRect;

    [Header("Role Buttons Targets")]
    [SerializeField] private RectTransform fieldDetectiveBtnRect;
    [SerializeField] private RectTransform forensicsBtnRect;
    [SerializeField] private RectTransform digitalAnalystBtnRect;
    [SerializeField] private RectTransform interrogatorBtnRect;

    [Header("Panels Navigation")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject mainMenuButtons; // لوحة أزرار القائمة الرئيسية لإعادتها عند الرجوع

    // المتغيرات المحفوظة
    public static string SelectedName { get; private set; } = "Detective";
    public static string SelectedGender { get; private set; } = "Male";
    public static string SelectedRole { get; private set; } = "Field Detective";

    private void OnEnable()
    {
        // يتفعل تلقائياً في كل مرة تفتح فيها هذه اللوحة
        // إعادة التحديد الافتراضي أو استرجاع ما كان محدداً
        SelectGender(SelectedGender);
        SelectRole(SelectedRole);

        if (nameInputField != null && !string.IsNullOrEmpty(SelectedName))
        {
            nameInputField.text = SelectedName;
        }
    }

    // --- اختيار الجنس ---
    public void SelectMale()
    {
        SelectedGender = "Male";
        UpdateHighlight(genderHighlight, maleBtnRect);
    }

    public void SelectFemale()
    {
        SelectedGender = "Female";
        UpdateHighlight(genderHighlight, femaleBtnRect);
    }

    public void SelectGender(string gender)
    {
        if (gender == "Female") SelectFemale();
        else SelectMale();
    }

    // --- اختيار الدور ---
    public void SelectFieldDetective()
    {
        SelectedRole = "Field Detective";
        UpdateHighlight(roleHighlight, fieldDetectiveBtnRect);
    }

    public void SelectForensics()
    {
        SelectedRole = "Forensics Expert";
        UpdateHighlight(roleHighlight, forensicsBtnRect);
    }

 public void SelectDigitalAnalyst()
{
    SelectedRole = "Digital Analyst";
    UpdateHighlight(roleHighlight, digitalAnalystBtnRect);
}

    public void SelectInterrogator()
    {
        SelectedRole = "Interrogator";
        UpdateHighlight(roleHighlight, interrogatorBtnRect);
    }

    public void SelectRole(string role)
    {
        switch (role)
        {
            case "Forensics Expert": SelectForensics(); break;
            case "Digital Analyst": SelectDigitalAnalyst(); break;
            case "Interrogator": SelectInterrogator(); break;
            default: SelectFieldDetective(); break;
        }
    }

    private void UpdateHighlight(RectTransform highlight, RectTransform target)
    {
        if (highlight != null && target != null)
        {
            highlight.gameObject.SetActive(true);
            highlight.position = target.position;
            highlight.sizeDelta = target.sizeDelta;
        }
    }

    // --- زر التأكيد (Confirm & Start) ---
    public void OnConfirmAndStart()
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            SelectedName = nameInputField.text.Trim();
        }

        PlayerPrefs.SetString("Player_Name", SelectedName);
        PlayerPrefs.SetString("Player_Gender", SelectedGender);
        PlayerPrefs.SetString("Player_Role", SelectedRole);
        PlayerPrefs.Save();

        Debug.Log($"تم الحفظ بنجاح: الاسم={SelectedName} | الجنس={SelectedGender} | الدور={SelectedRole}");

        // إغلاق لوحة الشخصية وفتح اللوبي
        gameObject.SetActive(false);
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }
    }

    // --- زر الرجوع (Back) ---
    public void OnBackClicked()
    {
        gameObject.SetActive(false);
        if (mainMenuButtons != null)
        {
            mainMenuButtons.SetActive(true);
        }
    }
}