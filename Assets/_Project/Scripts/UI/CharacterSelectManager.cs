using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        ResolveInputAndButtons();
        LoadSavedIdentity();
        PositionNameInput();
        // يتفعل تلقائياً في كل مرة تفتح فيها هذه اللوحة
        // إعادة التحديد الافتراضي أو استرجاع ما كان محدداً
        SelectGender(SelectedGender);
        SelectRole(SelectedRole);

        if (nameInputField != null && !string.IsNullOrEmpty(SelectedName))
        {
            nameInputField.text = SelectedName;
        }
    }

    private void ResolveInputAndButtons()
    {
        if (nameInputField == null)
        {
            foreach (Transform input in GetComponentsInChildren<Transform>(true))
            {
                if (input.name != "Input_PlayerName") continue;
                nameInputField = input.GetComponent<TMP_InputField>();
                break;
            }
        }

        if (nameInputField != null)
        {
            nameInputField.interactable = true;
            if (nameInputField.textComponent != null)
                nameInputField.textComponent.color = Color.white;
            nameInputField.onEndEdit.RemoveListener(SaveEnteredName);
            nameInputField.onEndEdit.AddListener(SaveEnteredName);
        }

        Button confirm = FindButton("Btn_Confirm");
        if (confirm != null)
        {
            confirm.onClick.RemoveListener(OnConfirmAndStart);
            confirm.onClick.AddListener(OnConfirmAndStart);
        }

        BindChoiceButton("Btn_Male", SelectMale);
        BindChoiceButton("Btn_Female", SelectFemale);
        BindChoiceButton("Btn_FieldDetective", SelectFieldDetective);
        BindChoiceButton("Btn_Forensics", SelectForensics);
        BindChoiceButton("Btn_DigitalAnalyst", SelectDigitalAnalyst);
        BindChoiceButton("Btn_Interrogator", SelectInterrogator);
        BindBackButtons();
    }

    private void LoadSavedIdentity()
    {
        SelectedName = PlayerPrefs.GetString("Player_Name", SelectedName);
        SelectedGender = PlayerPrefs.GetString("Player_Gender", SelectedGender);
        SelectedRole = PlayerPrefs.GetString("Player_Role", SelectedRole);
    }

    private void BindChoiceButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindButton(buttonName);
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void BindBackButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (!button.name.ToLowerInvariant().Contains("back")) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnBackClicked);
        }
    }

    private Button FindButton(string objectName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            if (button.name == objectName) return button;
        return null;
    }

    // --- اختيار الجنس ---
    public void SelectMale()
    {
        SelectedGender = "Male";
        UpdateHighlight(genderHighlight, maleBtnRect);
        SaveIdentity();
    }

    public void SelectFemale()
    {
        SelectedGender = "Female";
        UpdateHighlight(genderHighlight, femaleBtnRect);
        SaveIdentity();
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
        SaveIdentity();
    }

    public void SelectForensics()
    {
        SelectedRole = "Forensics Expert";
        UpdateHighlight(roleHighlight, forensicsBtnRect);
        SaveIdentity();
    }

    public void SelectDigitalAnalyst()
{
    SelectedRole = "Digital Analyst";
    UpdateHighlight(roleHighlight, digitalAnalystBtnRect);
    SaveIdentity();
}

    public void SelectInterrogator()
    {
        SelectedRole = "Interrogator";
        UpdateHighlight(roleHighlight, interrogatorBtnRect);
        SaveIdentity();
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
            highlight.SetParent(target.parent, false);
            highlight.anchorMin = target.anchorMin;
            highlight.anchorMax = target.anchorMax;
            highlight.pivot = target.pivot;
            highlight.anchoredPosition = target.anchoredPosition;
            highlight.sizeDelta = target.sizeDelta;
            highlight.localRotation = target.localRotation;
            highlight.localScale = Vector3.one;
            highlight.SetAsLastSibling();
        }
    }

    private void PositionNameInput()
    {
        if (nameInputField == null) return;

        RectTransform inputRect = nameInputField.transform as RectTransform;
        if (inputRect == null) return;

        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(-592f, 172f);
        inputRect.sizeDelta = new Vector2(460f, 74f);
        inputRect.localScale = Vector3.one;
    }

    // --- زر التأكيد (Confirm & Start) ---
    public void OnConfirmAndStart()
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            SelectedName = nameInputField.text.Trim();
        }

        SaveIdentity();

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

    private void SaveEnteredName(string enteredName)
    {
        if (!string.IsNullOrWhiteSpace(enteredName))
        {
            SelectedName = enteredName.Trim();
            SaveIdentity();
        }
    }

    private static void SaveIdentity()
    {
        PlayerPrefs.SetString("Player_Name", SelectedName);
        PlayerPrefs.SetString("Player_Gender", SelectedGender);
        PlayerPrefs.SetString("Player_Role", SelectedRole);
        PlayerPrefs.Save();
    }
}
