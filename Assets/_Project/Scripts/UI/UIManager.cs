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

    [Header("Evidence Inventory Panel")]
    public GameObject inventoryPanel;
    public Transform evidenceListContainer;
    public GameObject evidenceItemUIPrefab;

    [Header("Detective Board Panel")]
    public GameObject interrogationBoardPanel; // لوحة التحقيق الكبيرة (تفتح بزر B)

    [Header("Evidence Detail View")]
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailDescriptionText;

    private bool isInventoryOpen = false;
    private bool isBoardOpen = false;
    private Coroutine notificationCoroutine;

    private bool IsTouchDevice()
    {
        return Application.isMobilePlatform && !Application.isEditor;
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
    }

    private void Update()
    {
        // زر Tab للحقيبة
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (EvidenceManager.Instance != null && EvidenceManager.Instance.GetCollectedEvidence().Count > 0)
            {
                if (isBoardOpen) ToggleDetectiveBoard();
                ToggleInventory();
            }
        }

        // زر B لوحة التحقيق
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (isInventoryOpen) ToggleInventory();
            ToggleDetectiveBoard();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null) inventoryPanel.SetActive(isInventoryOpen);
        if (inGameHUD != null) inGameHUD.SetActive(!isInventoryOpen);

        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(!isInventoryOpen);
        }

        if (mobileControls != null && IsTouchDevice())
        {
            mobileControls.SetActive(!isInventoryOpen);
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

        isBoardOpen = !isBoardOpen;
        interrogationBoardPanel.SetActive(isBoardOpen);

        // إخفاء الـ HUD بالكامل عند فتح لوحة التحقيق
        if (inGameHUD != null) inGameHUD.SetActive(!isBoardOpen);

        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(!isBoardOpen);
        }

        if (mobileControls != null && IsTouchDevice())
        {
            mobileControls.SetActive(!isBoardOpen);
        }

        if (isBoardOpen)
        {
            if (InterrogationUI.Instance != null)
            {
                InterrogationUI.Instance.RefreshDetectiveBoard();
            }

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
        // إذا كانت نافذة الاستجواب الفردية مفتوحة، امنع ظهور رسالة [E] نهائياً
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

    public void ShowOnScreenNotification(string message)
    {
        if (notificationBannerText == null) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        notificationCoroutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationBannerText.text = message;

        if (notificationBannerText.transform.parent != null)
        {
            notificationBannerText.transform.parent.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(2.5f);

        if (notificationBannerText.transform.parent != null)
        {
            notificationBannerText.transform.parent.gameObject.SetActive(false);
        }
    }
}