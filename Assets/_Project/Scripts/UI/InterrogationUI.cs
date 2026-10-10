using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterrogationUI : MonoBehaviour
{
    public static InterrogationUI Instance { get; private set; }
    public static SuspectInterrogation lastInterrogatedSuspect;

    [Header("UI Panels")]
    public GameObject interrogationPanel;

    [Header("Dossier Texts & Image")]
    public TextMeshProUGUI suspectNameText;
    public TextMeshProUGUI dialogueStatementText;
    public TextMeshProUGUI statusFeedbackText;
    public Image suspectPortraitImage;

    [Header("Action Buttons")]
    public Button confrontWithKeycardBtn;
    public Button closeDossierBtn;

    [Header("Detective Board / Multiple Records (Dynamic List)")]
    public Transform contentParent;
    public GameObject statementCardPrefab;

    // تخزين بيانات المتهم المستجوب
    private string savedSuspectName = "";
    private string savedStatement = "";
    private Sprite savedPortrait = null;
    private bool hasRecordedStatement = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(false);
        }

        if (confrontWithKeycardBtn != null)
        {
            confrontWithKeycardBtn.onClick.RemoveAllListeners();
            confrontWithKeycardBtn.onClick.AddListener(ConfrontSuspect);
        }

        if (closeDossierBtn != null)
        {
            closeDossierBtn.onClick.RemoveAllListeners();
            closeDossierBtn.onClick.AddListener(CloseInterrogation);
        }
    }

    private void Update()
    {
        // فتح وإغلاق اللوحة عند الضغط على B
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (interrogationPanel != null)
            {
                if (interrogationPanel.activeSelf)
                {
                    CloseInterrogation();
                }
                else
                {
                    OpenRecordedInterrogation();
                }
            }
        }
    }

    // دالة لاستقبال بيانات المتهم وتخزينها عند انتهاء التحقيق
    public void SetInterrogationRecord(string suspectName, string statement, Sprite portrait = null)
    {
        savedSuspectName = suspectName;
        savedStatement = statement;
        savedPortrait = portrait;
        hasRecordedStatement = true;

        // تحديث النصوص مسبقاً
        UpdateUIFields(savedSuspectName, savedStatement, savedPortrait);
    }

    public void OpenRecordedInterrogation()
    {
        if (interrogationPanel == null) return;

        interrogationPanel.SetActive(true);

        // تشغيل صوت فتح الملف إن وجد
        AudioClip openClip = Resources.Load<AudioClip>("Audio/dragon-studio-flipping-book-page-499646");
        if (openClip != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(openClip, Camera.main.transform.position);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInteractionPrompt();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (hasRecordedStatement)
        {
            UpdateUIFields(savedSuspectName, savedStatement, savedPortrait);
        }
        else
        {
            if (suspectNameText != null) suspectNameText.text = "// SUSPECT DOSSIER: NO DATA";
            if (dialogueStatementText != null) dialogueStatementText.text = "No interrogation recorded yet. Complete the questioning first.";
            if (statusFeedbackText != null) statusFeedbackText.text = "STATUS: PENDING INTERROGATION";
        }

        RefreshDetectiveBoard();
    }

    private void UpdateUIFields(string name, string statement, Sprite portrait)
    {
        if (suspectNameText != null)
        {
            suspectNameText.text = $"// SUSPECT DOSSIER: {name.ToUpper()}";
        }

        if (dialogueStatementText != null)
        {
            dialogueStatementText.text = statement;
        }

        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = "<color=green>[STATEMENT LOGGED]</color>";
        }

        if (suspectPortraitImage != null)
        {
            if (portrait != null)
            {
                suspectPortraitImage.gameObject.SetActive(true);
                suspectPortraitImage.sprite = portrait;
            }
            else
            {
                suspectPortraitImage.gameObject.SetActive(false);
            }
        }
    }

    public void RefreshDetectiveBoard()
    {
        if (contentParent == null || statementCardPrefab == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (InterrogationManager.Instance != null)
        {
            var records = InterrogationManager.Instance.GetAllRecords();
            foreach (var rec in records)
            {
                GameObject card = Instantiate(statementCardPrefab, contentParent);

                TextMeshProUGUI[] texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var t in texts)
                {
                    if (t.name.Contains("Name"))
                        t.text = rec.suspectName;
                    else if (t.name.Contains("Text") || t.name.Contains("Statement"))
                        t.text = rec.statementText;
                }

                Image[] images = card.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.name.Contains("Portrait") && rec.suspectPortrait != null)
                    {
                        img.sprite = rec.suspectPortrait;
                    }
                }
            }
        }
    }

    public void ConfrontSuspect()
    {
        if (EvidenceManager.Instance != null && EvidenceManager.Instance.CollectedCount >= 2)
        {
            AudioClip successClip = Resources.Load<AudioClip>("Audio/meldix-success-340660");
            if (successClip != null && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(successClip, Camera.main.transform.position);
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=green>[CONTRADICTION CONFIRMED: TIMELINE BREACH]</color>";
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("ALIBI SHATTERED // NEW LEAD UNLOCKED");
            }
        }
        else
        {
            AudioClip errorClip = Resources.Load<AudioClip>("Audio/u_xg7ssi08yr-error-tone-10-363618");
            if (errorClip != null && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(errorClip, Camera.main.transform.position);
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=red>[INSUFFICIENT EVIDENCE] COLLECT DIGITAL KEYCARD FIRST!</color>";
            }
        }
    }

    public void CloseInterrogation()
    {
        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}