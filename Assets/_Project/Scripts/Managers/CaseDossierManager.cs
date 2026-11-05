using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CaseDossierManager : MonoBehaviour
{
    public static CaseDossierManager Instance { get; private set; }

    [Header("Suspects Database (5 Suspects)")]
    public List<SuspectProfile> suspects = new List<SuspectProfile>();

    [Header("Main Grid UI")]
    [SerializeField] private GameObject dossierPanel;
    [SerializeField] private TMP_Text findingSummaryText;
    [SerializeField] private Button closeDossierBtn;

    public bool IsDossierOpen => (dossierPanel != null && dossierPanel.activeSelf) || (suspectDetailsPanel != null && suspectDetailsPanel.activeSelf);

    [Header("Suspect Card UI Slots (5 Cards)")]
    [SerializeField] private SuspectCardUI[] cardSlots;

    [Header("Suspect Detailed File Panel (Popup)")]
    [SerializeField] private GameObject suspectDetailsPanel;
    [SerializeField] private Image detailPortrait;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailRoleText;
    [SerializeField] private TMP_Text detailBloodText;
    [SerializeField] private TMP_Text detailAlibiText;
    [SerializeField] private GameObject detailEliminatedStamp; // شارة/ختم الاستبعاد داخل تفاصيل المتهم
    [SerializeField] private Button manualEliminateBtn;
    [SerializeField] private Button backToGridBtn;

    [Header("Evidence & Final Accusation Tracking")]
    [SerializeField] private int totalEvidencesRequired = 5;
    private int collectedEvidencesCount = 0;

    [Header("Final Interrogation")]
    [SerializeField] private GameObject interrogationPromptUI;
    [SerializeField] private Button confrontBtn;

    [Header("UI Icons & Navigation Switch")]
    [SerializeField] private GameObject generalDossierIconBtn;   // أيقونة ملف القضية العامة
    [SerializeField] private GameObject finalSuspectsIconBtn;    // أيقونة المتهمين الأساسيين (إيفان ونورا)

    [Header("Mobile Controls")]
    [SerializeField] private GameObject mobileControls;

    private SuspectProfile currentSelectedSuspect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string existingScene = Instance.gameObject.scene.name;
            bool enteringDifferentCase = existingScene != activeScene && activeScene.Contains("Case02");

            if (enteringDifferentCase)
            {
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        LoadCaseSpecificSuspects();

        AssignVictorPortrait();
        AssignElenaPortrait();
        AssignPortrait("SUS_ARTHUR", "Arthur", "Portraits/Arthur");
        AssignPortrait("SUS_MAYA", "Maya", "Portraits/Maya_Lin");
        AssignPortrait("SUS_DAMIAN", "Damian", "Portraits/Damian");

        if (closeDossierBtn != null)
        {
            closeDossierBtn.onClick.RemoveAllListeners();
            closeDossierBtn.onClick.AddListener(CloseDossier);
        }

        // ربط أي أزرار إغلاق إضافية داخل dossierPanel لضمان إغلاق اللوحة تحت أي ظرف
        if (dossierPanel != null)
        {
            Button[] allButtons = dossierPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in allButtons)
            {
                if (b != null && b.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(CloseDossier);
                    b.onClick.AddListener(CloseDossier);
                }
            }
        }

        if (backToGridBtn != null)
        {
            backToGridBtn.onClick.RemoveAllListeners();
            backToGridBtn.onClick.AddListener(CloseDetailsPanel);
        }

        if (manualEliminateBtn != null)
        {
            manualEliminateBtn.onClick.RemoveAllListeners();
            manualEliminateBtn.onClick.AddListener(EliminateCurrentSuspect);
        }
    }

    private void LoadCaseSpecificSuspects()
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02")) return;

        // Case 02 has its own suspect roster. Do not reuse the Case 01
        // serialized list (Nora/Evan/Marcus/Leo/Sarah).
        suspects = new List<SuspectProfile>
        {
            CreateCase2Suspect("SUS_DAMIAN", "Damian King", "External Security / Access Control", "O+", "Was stationed outside the main security gate.", "Portraits/Damian"),
            CreateCase2Suspect("SUS_ARTHUR", "Arthur Vale", "Systems Engineer", "A-", "Claims he was working remotely through the lab VPN.", "Portraits/Arthur"),
            CreateCase2Suspect("SUS_MAYA", "Maya Lin", "Biochemical Researcher", "AB+", "Was assigned to the biochemical research wing.", "Portraits/Maya_Lin"),
            CreateCase2Suspect("SUS_VICTOR", "Victor Sterling", "Lead Lab Engineer", "B+", "Had terminal encryption credentials and access to the lab systems.", "Portraits/Victor_Sterling"),
            CreateCase2Suspect("SUS_ELENA", "Elena Rostova", "Senior Research Scientist", "AB-", "Had access to the chemical storage and encryption protocols.", "Portraits/Elena_Rostova")
        };
    }

    private static SuspectProfile CreateCase2Suspect(string id, string name, string role, string blood, string alibi, string portraitPath)
    {
        return new SuspectProfile
        {
            suspectID = id,
            suspectName = name,
            roleOrTitle = role,
            bloodType = blood,
            alibi = alibi,
            portrait = Resources.Load<Sprite>(portraitPath),
            isEliminated = false,
            eliminationReason = string.Empty
        };
    }

    private void AssignElenaPortrait()
    {
        Sprite elenaPortrait = Resources.Load<Sprite>("Portraits/Elena_Rostova");
        if (elenaPortrait == null) return;

        foreach (SuspectProfile suspect in suspects)
        {
            if (suspect != null && (suspect.suspectID == "SUS_ELENA" || (suspect.suspectName ?? "").Contains("Elena")))
            {
                suspect.portrait = elenaPortrait;
            }
        }
    }

    private void AssignVictorPortrait()
    {
        Sprite victorPortrait = Resources.Load<Sprite>("Portraits/Victor_Sterling");
        if (victorPortrait == null) return;

        foreach (SuspectProfile suspect in suspects)
        {
            if (suspect != null && (suspect.suspectID == "SUS_VICTOR" || (suspect.suspectName ?? "").Contains("Victor")))
            {
                suspect.portrait = victorPortrait;
            }
        }
    }

    private void AssignPortrait(string suspectId, string suspectName, string resourcePath)
    {
        Sprite portrait = Resources.Load<Sprite>(resourcePath);
        if (portrait == null) return;

        foreach (SuspectProfile suspect in suspects)
        {
            if (suspect != null && (suspect.suspectID == suspectId || (suspect.suspectName ?? "").Contains(suspectName)))
            {
                suspect.portrait = portrait;
            }
        }
    }

    private void Start()
    {
        ResolveDossierUIReferences();
        InitializeCards();
        if (dossierPanel != null) dossierPanel.SetActive(false);
        if (suspectDetailsPanel != null) suspectDetailsPanel.SetActive(false);
        if (interrogationPromptUI != null) interrogationPromptUI.SetActive(false);

        // ربط زر الإغلاق مجدداً للتأكيد التام
        if (closeDossierBtn != null)
        {
            closeDossierBtn.onClick.RemoveAllListeners();
            closeDossierBtn.onClick.AddListener(CloseDossier);
        }

        // ربط أي أزرار إغلاق إضافية داخل dossierPanel لضمان إغلاق اللوحة تحت أي ظرف
        if (dossierPanel != null)
        {
            Button[] allButtons = dossierPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in allButtons)
            {
                if (b != null && b.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(CloseDossier);
                    b.onClick.AddListener(CloseDossier);
                }
            }
        }

        // البحث التلقائي عن زر ملف المتهمين في المشهد إن لم يكن مربوطاً في الـ Inspector
        if (generalDossierIconBtn == null)
        {
            var foundBtn = GameObject.Find("Btn_Dossier_Mobile");
            if (foundBtn != null) generalDossierIconBtn = foundBtn;
        }

        // أيقونة ملف المشتبه فيهم تبقى ظاهرة دائماً؛ القفل يخص المحتوى فقط.
        if (generalDossierIconBtn != null)
        {
            generalDossierIconBtn.SetActive(true);
            Button btnComp = generalDossierIconBtn.GetComponent<Button>();
            if (btnComp != null)
            {
                btnComp.onClick.RemoveListener(ToggleDossier);
                btnComp.onClick.AddListener(ToggleDossier);
            }
        }

        if (finalSuspectsIconBtn != null) finalSuspectsIconBtn.SetActive(false);
        CheckSuspectsCountAndSwitchIcons();
    }

    private void Update()
    {
        // إغلاق ملف التحقيق فوراً عند الضغط على زر الهروب (Escape) أو زر الرجوع في الجوال/الكمبيوتر
        if (IsDossierOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (suspectDetailsPanel != null && suspectDetailsPanel.activeSelf)
                {
                    CloseDetailsPanel();
                }
                else
                {
                    CloseDossier();
                }
            }
        }
    }

    private void ResolveDossierUIReferences()
    {
        if (dossierPanel == null) dossierPanel = FindUIObject("DossierPanel", "Dossier", "SuspectsPanel", "SuspectPanel", "CaseDossierPanel");
        if (suspectDetailsPanel == null) suspectDetailsPanel = FindUIObject("SuspectDetailsPanel", "SuspectDetailPanel", "DetailsPanel");
        if (findingSummaryText == null) findingSummaryText = FindUIText("FindingSummaryText", "EvidenceFindingText");
        if (detailPortrait == null) detailPortrait = FindUIImageInRoot(suspectDetailsPanel, "Detail_Portrait", "SuspectPortrait_UI", "DetailPortrait");
        if (detailNameText == null) detailNameText = FindUITextInRoot(suspectDetailsPanel, "Detail_NameText", "DetailName", "DetailNameText");
        if (detailRoleText == null) detailRoleText = FindUITextInRoot(suspectDetailsPanel, "Detail_RoleText", "DetailRole", "DetailRoleText");
        if (detailBloodText == null) detailBloodText = FindUITextInRoot(suspectDetailsPanel, "Detail_BloodText", "DetailBlood", "DetailBloodText");
        if (detailAlibiText == null) detailAlibiText = FindUITextInRoot(suspectDetailsPanel, "Detail_AlibiText", "DetailAlibi", "DetailAlibiText");
        if (generalDossierIconBtn == null) generalDossierIconBtn = FindUIObject("Btn_Dossier_Mobile");
        if (finalSuspectsIconBtn == null) finalSuspectsIconBtn = FindUIObject("Btn_FinalSuspects");
        if (interrogationPromptUI == null) interrogationPromptUI = FindUIObject("InterrogationPrompt");

        // البحث الدقيق أولاً داخل dossierPanel لتجنب جلب أزرار إغلاق أخرى (مثل CloseBtn الخاص بلوحة الاستجواب)
        if (closeDossierBtn == null && dossierPanel != null)
        {
            Button[] buttonsInDossier = dossierPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttonsInDossier)
            {
                if (b != null && (b.name.Equals("CloseDossierBtn", System.StringComparison.OrdinalIgnoreCase) ||
                                  b.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    closeDossierBtn = b;
                    break;
                }
            }
        }

        if (closeDossierBtn == null) closeDossierBtn = FindUIButton("CloseDossierBtn", "CloseDossier");
        if (backToGridBtn == null) backToGridBtn = FindUIButton("BackToGridBtn", "Back", "CloseDetails");
        if (manualEliminateBtn == null) manualEliminateBtn = FindUIButton("ManualEliminateBtn", "Eliminate", "Accuse");

        if (cardSlots == null || cardSlots.Length == 0 || !HasCardSlot(cardSlots))
        {
            cardSlots = Object.FindObjectsByType<SuspectCardUI>(FindObjectsInactive.Include);
            System.Array.Sort(cardSlots, (a, b) =>
                string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        }

        // Make the dossier cover the actual screen
        if (dossierPanel != null)
        {
            RectTransform rect = dossierPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            Canvas canvas = dossierPanel.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 500;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = Screen.height > 0 && (float)Screen.width / Screen.height < 1.5f ? 0f : 0.5f;
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    private static bool HasCardSlot(SuspectCardUI[] slots)
    {
        if (slots == null || slots.Length == 0) return false;
        foreach (SuspectCardUI slot in slots)
            if (slot != null) return true;
        return false;
    }

    private static GameObject FindUIObject(params string[] objectNames)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            foreach (string name in objectNames)
            {
                if (t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return t.gameObject;
            }
        }
        return null;
    }

    private static Button FindUIButton(params string[] names)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            foreach (string name in names)
                if (button.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;
        }
        return null;
    }

    private static TMP_Text FindUIText(params string[] names)
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            foreach (string name in names)
                if (text.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return text;
        return null;
    }

    private static Image FindUIImage(params string[] names)
    {
        foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            foreach (string name in names)
                if (image.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return image;
        return null;
    }

    private static TMP_Text FindUITextInRoot(GameObject root, params string[] names)
    {
        if (root == null) return null;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            foreach (string name in names)
                if (text.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return text;
        return FindUIText(names);
    }

    private static Image FindUIImageInRoot(GameObject root, params string[] names)
    {
        if (root == null) return null;
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            foreach (string name in names)
                if (image.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return image;
        return FindUIImage(names);
    }

    public void InitializeCards()
    {
        if (cardSlots == null) return;
        for (int i = 0; i < cardSlots.Length && i < suspects.Count; i++)
        {
            if (cardSlots[i] != null && suspects[i] != null)
            {
                cardSlots[i].Setup(suspects[i], this);
            }
        }
    }

    public void ProcessEvidenceCollected(Evidence evidence)
    {
        collectedEvidencesCount++;

        if (findingSummaryText != null && evidence != null)
        {
            findingSummaryText.text = $"<b>EVIDENCE COLLECTED ({collectedEvidencesCount}/{totalEvidencesRequired}):</b> {evidence.evidenceName}\n<color=#FFD54F>{evidence.eliminationFinding}</color>";
        }

        CloseDetailsPanel();
        if (SeparateInterrogationManager.Instance == null || SeparateInterrogationManager.Instance.AreBothSuspectsCompleted)
        {
            OpenDossier();
        }

        // استبعاد المشتبه به تلقائياً المرتبط بهذا الدليل إن وُجد
        if (evidence != null && !string.IsNullOrEmpty(evidence.eliminatesSuspectID))
        {
            EliminateSuspectByID(evidence.eliminatesSuspectID, evidence.eliminationFinding);
        }

        CheckSuspectsCountAndSwitchIcons();

        // التحقق مما إذا جمع اللاعب كافة الأدلة الـ 5 لفتح مرحلة المواجهة النهائية
        if (collectedEvidencesCount >= totalEvidencesRequired)
        {
            UnlockFinalConfrontation();
        }
    }

    public void EliminateSuspectByID(string suspectID, string reason = "")
    {
        if (string.IsNullOrWhiteSpace(suspectID)) return;

        string targetKey = suspectID.Trim().ToUpperInvariant();
        bool found = false;

        foreach (var s in suspects)
        {
            if (s == null) continue;

            string sID = (s.suspectID ?? "").Trim().ToUpperInvariant();
            string sName = (s.suspectName ?? "").Trim().ToUpperInvariant();

            // مطابقة إما بالـ ID أو بالاسم (مثل SUS_SARAH أو SARAH)
            if (sID == targetKey || sName.Contains(targetKey) || targetKey.Contains(sName))
            {
                if (sID == "SUS_NORA" || sID == "SUS_EVAN" || sID == "SUS_VICTOR" || sID == "SUS_ELENA" ||
                    sName.Contains("NORA") || sName.Contains("EVAN") || sName.Contains("VICTOR") || sName.Contains("ELENA"))
                {
                    Debug.LogWarning($"[SECURITY] Cannot eliminate core suspect {s.suspectName}!");
                    return;
                }

                s.isEliminated = true;
                if (!string.IsNullOrEmpty(reason))
                {
                    s.eliminationReason = reason;
                }
                found = true;
                Debug.Log($"<color=orange>[ELIMINATED]</color> Suspect eliminated successfully: <b>{s.suspectName}</b> ({s.suspectID}) | Reason: {reason}");
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[CaseDossierManager] Could not find suspect matching ID '{suspectID}' to eliminate.");
        }

        RefreshCards();
    }

    public void OpenSuspectDetails(SuspectProfile suspect)
    {
        if (suspect == null) return;
        currentSelectedSuspect = suspect;

        SetMobileControls(false);

        if (suspectDetailsPanel != null) 
        {
            suspectDetailsPanel.SetActive(true);
            suspectDetailsPanel.transform.SetAsLastSibling();
        }

        // تنسيق وترتيب عناصر الشاشة ديناميكياً لتظهر كملف تحقيق جنائي فخم ومتناسق
        ApplyPolishedDossierLayout(suspect);

        // التأكد من ربط زر الرجوع (Back Button)
        if (backToGridBtn != null)
        {
            backToGridBtn.onClick.RemoveListener(CloseDetailsPanel);
            backToGridBtn.onClick.AddListener(CloseDetailsPanel);
        }

        // منع استبعاد إيفان ونورا نهائياً يدوياً (لأنهما المشتبه بهما الأساسيان للمواجهة الأخيرة)
        bool isFinalSuspect = (suspect.suspectID == "SUS_NORA" || suspect.suspectID == "SUS_EVAN");

        if (manualEliminateBtn != null)
        {
            TMP_Text btnText = manualEliminateBtn.GetComponentInChildren<TMP_Text>();

            if (suspect.isEliminated)
            {
                manualEliminateBtn.interactable = false;
                if (btnText != null) btnText.text = "ALREADY ELIMINATED";
            }
            else if (isFinalSuspect)
            {
                manualEliminateBtn.interactable = false;
                if (btnText != null) btnText.text = "FINAL SUSPECT (LOCKED)";
            }
            else
            {
                manualEliminateBtn.interactable = true;
                if (btnText != null) btnText.text = "ELIMINATE SUSPECT";
            }
        }
    }

    private void ApplyPolishedDossierLayout(SuspectProfile suspect)
    {
        // 1. تلوين وضبط نافذة تفاصيل المشتبه به بكامل الشاشة مع خلفية سينمائية داكنة
        if (suspectDetailsPanel != null)
        {
            RectTransform pRect = suspectDetailsPanel.GetComponent<RectTransform>();
            if (pRect != null)
            {
                pRect.anchorMin = Vector2.zero;
                pRect.anchorMax = Vector2.one;
                pRect.offsetMin = Vector2.zero;
                pRect.offsetMax = Vector2.zero;
            }

            Image panelImg = suspectDetailsPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.04f, 0.05f, 0.08f, 0.96f); // Dark Slate Onyx
            }
        }

        // 2. تنسيق الصورة الشخصية (Portrait) ووضعها في المنتصف المائل يساراً بتناسق ممتاز
        if (detailPortrait != null)
        {
            detailPortrait.sprite = suspect.portrait;
            detailPortrait.preserveAspect = true;

            RectTransform portRect = detailPortrait.GetComponent<RectTransform>();
            if (portRect != null)
            {
                portRect.anchorMin = new Vector2(0.5f, 0.5f);
                portRect.anchorMax = new Vector2(0.5f, 0.5f);
                portRect.pivot = new Vector2(0.5f, 0.5f);
                portRect.anchoredPosition = new Vector2(-280f, 0f);
                portRect.sizeDelta = new Vector2(280f, 360f);
            }
        }

        // 3. تنسيق اسم المتهم
        if (detailNameText != null)
        {
            detailNameText.text = suspect.isEliminated
                ? $"<color=#E53935><s>{suspect.suspectName}</s></color> <size=65%><color=#EF5350>[ELIMINATED]</color></size>"
                : $"<color=#FFD54F><b>{suspect.suspectName}</b></color>";

            detailNameText.fontSize = 36f;
            detailNameText.alignment = TextAlignmentOptions.Left;

            RectTransform nameRect = detailNameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.anchorMin = new Vector2(0.5f, 0.5f);
                nameRect.anchorMax = new Vector2(0.5f, 0.5f);
                nameRect.pivot = new Vector2(0f, 0.5f);
                nameRect.anchoredPosition = new Vector2(-100f, 150f);
                nameRect.sizeDelta = new Vector2(650f, 50f);
            }
        }

        // 4. تنسيق الوظيفة والدور (Role)
        if (detailRoleText != null)
        {
            detailRoleText.text = $"<color=#78909C><b>ROLE:</b></color> <color=#ECEFF1>{suspect.roleOrTitle}</color>";
            detailRoleText.fontSize = 24f;
            detailRoleText.alignment = TextAlignmentOptions.Left;

            RectTransform roleRect = detailRoleText.GetComponent<RectTransform>();
            if (roleRect != null)
            {
                roleRect.anchorMin = new Vector2(0.5f, 0.5f);
                roleRect.anchorMax = new Vector2(0.5f, 0.5f);
                roleRect.pivot = new Vector2(0f, 0.5f);
                roleRect.anchoredPosition = new Vector2(-100f, 95f);
                roleRect.sizeDelta = new Vector2(650f, 40f);
            }
        }

        // 5. تنسيق فصيلة الدم (Blood Type)
        if (detailBloodText != null)
        {
            detailBloodText.text = $"<color=#78909C><b>BLOOD TYPE:</b></color> <color=#FF5252><b>{suspect.bloodType}</b></color>";
            detailBloodText.fontSize = 24f;
            detailBloodText.alignment = TextAlignmentOptions.Left;

            RectTransform bloodRect = detailBloodText.GetComponent<RectTransform>();
            if (bloodRect != null)
            {
                bloodRect.anchorMin = new Vector2(0.5f, 0.5f);
                bloodRect.anchorMax = new Vector2(0.5f, 0.5f);
                bloodRect.pivot = new Vector2(0f, 0.5f);
                bloodRect.anchoredPosition = new Vector2(-100f, 45f);
                bloodRect.sizeDelta = new Vector2(650f, 40f);
            }
        }

        // 6. تنسيق الأقوال وحجة الغياب (Alibi) مع سبب الاستبعاد المنظم
        if (detailAlibiText != null)
        {
            string alibiContent = !string.IsNullOrEmpty(suspect.alibi) ? suspect.alibi : "No statement on file.";
            string statusBlock = "";

            if (suspect.isEliminated)
            {
                string reason = !string.IsNullOrEmpty(suspect.eliminationReason)
                    ? suspect.eliminationReason
                    : "Cleared of suspicion by forensics analysis.";

                statusBlock = $"\n\n<color=#EF5350><b>DISPROVED FINDING / ALIBI CRACKED:</b></color>\n<color=#FFCDD2>{reason}</color>";
            }

            detailAlibiText.text = $"<color=#78909C><b>RECORDED STATEMENT & ALIBI:</b></color>\n<color=#CFD8DC>\"{alibiContent}\"</color>{statusBlock}";
            detailAlibiText.fontSize = 20f;
            detailAlibiText.lineSpacing = 16f;
            detailAlibiText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform alibiRect = detailAlibiText.GetComponent<RectTransform>();
            if (alibiRect != null)
            {
                alibiRect.anchorMin = new Vector2(0.5f, 0.5f);
                alibiRect.anchorMax = new Vector2(0.5f, 0.5f);
                alibiRect.pivot = new Vector2(0f, 1f);
                alibiRect.anchoredPosition = new Vector2(-100f, 15f);
                alibiRect.sizeDelta = new Vector2(650f, 220f);
            }
        }

        // 7. تنسيق وتثبيت مكان ختم الاستبعاد المائل (Eliminated Stamp) فوق صورة المتهم بدقة
        GameObject stampObj = detailEliminatedStamp != null ? detailEliminatedStamp : (manualEliminateBtn != null ? manualEliminateBtn.gameObject : null);
        if (stampObj != null)
        {
            stampObj.SetActive(suspect.isEliminated);
            if (suspect.isEliminated)
            {
                RectTransform stampRect = stampObj.GetComponent<RectTransform>();
                if (stampRect != null)
                {
                    stampRect.anchorMin = new Vector2(0.5f, 0.5f);
                    stampRect.anchorMax = new Vector2(0.5f, 0.5f);
                    stampRect.pivot = new Vector2(0.5f, 0.5f);
                    stampRect.anchoredPosition = new Vector2(-280f, 0f); // متطابق تماماً في منتصف الصورة
                    stampRect.sizeDelta = new Vector2(260f, 260f);
                    stampRect.localRotation = Quaternion.Euler(0f, 0f, -18f);
                }
            }
        }

        // 8. زر الرجوع (Back Button) داخل المنطقة الآمنة للشاشة
        if (backToGridBtn != null)
        {
            RectTransform backRect = backToGridBtn.GetComponent<RectTransform>();
            if (backRect != null)
            {
                backRect.anchorMin = new Vector2(1f, 1f);
                backRect.anchorMax = new Vector2(1f, 1f);
                backRect.pivot = new Vector2(1f, 1f);
                backRect.anchoredPosition = new Vector2(-60f, -40f);
                backRect.sizeDelta = new Vector2(60f, 60f);
            }
        }
    }

    private void ApplyPolishedDossierMainLayout()
    {
        // 1. تنسيق لوحة DossierPanel بالكامل مع خلفية فخمة
        if (dossierPanel != null)
        {
            Canvas parentCanvas = dossierPanel.GetComponentInParent<Canvas>(true);
            RectTransform pRect = dossierPanel.GetComponent<RectTransform>();
            if (pRect != null)
            {
                if (parentCanvas != null && dossierPanel.transform.parent != parentCanvas.transform)
                    dossierPanel.transform.SetParent(parentCanvas.transform, false);
                pRect.anchorMin = Vector2.zero;
                pRect.anchorMax = Vector2.one;
                pRect.pivot = new Vector2(0.5f, 0.5f);
                pRect.offsetMin = Vector2.zero;
                pRect.offsetMax = Vector2.zero;
                pRect.anchoredPosition = Vector2.zero;
                pRect.sizeDelta = Vector2.zero;
                pRect.localRotation = Quaternion.identity;
                pRect.localScale = Vector3.one;
            }

            Image panelImg = dossierPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
            }

            Transform header = dossierPanel.transform.Find("HeaderBar");
            if (header != null)
            {
                RectTransform headerRect = header.GetComponent<RectTransform>();
                if (headerRect != null)
                {
                    headerRect.anchorMin = new Vector2(0f, 1f);
                    headerRect.anchorMax = new Vector2(1f, 1f);
                    headerRect.pivot = new Vector2(0.5f, 1f);
                    headerRect.anchoredPosition = Vector2.zero;
                    headerRect.sizeDelta = new Vector2(0f, 86f);
                }

                TMP_Text title = header.GetComponentInChildren<TMP_Text>(true);
                if (title != null)
                {
                    title.text = "<color=#00E5FF><b>CASE DOSSIER</b></color>  <color=#607D8B>//</color>  <color=#B0BEC5>COLLECTED EVIDENCE ARCHIVE</color>";
                    float titleWidth = pRect != null ? pRect.rect.width : 1920f;
                    title.fontSize = Mathf.Clamp(titleWidth * 0.013f, 17f, 25f);
                    title.alignment = TextAlignmentOptions.Left;
                    RectTransform titleRect = title.GetComponent<RectTransform>();
                    if (titleRect != null)
                    {
                        titleRect.anchorMin = new Vector2(0f, 0f);
                        titleRect.anchorMax = new Vector2(1f, 1f);
                        titleRect.offsetMin = new Vector2(32f, 0f);
                        titleRect.offsetMax = new Vector2(-100f, 0f);
                    }
                }
            }
        }

        // 2. تنسيق نص ملخص الدليل (FindingSummaryText)
        if (findingSummaryText != null)
        {
            RectTransform sumRect = findingSummaryText.GetComponent<RectTransform>();
            if (sumRect != null)
            {
                sumRect.anchorMin = new Vector2(0.5f, 1f);
                sumRect.anchorMax = new Vector2(0.5f, 1f);
                sumRect.pivot = new Vector2(0.5f, 1f);
                sumRect.anchoredPosition = new Vector2(0f, -40f);
                float dossierWidth = dossierPanel != null && dossierPanel.GetComponent<RectTransform>() != null
                    ? dossierPanel.GetComponent<RectTransform>().rect.width : 1920f;
                sumRect.sizeDelta = new Vector2(Mathf.Min(900f, dossierWidth - 64f), 100f);
            }
            findingSummaryText.alignment = TextAlignmentOptions.Center;
            findingSummaryText.fontSize = Mathf.Clamp((dossierPanel != null ? dossierPanel.GetComponent<RectTransform>().rect.width : 1920f) * 0.012f, 16f, 24f);
        }

        // 3. تنسيق زر إغلاق لوحة المتهمين (Close Button) وضمان وضوحه وسهولة لمسه
        if (closeDossierBtn == null && dossierPanel != null)
        {
            Button[] bList = dossierPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in bList)
            {
                if (b != null && (b.name.Equals("CloseDossierBtn", System.StringComparison.OrdinalIgnoreCase) ||
                                  b.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    closeDossierBtn = b;
                    break;
                }
            }
        }

        if (closeDossierBtn != null)
        {
            closeDossierBtn.gameObject.SetActive(true);
            closeDossierBtn.transform.SetAsLastSibling();
            float closeSize = 76f;
            RectTransform closeRect = closeDossierBtn.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(1f, 1f);
                closeRect.anchorMax = new Vector2(1f, 1f);
                closeRect.pivot = new Vector2(1f, 1f);
                // إبعاد الزر عن زاوية الشاشة لتفادي النوتش في الآيفون وهواتف الأندرويد، وتكبير مساحة اللمس
                float closeInset = dossierPanel != null && dossierPanel.GetComponent<RectTransform>() != null && dossierPanel.GetComponent<RectTransform>().rect.width < 1200f ? 24f : 60f;
                closeSize = dossierPanel != null && dossierPanel.GetComponent<RectTransform>() != null && dossierPanel.GetComponent<RectTransform>().rect.width < 900f ? 60f : 76f;
                closeRect.anchoredPosition = new Vector2(-closeInset, -Mathf.Max(20f, closeInset * 0.67f));
                closeRect.sizeDelta = new Vector2(closeSize, closeSize);
            }
            Image closeImage = closeDossierBtn.GetComponent<Image>();
            if (closeImage != null)
            {
                closeImage.raycastTarget = true;
                closeImage.color = new Color(0.92f, 0.18f, 0.22f, 1f);
            }
            TMP_Text closeText = closeDossierBtn.GetComponentInChildren<TMP_Text>(true);
            if (closeText != null)
            {
                closeText.raycastTarget = false;
                closeText.text = "×";
                closeText.fontSize = Mathf.Clamp(closeSize * 0.5f, 28f, 38f);
                closeText.alignment = TextAlignmentOptions.Center;
                closeText.color = Color.white;
            }

            closeDossierBtn.onClick.RemoveListener(CloseDossier);
            closeDossierBtn.onClick.AddListener(CloseDossier);
        }

        // 4. ترتيب البطاقات في صف واحد: القضيّة تحتوي خمسة مشتبهين ويجب أن
        // يظهروا جنبًا إلى جنب بدل أن تنزل البطاقة الخامسة لسطر جديد.
        if (cardSlots != null && cardSlots.Length > 0)
        {
            Transform cardParent = cardSlots[0] != null ? cardSlots[0].transform.parent : null;
            HorizontalLayoutGroup hlg = cardParent != null
                ? cardParent.GetComponent<HorizontalLayoutGroup>()
                : null;
            if (hlg != null)
                hlg.enabled = false;

            RectTransform cardsRect = cardParent as RectTransform;
            if (cardsRect != null)
            {
                int count = cardSlots.Length;
                float availableWidth = cardsRect.rect.width > 10f ? cardsRect.rect.width : 1920f;
                float availableHeight = cardsRect.rect.height > 10f ? cardsRect.rect.height : 1080f;
                bool compact = availableWidth < 1450f || availableHeight < 800f;
                int columns = count;
                float gap = compact ? 10f : 18f;
                float margin = compact ? 18f : 32f;
                float cardWidth = Mathf.Min(360f, (availableWidth - margin * 2f - gap * (columns - 1)) / columns);
                float cardHeight = Mathf.Clamp(compact ? availableHeight * 0.30f : availableHeight * 0.36f, 180f, 300f);
                float contentTop = -Mathf.Max(150f, availableHeight * 0.20f);
                int rows = Mathf.CeilToInt(count / (float)columns);
                float totalHeight = rows * cardHeight + Mathf.Max(0, rows - 1) * gap;
                float startX = -(columns - 1) * (cardWidth + gap) * 0.5f;
                float startY = contentTop - cardHeight * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    if (cardSlots[i] == null) continue;
                    RectTransform cardRect = cardSlots[i].GetComponent<RectTransform>();
                    if (cardRect != null)
                    {
                        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                        cardRect.pivot = new Vector2(0.5f, 0.5f);
                        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
                        int row = i / columns;
                        int column = i % columns;
                        cardRect.anchoredPosition = new Vector2(startX + column * (cardWidth + gap), startY - row * (cardHeight + gap) + totalHeight * 0.5f);
                    }
                }
            }
        }
    }

    public void EliminateCurrentSuspect()
    {
        if (currentSelectedSuspect != null)
        {
            // حماية صارمة لمنع استبعاد المشتبه بهم الأساسيين (إيفان ونورا في القضية 1، وفيكتور وإيلينا في القضية 2)
            string selID = currentSelectedSuspect.suspectID ?? "";
            string selName = currentSelectedSuspect.suspectName ?? "";
            if (selID == "SUS_NORA" || selID == "SUS_EVAN" || selID == "SUS_VICTOR" || selID == "SUS_ELENA" ||
                selName.ToUpper().Contains("NORA") || selName.ToUpper().Contains("EVAN") ||
                selName.ToUpper().Contains("VICTOR") || selName.ToUpper().Contains("ELENA"))
            {
                Debug.LogWarning($"[SECURITY] Cannot eliminate core suspect {selName}! They are part of the final confrontation.");
                CloseDetailsPanel();
                return;
            }

            currentSelectedSuspect.isEliminated = true;
            RefreshCards();
            CheckSuspectsCountAndSwitchIcons();
            CloseDetailsPanel();
        }
    }

    // تُستدعى كلما استُبعد متهم بعد حل سؤال أي دليل
    public void CheckSuspectsCountAndSwitchIcons()
    {
        // الأيقونة العامة تبقى ظاهرة دائماً ويمكن فتحها في أي وقت.
        if (SeparateInterrogationManager.Instance != null && !SeparateInterrogationManager.Instance.AreBothSuspectsCompleted)
        {
            if (generalDossierIconBtn != null) generalDossierIconBtn.SetActive(true);
            if (finalSuspectsIconBtn != null) finalSuspectsIconBtn.SetActive(false);
            return;
        }

        // بعد إنهاء محادثة المتهمين الاثنين ننتقل مباشرة إلى صفحة المتهمين
        // الأساسيين. لا نعتمد على عدد البطاقات غير المستبعدة هنا، لأن
        // المتهمين الأساسيين (Evan/Nora أو Victor/Elena) لا يتم استبعادهما.
        if (SeparateInterrogationManager.Instance != null &&
            SeparateInterrogationManager.Instance.AreBothSuspectsCompleted &&
            suspects.Count > 0)
        {
            Debug.Log("<color=yellow>[TRANSITION] Both interrogations completed! Final Suspects Phase.</color>");

            // إظهار أيقونة المتهمين الأساسيين الحاسمة في الـ HUD إن وُجدت
            if (finalSuspectsIconBtn != null)
            {
                if (generalDossierIconBtn != null) generalDossierIconBtn.SetActive(true);
                finalSuspectsIconBtn.SetActive(true);
            }
        }
    }

    public void RefreshCards()
    {
        if (cardSlots == null) return;
        for (int i = 0; i < cardSlots.Length && i < suspects.Count; i++)
        {
            if (cardSlots[i] != null)
            {
                cardSlots[i].UpdateStatus();
            }
        }
    }

    private void UnlockFinalConfrontation()
    {
        Debug.Log("<color=green>[CASE READY] All 5 evidences collected! Final confrontation unlocked between the 2 remaining suspects.</color>");
        if (interrogationPromptUI != null) interrogationPromptUI.SetActive(true);
        if (confrontBtn != null)
        {
            confrontBtn.interactable = true;
            confrontBtn.onClick.RemoveAllListeners();
            confrontBtn.onClick.AddListener(() =>
            {
                CloseDossier();
                if (SeparateInterrogationManager.Instance != null)
                {
                    SeparateInterrogationManager.Instance.OpenLogPanel();
                }
            });
        }
    }

    public void UnlockDossierAfterInterrogation()
    {
        ResolveDossierUIReferences();
        if (generalDossierIconBtn != null)
        {
            generalDossierIconBtn.SetActive(true);
        }
        CheckSuspectsCountAndSwitchIcons();
        Debug.Log("<color=green>[CaseDossierManager] Dossier unlocked after completing both suspect interrogations!</color>");
    }

    public void OpenDossier()
    {
        // Keep the HUD icon visible, but lock the dossier contents until
        // both Case 2 suspects have completed their interrogations.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02") &&
            (SeparateInterrogationManager.Instance == null ||
             !SeparateInterrogationManager.Instance.AreBothSuspectsCompleted))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowOnScreenNotification("CASE DOSSIER LOCKED // COMPLETE BOTH INTERROGATIONS FIRST", 3.5f);
            return;
        }

        ResolveDossierUIReferences();
        if (dossierPanel == null)
            dossierPanel = FindUIObject("DossierPanel", "Dossier", "SuspectsPanel", "SuspectPanel", "CaseDossierPanel");

        if (dossierPanel == null)
        {
            Debug.LogError("[CaseDossierManager] DossierPanel was not found in this scene!");
            return;
        }

        SetMobileControls(false);

        // إغلاق الحقيبة إن كانت مفتوحة لمنع تداخل اللوحات
        if (UIManager.Instance != null && UIManager.Instance.inventoryPanel != null && UIManager.Instance.inventoryPanel.activeSelf)
        {
            UIManager.Instance.ToggleInventory();
        }

        Canvas parentCanvas = dossierPanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.gameObject.SetActive(true);
            parentCanvas.enabled = true;
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 500;
        }

        ApplyPolishedDossierMainLayout();
        RefreshCards();

        dossierPanel.SetActive(true);
        dossierPanel.transform.SetAsLastSibling();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (closeDossierBtn != null)
        {
            closeDossierBtn.onClick.RemoveAllListeners();
            closeDossierBtn.onClick.AddListener(CloseDossier);
        }

        // ربط أي أزرار إغلاق إضافية داخل dossierPanel لضمان إغلاق اللوحة تحت أي ظرف
        if (dossierPanel != null)
        {
            Button[] allButtons = dossierPanel.GetComponentsInChildren<Button>(true);
            foreach (Button b in allButtons)
            {
                if (b != null && b.name.IndexOf("Close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.onClick.RemoveListener(CloseDossier);
                    b.onClick.AddListener(CloseDossier);
                }
            }
        }

        if (suspectDetailsPanel != null) suspectDetailsPanel.SetActive(false);
    }

    public void CloseDossier()
    {
        if (dossierPanel != null)
        {
            dossierPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (suspectDetailsPanel != null) suspectDetailsPanel.SetActive(false);
        currentSelectedSuspect = null;
        SetMobileControls(true);
    }

    public void ToggleDossier()
    {
        ResolveDossierUIReferences();
        if (dossierPanel != null)
        {
            if (dossierPanel.activeSelf) CloseDossier();
            else OpenDossier();
        }
    }

    public void CloseDetailsPanel()
    {
        if (suspectDetailsPanel != null)
        {
            suspectDetailsPanel.SetActive(false);
        }
        currentSelectedSuspect = null;
        RefreshCards();

        // إذا كان ملف القضية مغلقاً أيضاً، نعيد أزرار الجوال
        if (dossierPanel == null || !dossierPanel.activeSelf)
        {
            SetMobileControls(true);
        }
    }

    private void SetMobileControls(bool active)
    {
        if (mobileControls == null)
        {
            mobileControls = GameObject.Find("MobileControls");
        }

        if (mobileControls != null)
        {
            mobileControls.SetActive(active);
        }

        // إخفاء أو إظهار أزرار الجوال عبر UIManager أيضاً
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(active);
        }
    }
}
