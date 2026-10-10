using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    public TextMeshProUGUI interactionPromptText;
    public TextMeshProUGUI evidenceCounterText;
    public TextMeshProUGUI notificationBannerText;

    [Header("HUD Container to Hide in Menus")]
    public GameObject inGameHUD; // الكائن الذي يجمع عناصر الـ HUD ليختفي عند فتح القوائم

    [Header("Mobile References")]
    public GameObject mobileControls;         // مجسم أزرار الموبايل
    public GameObject mobileInventoryBtn;    // زر الحقيبة BAG
    public GameObject mobileDossierBtn;      // زر ملفات التحقيق (بديل حرف B)

    [Header("Evidence Inventory Panel")]
    public GameObject inventoryPanel;
    public Transform evidenceListContainer;
    public GameObject evidenceItemUIPrefab;

    [Header("Detective Board Panel (لوحة الاستجواب أو الملاحظات)")]
    public GameObject interrogationBoardPanel; // لوحة التحقيق القديمة إن رغبتِ باستخدامها مستقبلاً

    [Header("Evidence Detail View")]
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailDescriptionText;

    private bool isInventoryOpen = false;
    private Coroutine notificationCoroutine;

    [Header("Mobile Testing")]
    public bool forceMobileUIInEditor = true; // ضع عليها صح في الـ Inspector لاختبار الجوال

    private bool IsTouchDevice()
    {
#if UNITY_EDITOR
        return forceMobileUIInEditor;
#else
        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HideInteractionPrompt();
        UpdateEvidenceCount(0, 5);

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (interrogationBoardPanel != null) interrogationBoardPanel.SetActive(false);

        if (notificationBannerText != null && notificationBannerText.transform.parent != null)
        {
            notificationBannerText.transform.parent.gameObject.SetActive(false);
        }

        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(false);
        }

        if (mobileControls != null)
        {
            mobileControls.SetActive(IsTouchDevice());
        }

        if (mobileInventoryBtn != null)
        {
            mobileInventoryBtn.SetActive(false);
        }

        if (mobileDossierBtn != null)
        {
            mobileDossierBtn.SetActive(false);
        }
    }

    private void Update()
    {
        // زر Tab للحقيبة
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (EvidenceManager.Instance != null && EvidenceManager.Instance.GetCollectedEvidence().Count > 0)
            {
                // إغلاق لوحة الاستجواب إن كانت مفتوحة
                if (InterrogationUI.Instance != null && InterrogationUI.Instance.interrogationPanel != null && InterrogationUI.Instance.interrogationPanel.activeSelf)
                {
                    InterrogationUI.Instance.CloseInterrogation();
                }
                ToggleInventory();
            }
        }

        // زر B لوحة ملفات واستجواب المتهم (Interrogation Panel)
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (isInventoryOpen) ToggleInventory();
            ToggleInterrogationDossier();
        }
    }

    // فتح وإغلاق ملف التحقيق الصحيح
    public void ToggleInterrogationDossier()
    {
        // إغلاق الحقيبة إن كانت مفتوحة لمنع التداخل
        if (isInventoryOpen)
        {
            isInventoryOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        if (InterrogationUI.Instance != null && InterrogationUI.Instance.interrogationPanel != null)
        {
            bool isCurrentlyOpen = InterrogationUI.Instance.interrogationPanel.activeSelf;
            if (isCurrentlyOpen)
            {
                InterrogationUI.Instance.CloseInterrogation();
                if (inGameHUD != null) inGameHUD.SetActive(true);
            }
            else
            {
                InterrogationUI.Instance.OpenRecordedInterrogation();
                if (inGameHUD != null) inGameHUD.SetActive(false);
            }
        }
        else
        {
            // في حال عدم وجود InterrogationUI يعود احتياطياً للوحة القديمة
            ToggleDetectiveBoard();
        }
    }

    // دالة لاستدعائها من زر الموبايل لفتح/إغلاق ملف التحقيق
    public void OnMobileDossierButtonClicked()
    {
        ToggleInterrogationDossier();
    }

    public void ToggleInventory()
    {
        // إغلاق ملف التحقيق إن كان مفتوحاً لمنع التداخل
        if (InterrogationUI.Instance != null && InterrogationUI.Instance.interrogationPanel != null && InterrogationUI.Instance.interrogationPanel.activeSelf)
        {
            InterrogationUI.Instance.CloseInterrogation();
        }
        else if (interrogationBoardPanel != null && interrogationBoardPanel.activeSelf)
        {
            interrogationBoardPanel.SetActive(false);
        }

        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null) inventoryPanel.SetActive(isInventoryOpen);
        if (inGameHUD != null) inGameHUD.SetActive(!isInventoryOpen);

        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(!isInventoryOpen);
        }

        if (isInventoryOpen)
        {
            RefreshEvidenceList();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ToggleDetectiveBoard()
    {
        if (interrogationBoardPanel == null) return;

        bool isBoardOpen = !interrogationBoardPanel.activeSelf;
        interrogationBoardPanel.SetActive(isBoardOpen);

        if (inGameHUD != null) inGameHUD.SetActive(!isBoardOpen);

        if (isBoardOpen)
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

    public void UnlockInventoryAccess()
    {
        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(true);
        }

        if (mobileInventoryBtn != null && IsTouchDevice())
        {
            mobileInventoryBtn.SetActive(true);
        }

        if (IsTouchDevice())
        {
            ShowOnScreenNotification("NEW CLUE ADDED // TAP [BAG] TO INSPECT");
        }
        else
        {
            ShowOnScreenNotification("NEW CLUE ADDED // PRESS [TAB] TO INSPECT");
        }
    }

    public void UnlockDossierAccess()
    {
        if (mobileDossierBtn != null && IsTouchDevice())
        {
            mobileDossierBtn.SetActive(true);
        }
    }

    public void SetMobileControlsActive(bool isActive)
    {
        if (mobileControls != null && IsTouchDevice())
        {
            mobileControls.SetActive(isActive);
        }
    }

    public void RefreshEvidenceList()
    {
        if (evidenceListContainer == null || evidenceItemUIPrefab == null || EvidenceManager.Instance == null)
            return;

        foreach (Transform child in evidenceListContainer)
        {
            Destroy(child.gameObject);
        }

        List<Evidence> list = EvidenceManager.Instance.GetCollectedEvidence();
        for (int i = 0; i < list.Count; i++)
        {
            GameObject itemObj = Instantiate(evidenceItemUIPrefab, evidenceListContainer);
            EvidenceItemUI itemUI = itemObj.GetComponent<EvidenceItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(list[i]);
            }
        }

        if (list.Count > 0)
        {
            ShowEvidenceDetails(list[0]);
        }
        else
        {
            ClearDetails();
        }
    }

    public void ShowEvidenceDetails(Evidence evidence)
    {
        if (detailNameText != null) detailNameText.text = evidence.evidenceName;
        if (detailTypeText != null) detailTypeText.text = $"Type: {evidence.type}";
        if (detailDescriptionText != null) detailDescriptionText.text = evidence.description;
    }

    private void ClearDetails()
    {
        if (detailNameText != null) detailNameText.text = "No Evidence Selected";
        if (detailTypeText != null) detailTypeText.text = "";
        if (detailDescriptionText != null) detailDescriptionText.text = "Evidence details will appear here once inspected.";
    }

    public void ShowInteractionPrompt(string message)
    {
        if (InterrogationUI.Instance != null && InterrogationUI.Instance.interrogationPanel != null && InterrogationUI.Instance.interrogationPanel.activeSelf)
        {
            return;
        }

        if (interactionPromptText != null)
        {
            if (IsTouchDevice())
            {
                interactionPromptText.text = "TAP TO INVESTIGATE";
            }
            else
            {
                interactionPromptText.text = message;
            }

            if (interactionPromptText.transform.parent != null)
            {
                interactionPromptText.transform.parent.gameObject.SetActive(true);
            }
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null && interactionPromptText.transform.parent != null)
        {
            interactionPromptText.transform.parent.gameObject.SetActive(false);
        }
    }

    public void UpdateEvidenceCount(int current, int total)
    {
        if (evidenceCounterText != null)
        {
            evidenceCounterText.text = $"EVIDENCE: {current}/{total}";
        }
    }

    public void ShowOnScreenNotification(string message, float duration = 5.5f)
    {
        if (notificationBannerText == null) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(NotificationRoutine(message, duration));
    }

    private IEnumerator NotificationRoutine(string message, float duration)
    {
        notificationBannerText.text = message;

        if (notificationBannerText.transform.parent != null)
        {
            notificationBannerText.transform.parent.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(duration);

        if (notificationBannerText.transform.parent != null)
        {
            notificationBannerText.transform.parent.gameObject.SetActive(false);
        }
    }
}