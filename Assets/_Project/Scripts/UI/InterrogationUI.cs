using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterrogationUI : MonoBehaviour
{
    public static InterrogationUI Instance { get; private set; }

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

    private SuspectInterrogation currentSuspect;

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

    public void OpenInterrogation(SuspectInterrogation suspect)
    {
        currentSuspect = suspect;

        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(true);
        }

        // تشغيل صوت فتح الملف (Dossier / Paper Flip)
        AudioClip openClip = Resources.Load<AudioClip>("Audio/dragon-studio-flipping-book-page-499646");
        if (openClip != null)
        {
            AudioSource.PlayClipAtPoint(openClip, Camera.main.transform.position);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInteractionPrompt();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (suspectNameText != null)
        {
            suspectNameText.text = $"// SUSPECT DOSSIER: {suspect.suspectName.ToUpper()}";
        }

        if (dialogueStatementText != null)
        {
            dialogueStatementText.text = suspect.isAlibiBroken ? suspect.brokenAlibiResponse : suspect.initialAlibi;
        }

        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = suspect.isAlibiBroken ? "<color=green>[CONTRADICTION CONFIRMED: ALIBI BROKEN]</color>" : "STATUS: UNVERIFIED CLAIM";
        }

        if (suspectPortraitImage != null)
        {
            if (suspect.suspectPortrait != null)
            {
                suspectPortraitImage.gameObject.SetActive(true);
                suspectPortraitImage.sprite = suspect.suspectPortrait;
            }
            else
            {
                suspectPortraitImage.gameObject.SetActive(false);
            }
        }

        RefreshDetectiveBoard();
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
        if (currentSuspect == null) return;

        if (EvidenceManager.Instance != null && EvidenceManager.Instance.CollectedCount >= 2)
        {
            currentSuspect.BreakAlibi();

            // تشغيل صوت النجاح عند كسر العذر
            AudioClip successClip = Resources.Load<AudioClip>("Audio/meldix-success-340660");
            if (successClip != null)
            {
                AudioSource.PlayClipAtPoint(successClip, Camera.main.transform.position);
            }

            if (dialogueStatementText != null)
            {
                dialogueStatementText.text = currentSuspect.brokenAlibiResponse;
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
            // تشغيل صوت الخطأ عند نقص الأدلة
            AudioClip errorClip = Resources.Load<AudioClip>("Audio/u_xg7ssi08yr-error-tone-10-363618");
            if (errorClip != null)
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