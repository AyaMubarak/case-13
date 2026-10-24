using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [Header("Evidence Video Replay & Suspect Outcome")]
    public Button replayEvidenceVideoBtn;
    public TextMeshProUGUI evidenceEliminatedSuspectText;

    [Header("End Game Screen (Case Closed)")]
    public GameObject endGamePanel;
    public TextMeshProUGUI timeTakenText;
    public TextMeshProUGUI evidenceScoreText;
    public TextMeshProUGUI finalGradeText;

    [Header("Settings Screen")]
    public GameObject settingsPanel;

    private float gameStartTime;
    private bool isGameActive = true;

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

        // إغلاق أي لوحات قديمة قد تعترض وتحجب الضغطات (Raycast Blockers) فور الاستيقاظ
        GameObject uiContainer = GameObject.Find("UI_Container");
        if (uiContainer != null)
        {
            RectTransform containerRt = uiContainer.GetComponent<RectTransform>();
            if (containerRt != null)
            {
                containerRt.anchorMin = Vector2.zero;
                containerRt.anchorMax = Vector2.one;
                containerRt.offsetMin = Vector2.zero;
                containerRt.offsetMax = Vector2.zero;
                containerRt.sizeDelta = Vector2.zero;
            }

            Transform interr = uiContainer.transform.Find("InterrogationPanel");
            if (interr != null)
            {
                interr.gameObject.SetActive(false);
            }
        }

        Canvas mainCanvas = GetMainCanvas();
        if (mainCanvas != null && mainCanvas.GetComponent<ResponsiveCanvasLayout>() == null)
        {
            mainCanvas.gameObject.AddComponent<ResponsiveCanvasLayout>();
        }
    }

    public Canvas GetMainCanvas()
    {
        if (inGameHUD != null)
        {
            Canvas c = inGameHUD.GetComponentInParent<Canvas>(true);
            if (c != null) return c;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c.gameObject.name != "MobileControls" && (inventoryPanel == null || c.gameObject != inventoryPanel) && (endGamePanel == null || c.gameObject != endGamePanel))
            {
                return c;
            }
        }
        return FindAnyObjectByType<Canvas>();
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

        gameStartTime = Time.time;
        isGameActive = true;

        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (inGameHUD != null)
        {
            inGameHUD.SetActive(true);
        }

        if (mobileControls != null)
        {
            mobileControls.SetActive(IsTouchDevice());
        }

        // إصلاح أبعاد حاوية UI_Container تلقائياً لضمان استلام النقرات للأزرار
        GameObject uiContainer = GameObject.Find("UI_Container");
        if (uiContainer != null)
        {
            RectTransform containerRt = uiContainer.GetComponent<RectTransform>();
            if (containerRt != null)
            {
                containerRt.anchorMin = Vector2.zero;
                containerRt.anchorMax = Vector2.one;
                containerRt.offsetMin = Vector2.zero;
                containerRt.offsetMax = Vector2.zero;
            }

            Transform interr = uiContainer.transform.Find("InterrogationPanel");
            if (interr != null)
            {
                interr.gameObject.SetActive(false);
            }
        }

        // إخفاء لوحات الكاتسين إذا لم تكن تعمل
        GameObject cutscenePnl = GameObject.Find("CutscenePanel");
        if (cutscenePnl != null && !CaseIntroCutscene.IsPlaying)
        {
            cutscenePnl.SetActive(false);
        }

        // البحث التلقائي عن أزرار الجوال إن لم تكن مربوطة في الـ Inspector
        if (mobileInventoryBtn == null)
        {
            var foundInv = GameObject.Find("MobileInventoryBtn");
            if (foundInv != null) mobileInventoryBtn = foundInv;
        }

        if (mobileDossierBtn == null)
        {
            var foundDos = GameObject.Find("Btn_Dossier_Mobile");
            if (foundDos != null) mobileDossierBtn = foundDos;
        }

        // تفريغ مساحة علوية لمنع TouchLookArea من حجب أزرار الواجهة في أعلى الشاشة
        GameObject touchLookObj = GameObject.Find("TouchLookArea");
        if (touchLookObj != null)
        {
            RectTransform lookRt = touchLookObj.GetComponent<RectTransform>();
            if (lookRt != null)
            {
                // إبعاد منطقة اللمس الخاصة بالكاميرا عن الشريط العلوي للحقيبة والملفات
                lookRt.offsetMax = new Vector2(lookRt.offsetMax.x, -110f);
            }
        }

        // التأكد من وجود inGameHUD
        if (inGameHUD == null)
        {
            inGameHUD = GameObject.Find("InGameHUD");
        }

        // تحديد الحاوية الشاملة للشاشة (Main Canvas أو UI_Container) لضمان تمركز الأزرار في زاوية الشاشة الحقيقية
        Transform hudContainer = (GetMainCanvas() != null) ? GetMainCanvas().transform : (uiContainer != null ? uiContainer.transform : null);

        // ضبط وتثبيت زر الحقيبة في الزاوية العلوية اليمنى للشاشة
        if (mobileInventoryBtn != null)
        {
            // إزالة أي Canvas فرعي كان مضافاً حتى لا يعلو فوق شاشات الفيديو
            Canvas oldInvCanvas = mobileInventoryBtn.GetComponent<Canvas>();
            if (oldInvCanvas != null) Destroy(oldInvCanvas);
            GraphicRaycaster oldInvRay = mobileInventoryBtn.GetComponent<GraphicRaycaster>();
            if (oldInvRay != null) Destroy(oldInvRay);

            if (hudContainer != null && mobileInventoryBtn.transform.parent != hudContainer)
            {
                mobileInventoryBtn.transform.SetParent(hudContainer, false);
            }
            mobileInventoryBtn.SetActive(true);
            mobileInventoryBtn.transform.SetAsLastSibling();

            RectTransform invRt = mobileInventoryBtn.GetComponent<RectTransform>();
            if (invRt != null)
            {
                invRt.anchorMin = new Vector2(1f, 1f);
                invRt.anchorMax = new Vector2(1f, 1f);
                invRt.pivot = new Vector2(1f, 1f);
                invRt.anchoredPosition = new Vector2(-25f, -25f); // الزاوية العلوية اليمنى للشاشة
                invRt.sizeDelta = new Vector2(70f, 70f);
                invRt.localScale = Vector3.one;
            }

            Button invBtnComp = mobileInventoryBtn.GetComponent<Button>();
            if (invBtnComp != null)
            {
                invBtnComp.onClick.RemoveAllListeners();
                invBtnComp.onClick.AddListener(ToggleInventory);
            }

            // إضافة EventTrigger لضمان التقاط أي نقرة بالماوس أو اللمس فوراً
            var trigger = mobileInventoryBtn.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = mobileInventoryBtn.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            trigger.triggers.Clear();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { ToggleInventory(); });
            trigger.triggers.Add(entry);
        }

        // ضبط زر ملف المتهمين بجانب زر الحقيبة في الزاوية العلوية اليمنى
        if (mobileDossierBtn != null)
        {
            Canvas oldDosCanvas = mobileDossierBtn.GetComponent<Canvas>();
            if (oldDosCanvas != null) Destroy(oldDosCanvas);
            GraphicRaycaster oldDosRay = mobileDossierBtn.GetComponent<GraphicRaycaster>();
            if (oldDosRay != null) Destroy(oldDosRay);

            if (hudContainer != null && mobileDossierBtn.transform.parent != hudContainer)
            {
                mobileDossierBtn.transform.SetParent(hudContainer, false);
            }
            mobileDossierBtn.SetActive(true);
            mobileDossierBtn.transform.SetAsLastSibling();

            RectTransform dosRt = mobileDossierBtn.GetComponent<RectTransform>();
            if (dosRt != null)
            {
                dosRt.anchorMin = new Vector2(1f, 1f);
                dosRt.anchorMax = new Vector2(1f, 1f);
                dosRt.pivot = new Vector2(1f, 1f);
                dosRt.anchoredPosition = new Vector2(-105f, -25f); // بجانب زر الحقيبة مباشرة
                dosRt.sizeDelta = new Vector2(70f, 70f);
                dosRt.localScale = Vector3.one;
            }

            Button dossierBtnComp = mobileDossierBtn.GetComponent<Button>();
            if (dossierBtnComp != null)
            {
                dossierBtnComp.onClick.RemoveAllListeners();
                dossierBtnComp.onClick.AddListener(OnMobileDossierButtonClicked);
            }

            var trigger = mobileDossierBtn.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = mobileDossierBtn.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            trigger.triggers.Clear();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnMobileDossierButtonClicked(); });
            trigger.triggers.Add(entry);
        }
    }

    private void Update()
    {
        // إخفاء فوري وتام لأزرار الحقيبة والملفات أثناء تشغيل أي فيديو أو كاتسين سينمائي
        bool isCutsceneOrVideo = CaseIntroCutscene.IsPlaying || EvidenceCutsceneManager.IsQuizOrVideoActive;
        if (isCutsceneOrVideo)
        {
            if (inGameHUD != null && inGameHUD.activeSelf) inGameHUD.SetActive(false);
            if (mobileInventoryBtn != null && mobileInventoryBtn.activeSelf) mobileInventoryBtn.SetActive(false);
            if (mobileDossierBtn != null && mobileDossierBtn.activeSelf) mobileDossierBtn.SetActive(false);
            return;
        }

        // زر Tab أو I للحقيبة
        if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame))
        {
            ToggleInventory();
        }

        // زر B لوحة ملفات واستجواب المتهم (Interrogation Panel)
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (isInventoryOpen) ToggleInventory();
            ToggleInterrogationDossier();
        }

        // زر Escape أو P لفتح/إغلاق الإعدادات داخل اللعبة
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
        {
            ToggleSettings();
        }
    }

    // فتح وإغلاق ملف التحقيق الصحيح
    public void ToggleInterrogationDossier()
    {
        if (!isGameActive) return; // منع الفتح إذا انتهت اللعبة

        // إغلاق الحقيبة إن كانت مفتوحة لمنع التداخل
        if (isInventoryOpen)
        {
            isInventoryOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        // إذا كان نظام DossierManager موجوداً، نفتحه أو نغلقه
        if (CaseDossierManager.Instance != null)
        {
            CaseDossierManager.Instance.ToggleDossier();
            return;
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
        Debug.Log("<color=cyan>[UI] Dossier Button Clicked via Mobile UI!</color>");
        ToggleInterrogationDossier();
    }

    public void ToggleInventory()
    {
        Debug.Log($"<color=cyan>[UI] Bag (Inventory) Button Clicked! Current isOpen: {isInventoryOpen}</color>");
        if (!isGameActive) return; // منع الفتح إذا انتهت اللعبة

        // إغلاق ملف التحقيق أو لوحة المشتبه بهم إن كانت مفتوحة لمنع حجب الشاشة
        if (CaseDossierManager.Instance != null && CaseDossierManager.Instance.IsDossierOpen)
        {
            CaseDossierManager.Instance.CloseDossier();
        }

        if (InterrogationUI.Instance != null && InterrogationUI.Instance.interrogationPanel != null && InterrogationUI.Instance.interrogationPanel.activeSelf)
        {
            InterrogationUI.Instance.CloseInterrogation();
        }
        else if (interrogationBoardPanel != null && interrogationBoardPanel.activeSelf)
        {
            interrogationBoardPanel.SetActive(false);
        }

        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel == null)
        {
            var found = GameObject.Find("InventoryPanel");
            if (found != null) inventoryPanel = found;
        }

        if (inventoryPanel != null)
        {
            Canvas mainCanvas = GetMainCanvas();
            if (mainCanvas != null && inventoryPanel.transform.parent != mainCanvas.transform && inventoryPanel != mainCanvas.gameObject)
            {
                inventoryPanel.transform.SetParent(mainCanvas.transform, false);
            }

            inventoryPanel.SetActive(isInventoryOpen);
            if (isInventoryOpen)
            {
                inventoryPanel.transform.SetAsLastSibling();
                ApplyPolishedInventoryLayout();
                RefreshEvidenceList();
            }
        }
        if (inGameHUD != null) inGameHUD.SetActive(!isInventoryOpen);

        // إخفاء أزرار الجوال (الجويستيك واليد) بالكامل عند فتح الحقيبة
        SetMobileControlsActive(!isInventoryOpen);

        if (evidenceCounterText != null && evidenceCounterText.transform.parent != null)
        {
            evidenceCounterText.transform.parent.gameObject.SetActive(!isInventoryOpen);
        }

        if (isInventoryOpen)
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
            RectTransform btnRt = mobileInventoryBtn.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(1f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(1f, 1f);
                btnRt.anchoredPosition = new Vector2(-30f, -30f);
                btnRt.sizeDelta = new Vector2(70f, 70f);
                btnRt.localScale = Vector3.one;
            }
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
            RectTransform btnRt = mobileDossierBtn.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(1f, 1f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.pivot = new Vector2(1f, 1f);
                btnRt.anchoredPosition = new Vector2(-115f, -30f);
                btnRt.sizeDelta = new Vector2(70f, 70f);
                btnRt.localScale = Vector3.one;
            }
        }
    }

    public void SetMobileControlsActive(bool isActive)
    {
        if (mobileControls == null)
        {
            mobileControls = GameObject.Find("MobileControls");
        }

        if (mobileControls != null)
        {
            // عند الإخفاء: إخفاء قاطع دائماً بغض النظر عن المنصة
            if (!isActive)
            {
                mobileControls.SetActive(false);
            }
            else if (IsTouchDevice())
            {
                mobileControls.SetActive(true);
            }
        }

        // عند إخفاء عناصر التحكم (مثل أثناء الفيديوهات أو القوائم)، إخفاء أزرار الحقيبة والملفات أيضاً
        if (!isActive)
        {
            if (mobileInventoryBtn != null) mobileInventoryBtn.SetActive(false);
            if (mobileDossierBtn != null) mobileDossierBtn.SetActive(false);
        }
        else if (isGameActive && !CaseIntroCutscene.IsPlaying && !EvidenceCutsceneManager.IsQuizOrVideoActive)
        {
            if (mobileInventoryBtn != null) mobileInventoryBtn.SetActive(true);
            if (mobileDossierBtn != null) mobileDossierBtn.SetActive(true);
        }
    }

    public void RefreshEvidenceList()
    {
        if (evidenceListContainer == null && inventoryPanel != null)
        {
            var found = inventoryPanel.transform.Find("EvidenceListContainer");
            if (found != null) evidenceListContainer = found;
        }

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

    private void ApplyPolishedInventoryLayout()
    {
        if (inventoryPanel == null) return;

        // 1. إعادة ضبط حجم وموضع لوحة الحقيبة الرئيسية لتغطي الشاشة بنسب متناسقة وأنيقة
        RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one;
            panelRect.anchorMin = new Vector2(0.04f, 0.05f);
            panelRect.anchorMax = new Vector2(0.96f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        // 2. خلفية الحقيبة الأساسية بلون داكن فخم مع لمعة كحلية
        Image panelImg = inventoryPanel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
        }

        // 3. ضبط شريط العنوان العلوي (HeaderBar)
        Transform headerBar = inventoryPanel.transform.Find("HeaderBar");
        if (headerBar != null)
        {
            RectTransform headerRect = headerBar.GetComponent<RectTransform>();
            if (headerRect != null)
            {
                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = new Vector2(1f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = Vector2.zero;
                headerRect.sizeDelta = new Vector2(0f, 75f);
            }

            Image headerImg = headerBar.GetComponent<Image>();
            if (headerImg != null)
            {
                headerImg.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);
            }

            // زر إغلاق الحقيبة داخل شريط العنوان (Close Button)
            Transform closeBtnTr = headerBar.Find("Btn_CloseInventory");
            GameObject closeBtnObj = closeBtnTr != null ? closeBtnTr.gameObject : null;
            if (closeBtnObj == null)
            {
                closeBtnObj = new GameObject("Btn_CloseInventory", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                closeBtnObj.transform.SetParent(headerBar, false);
            }

            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-20f, 0f);
            closeRt.sizeDelta = new Vector2(45f, 45f);

            Image closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.color = new Color(0.85f, 0.25f, 0.25f, 0.9f);

            Transform closeTxtTr = closeBtnObj.transform.Find("Text");
            TextMeshProUGUI closeTxt = closeTxtTr != null ? closeTxtTr.GetComponent<TextMeshProUGUI>() : null;
            if (closeTxt == null)
            {
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(closeBtnObj.transform, false);
                closeTxt = txtObj.GetComponent<TextMeshProUGUI>();
            }
            closeTxt.text = "X";
            closeTxt.fontSize = 24f;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            RectTransform txtRt = closeTxt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            Button closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(ToggleInventory);

            Transform titleTr = headerBar.Find("TitleText");
            if (titleTr != null)
            {
                RectTransform titleRect = titleTr.GetComponent<RectTransform>();
                if (titleRect != null)
                {
                    titleRect.anchorMin = new Vector2(0f, 0f);
                    titleRect.anchorMax = new Vector2(1f, 1f);
                    titleRect.pivot = new Vector2(0f, 0.5f);
                    titleRect.anchoredPosition = new Vector2(35f, 0f);
                    titleRect.sizeDelta = new Vector2(-120f, 0f);
                }
                TextMeshProUGUI titleTMP = titleTr.GetComponent<TextMeshProUGUI>();
                if (titleTMP != null)
                {
                    titleTMP.text = "<color=#00E5FF><b>CASE DOSSIER</b></color> <color=#455A64>//</color> <color=#90A4AE>COLLECTED EVIDENCE ARCHIVE</color>";
                    titleTMP.fontSize = 24f;
                    titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }
        }

        // 4. ضبط وتوسيع قائمة الأدلة على اليسار (EvidenceListContainer)
        if (evidenceListContainer != null)
        {
            RectTransform listRect = evidenceListContainer.GetComponent<RectTransform>();
            if (listRect != null)
            {
                listRect.localScale = Vector3.one;
                listRect.anchorMin = new Vector2(0.02f, 0.04f);
                listRect.anchorMax = new Vector2(0.32f, 0.88f);
                listRect.offsetMin = Vector2.zero;
                listRect.offsetMax = Vector2.zero;
                listRect.pivot = new Vector2(0f, 1f);
            }

            VerticalLayoutGroup vlg = evidenceListContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 10f;
                vlg.padding = new RectOffset(6, 6, 8, 8);
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
            }
        }

        // 5. ضبط وتنسيق بطاقة تفاصيل الدليل (DetailsCard) في الجانب الأيمن المنظم
        Transform detailsCard = inventoryPanel.transform.Find("DetailsCard");
        if (detailsCard != null)
        {
            RectTransform cardRect = detailsCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.anchorMin = new Vector2(0.34f, 0.04f);
                cardRect.anchorMax = new Vector2(0.98f, 0.88f);
                cardRect.offsetMin = Vector2.zero;
                cardRect.offsetMax = Vector2.zero;
                cardRect.pivot = new Vector2(0.5f, 0.5f);
            }

            Image cardImg = detailsCard.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.color = new Color(0.07f, 0.10f, 0.16f, 0.92f);
            }

            // ضبط عنوان الدليل في البطاقة
            if (detailNameText != null)
            {
                RectTransform nameRect = detailNameText.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    nameRect.anchorMin = new Vector2(0f, 1f);
                    nameRect.anchorMax = new Vector2(1f, 1f);
                    nameRect.pivot = new Vector2(0f, 1f);
                    nameRect.anchoredPosition = new Vector2(35f, -25f);
                    nameRect.sizeDelta = new Vector2(-70f, 48f);
                }
            }

            // ضبط تصنيف الدليل
            if (detailTypeText != null)
            {
                RectTransform typeRect = detailTypeText.GetComponent<RectTransform>();
                if (typeRect != null)
                {
                    typeRect.anchorMin = new Vector2(0f, 1f);
                    typeRect.anchorMax = new Vector2(1f, 1f);
                    typeRect.pivot = new Vector2(0f, 1f);
                    typeRect.anchoredPosition = new Vector2(35f, -76f);
                    typeRect.sizeDelta = new Vector2(-70f, 32f);
                }
            }

            // ضبط صندوق الوصف وتحليل الأدلة الجنائية
            if (detailDescriptionText != null)
            {
                RectTransform descRect = detailDescriptionText.GetComponent<RectTransform>();
                if (descRect != null)
                {
                    descRect.anchorMin = new Vector2(0f, 0f);
                    descRect.anchorMax = new Vector2(1f, 1f);
                    descRect.pivot = new Vector2(0f, 1f);
                    descRect.anchoredPosition = new Vector2(35f, -125f);
                    descRect.sizeDelta = new Vector2(-70f, -145f);
                }
            }
        }
    }

    public void ShowEvidenceDetails(Evidence evidence)
    {
        if (evidence == null) return;

        if (detailNameText != null)
        {
            detailNameText.text = $"<color=#FFCA28><b>{evidence.evidenceName}</b></color>";
            detailNameText.fontSize = 28f;
            detailNameText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (detailTypeText != null)
        {
            string typeColor = evidence.type == EvidenceType.Physical ? "#FF5252" : (evidence.type == EvidenceType.Digital ? "#00E5FF" : "#FFD54F");
            detailTypeText.text = $"<color=#78909C><size=80%>CLASSIFICATION:</size></color> <color={typeColor}><b>[{evidence.type.ToString().ToUpper()}]</b></color>";
            detailTypeText.fontSize = 18f;
            detailTypeText.alignment = TextAlignmentOptions.TopLeft;
        }

        // تنسيق أنيق ومميز لنتائج التحليل الجنائي والاستبعاد
        string findingBlock = "";
        if (!string.IsNullOrEmpty(evidence.eliminationFinding))
        {
            findingBlock = $"<color=#00E676><b>[FORENSIC EVIDENCE FINDING]</b></color>\n" +
                           $"<color=#E8F5E9>{evidence.eliminationFinding}</color>\n\n";
        }

        string suspectBlock = "";
        if (!string.IsNullOrEmpty(evidence.eliminatesSuspectID))
        {
            string suspectName = evidence.eliminatesSuspectID.Replace("SUS_", "");
            suspectBlock = $"<color=#FF5252><b>[SUSPECT EXONERATED / ALIBI VERIFIED]</b></color>\n" +
                           $"<color=#FFCDD2><b>{suspectName}</b> has been eliminated from suspicion by physical forensics.</color>";
        }

        if (detailDescriptionText != null)
        {
            string cleanDesc = !string.IsNullOrEmpty(evidence.description) ? evidence.description.Trim() : "No report details logged.";
            detailDescriptionText.text = $"<color=#90A4AE><b>CASE NOTES & DESCRIPTION:</b></color>\n" +
                                         $"<color=#ECEFF1>\"{cleanDesc}\"</color>\n\n" +
                                         $"{findingBlock}{suspectBlock}";
            detailDescriptionText.fontSize = 20f;
            detailDescriptionText.lineSpacing = 16f;
            detailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        }

        // ربط خيار إعادة مشاهدة فيديو الدليل إن وجد
        if (replayEvidenceVideoBtn != null)
        {
            replayEvidenceVideoBtn.gameObject.SetActive(evidence.evidenceVideoClip != null);
            replayEvidenceVideoBtn.onClick.RemoveAllListeners();
            replayEvidenceVideoBtn.onClick.AddListener(() =>
            {
                if (EvidenceCutsceneManager.Instance != null && evidence.evidenceVideoClip != null)
                {
                    ToggleInventory(); // إغلاق الحقيبة مؤقتاً
                    EvidenceCutsceneManager.Instance.PlayEvidenceFlow(evidence);
                }
            });
        }
    }

    private void ClearDetails()
    {
        if (detailNameText != null) detailNameText.text = "<color=#78909C>No Evidence Selected</color>";
        if (detailTypeText != null) detailTypeText.text = "";
        if (detailDescriptionText != null) detailDescriptionText.text = "<color=#546E7A>Select a clue from the left repository to review detailed forensic analysis.</color>";
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

        if (inGameHUD != null && !inGameHUD.activeSelf && isGameActive && !isInventoryOpen)
        {
            inGameHUD.SetActive(true);
        }

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

    public void ShowEndGameScreen()
    {
        isGameActive = false;
        
        // إخفاء الـ HUD المعتاد
        if (inGameHUD != null) inGameHUD.SetActive(false);
        SetMobileControlsActive(false);

        // إغلاق أي نوافذ أخرى
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (interrogationBoardPanel != null) interrogationBoardPanel.SetActive(false);
        if (CaseDossierManager.Instance != null) CaseDossierManager.Instance.CloseDossier();

        // إنشاء أو تجهيز لوحة النهاية بتصميم فخم ومستقل برمجياً إن لم تكن مربوطة في الـ Inspector
        EnsureEndGamePanelCreated();

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            endGamePanel.transform.SetAsLastSibling();
        }

        // إظهار الماوس
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // حساب الوقت والنقاط والرتبة
        float timeSpent = Time.time - gameStartTime;
        int minutes = Mathf.FloorToInt(timeSpent / 60);
        int seconds = Mathf.FloorToInt(timeSpent % 60);

        int evCount = EvidenceManager.Instance != null ? EvidenceManager.Instance.CollectedCount : 5;

        string rankBadge = "S";
        string rankTitle = "MASTER DETECTIVE";
        string rankColor = "#FFD700"; // Gold

        if (evCount >= 5 && timeSpent <= 360)
        {
            rankBadge = "S";
            rankTitle = "MASTER DETECTIVE";
            rankColor = "#FFD700";
        }
        else if (evCount >= 4)
        {
            rankBadge = "A";
            rankTitle = "SENIOR INVESTIGATOR";
            rankColor = "#00E5FF";
        }
        else if (evCount >= 2)
        {
            rankBadge = "B";
            rankTitle = "FIELD AGENT";
            rankColor = "#69F0AE";
        }
        else
        {
            rankBadge = "C";
            rankTitle = "ROOKIE OFFICER";
            rankColor = "#B0BEC5";
        }

        int totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : (evCount * 20);

        if (timeTakenText != null) 
            timeTakenText.text = $"<color=#78909C>INVESTIGATION TIME</color>\n<size=125%><color=#ECEFF1><b>{minutes:00}:{seconds:00}</b></size></color>";

        if (evidenceScoreText != null) 
            evidenceScoreText.text = $"<color=#78909C>EVIDENCE & SCORE</color>\n<size=120%><color=#ECEFF1><b>{evCount} / 5 CLUES</b></color></size>\n<size=90%><color=#FFD54F><b>SCORE: {totalScore} PTS</b></color></size>";

        if (finalGradeText != null) 
            finalGradeText.text = $"<color=#78909C>FINAL EVALUATION</color>\n<size=150%><color={rankColor}><b>RANK {rankBadge}</b></color></size>\n<size=80%><color=#CFD8DC>[{rankTitle}]</color></size>";
    }

    private void EnsureEndGamePanelCreated()
    {
        if (endGamePanel != null)
        {
            ApplyPolishedEndGameLayout();
            return;
        }

        Canvas mainCanvas = GetMainCanvas();
        if (mainCanvas == null) return;

        // 1. إنشاء الحاوية الرئيسية لخلفية شاشة النهاية
        endGamePanel = new GameObject("EndGamePanel_Polished", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        endGamePanel.transform.SetParent(mainCanvas.transform, false);

        Canvas panelCanvas = endGamePanel.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = endGamePanel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 1000;

        if (endGamePanel.GetComponent<GraphicRaycaster>() == null)
            endGamePanel.AddComponent<GraphicRaycaster>();

        RectTransform panelRt = endGamePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelImg = endGamePanel.GetComponent<Image>();
        panelImg.color = new Color(0.02f, 0.04f, 0.07f, 0.97f);

        // 2. بطاقة التقرير المركزية الفخمة (Card Modal)
        GameObject card = new GameObject("EvaluationCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(endGamePanel.transform, false);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(820f, 480f);
        cardRt.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);

        // 3. شريط العنوان العلوي (Header Title)
        GameObject headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(card.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(750f, 70f);
        headerRt.anchoredPosition = new Vector2(0f, -25f);

        TextMeshProUGUI headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
        bool isCase2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02");
        string caseTitle = isCase2 ? "CASE 02 // CYPHER VAULT RESOLVED" : "CASE 01 // CASE RESOLVED";
        headerTmp.text = $"<color=#00E5FF><b>{caseTitle}</b></color>\n<size=55%><color=#78909C>OFFICIAL FORENSIC BUREAU DEBRIEFING</color></size>";
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.fontSize = 28f;

        // 4. الرتبة الكبيرة في المركز (Rank Badge)
        GameObject gradeObj = new GameObject("FinalGradeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        gradeObj.transform.SetParent(card.transform, false);
        RectTransform gradeRt = gradeObj.GetComponent<RectTransform>();
        gradeRt.anchorMin = new Vector2(0.5f, 0.5f);
        gradeRt.anchorMax = new Vector2(0.5f, 0.5f);
        gradeRt.pivot = new Vector2(0.5f, 0.5f);
        gradeRt.sizeDelta = new Vector2(750f, 130f);
        gradeRt.anchoredPosition = new Vector2(0f, 45f);

        finalGradeText = gradeObj.GetComponent<TextMeshProUGUI>();
        finalGradeText.alignment = TextAlignmentOptions.Center;
        finalGradeText.fontSize = 24f;

        // 5. قسم الوقت المستغرق (Time Taken) في اليسار
        GameObject timeObj = new GameObject("TimeTakenText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timeObj.transform.SetParent(card.transform, false);
        RectTransform timeRt = timeObj.GetComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0.5f, 0.5f);
        timeRt.anchorMax = new Vector2(0.5f, 0.5f);
        timeRt.pivot = new Vector2(0.5f, 0.5f);
        timeRt.sizeDelta = new Vector2(320f, 70f);
        timeRt.anchoredPosition = new Vector2(-190f, -65f);

        timeTakenText = timeObj.GetComponent<TextMeshProUGUI>();
        timeTakenText.alignment = TextAlignmentOptions.Center;
        timeTakenText.fontSize = 18f;

        // 6. قسم النقاط والأدلة (Evidence Score) في اليمين
        GameObject scoreObj = new GameObject("EvidenceScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
        scoreObj.transform.SetParent(card.transform, false);
        RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRt.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRt.pivot = new Vector2(0.5f, 0.5f);
        scoreRt.sizeDelta = new Vector2(320f, 70f);
        scoreRt.anchoredPosition = new Vector2(190f, -65f);

        evidenceScoreText = scoreObj.GetComponent<TextMeshProUGUI>();
        evidenceScoreText.alignment = TextAlignmentOptions.Center;
        evidenceScoreText.fontSize = 18f;

        // 7. زر العودة للقائمة الرئيسية (Main Menu Button - على اليسار)
        GameObject returnBtnObj = new GameObject("ReturnToMenuBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        returnBtnObj.transform.SetParent(card.transform, false);
        RectTransform btnRt = returnBtnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.sizeDelta = new Vector2(330f, 52f);
        btnRt.anchoredPosition = new Vector2(-185f, 30f);

        Image btnImg = returnBtnObj.GetComponent<Image>();
        btnImg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f); // Dark Slate

        Button returnBtn = returnBtnObj.GetComponent<Button>();
        returnBtn.onClick.RemoveAllListeners();
        returnBtn.onClick.AddListener(ReturnToMainMenu);

        GameObject btnLabel = new GameObject("BtnLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabel.transform.SetParent(returnBtnObj.transform, false);
        RectTransform labelRt = btnLabel.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = btnLabel.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "<b>RETURN TO MAIN MENU</b>";
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.fontSize = 17f;
        labelTmp.color = Color.white;

        // 8. زر الانتقال للقضية التالية / القضية 2 (Proceed / Next Case Button - على اليمين)
        GameObject proceedBtnObj = new GameObject("ProceedCaseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        proceedBtnObj.transform.SetParent(card.transform, false);
        RectTransform proceedRt = proceedBtnObj.GetComponent<RectTransform>();
        proceedRt.anchorMin = new Vector2(0.5f, 0f);
        proceedRt.anchorMax = new Vector2(0.5f, 0f);
        proceedRt.pivot = new Vector2(0.5f, 0f);
        proceedRt.sizeDelta = new Vector2(330f, 52f);
        proceedRt.anchoredPosition = new Vector2(185f, 30f);

        Image proceedImg = proceedBtnObj.GetComponent<Image>();
        proceedImg.color = new Color(0.04f, 0.48f, 0.58f, 0.98f); // Cyan Teal Accent

        Button proceedBtn = proceedBtnObj.GetComponent<Button>();
        proceedBtn.onClick.RemoveAllListeners();
        proceedBtn.onClick.AddListener(ProceedToNextCase);

        GameObject proceedLabel = new GameObject("ProceedBtnLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        proceedLabel.transform.SetParent(proceedBtnObj.transform, false);
        RectTransform pLabelRt = proceedLabel.GetComponent<RectTransform>();
        pLabelRt.anchorMin = Vector2.zero;
        pLabelRt.anchorMax = Vector2.one;
        pLabelRt.offsetMin = Vector2.zero;
        pLabelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI pLabelTmp = proceedLabel.GetComponent<TextMeshProUGUI>();
        pLabelTmp.text = isCase2 ? "<b>REPLAY CASE 02 ➔</b>" : "<b>PROCEED TO CASE 02 ➔</b>";
        pLabelTmp.alignment = TextAlignmentOptions.Center;
        pLabelTmp.fontSize = 17f;
        pLabelTmp.color = Color.white;
    }

    private void ApplyPolishedEndGameLayout()
    {
        if (endGamePanel == null) return;

        RectTransform panelRt = endGamePanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
        }

        Image panelImg = endGamePanel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = new Color(0.02f, 0.04f, 0.07f, 0.97f);
        }
    }

    public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Photon.Pun.PhotonNetwork.IsConnected)
        {
            Photon.Pun.PhotonNetwork.LeaveRoom();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void ProceedToNextCase()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "Case01_Main")
        {
            PlayerPrefs.SetString("Selected_Case", "Case02_Main");
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Case02_Main");
        }
        else
        {
            PlayerPrefs.SetString("Selected_Case", "Case02_Main");
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Case02_Main");
        }
    }

    public void ToggleSettings()
    {
        EnsureSettingsPanelCreated();
        if (settingsPanel == null) return;
        
        bool isOpen = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpen);
        
        if (isOpen)
        {
            settingsPanel.transform.SetAsLastSibling();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (isGameActive && !isInventoryOpen) // العودة لوضع اللعب إذا كانت اللعبة مستمرة
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void EnsureSettingsPanelCreated()
    {
        if (settingsPanel != null) return;

        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null) return;

        // خلفية داكنة شاملة للشاشة
        settingsPanel = new GameObject("InGameSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        settingsPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRt = settingsPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelImg = settingsPanel.GetComponent<Image>();
        panelImg.color = new Color(0.03f, 0.05f, 0.09f, 0.95f);

        // بطاقة الإعدادات المركزية
        GameObject card = new GameObject("SettingsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(settingsPanel.transform, false);

        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(700f, 440f);
        cardRt.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.07f, 0.10f, 0.16f, 0.98f);

        // العنوان
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(600f, 60f);
        titleRt.anchoredPosition = new Vector2(0f, -25f);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "<color=#00E5FF><b>GAME SETTINGS</b></color> <color=#455A64>//</color> <color=#90A4AE>SYSTEM CONFIG</color>";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 24f;

        // إضافة سكربت SettingsManager وتوليد أزراره وخياراته
        SettingsManager sm = card.AddComponent<SettingsManager>();

        // زر إغلاق الإعدادات (Close Button)
        GameObject closeBtnObj = new GameObject("CloseSettingsBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(card.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0f);
        closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.sizeDelta = new Vector2(260f, 48f);
        closeRt.anchoredPosition = new Vector2(0f, 25f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.10f, 0.35f, 0.45f, 0.95f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(ToggleSettings);

        GameObject closeLbl = new GameObject("CloseLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLbl.transform.SetParent(closeBtnObj.transform, false);
        RectTransform lblRt = closeLbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        TextMeshProUGUI lblTmp = closeLbl.GetComponent<TextMeshProUGUI>();
        lblTmp.text = "<b>RESUME GAME</b>";
        lblTmp.alignment = TextAlignmentOptions.Center;
        lblTmp.fontSize = 18f;
        lblTmp.color = Color.white;
    }
}
