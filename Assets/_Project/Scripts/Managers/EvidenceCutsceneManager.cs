using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using InvestigationGame;

public class EvidenceCutsceneManager : MonoBehaviour
{
    public static EvidenceCutsceneManager Instance { get; private set; }

    [Header("1. Video UI Panel")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private Button skipVideoBtn;

    [Header("2. Quiz Popup Panel (Vertical Layout)")]
    [SerializeField] private GameObject quizPopupPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons;       // الأزرار الأربعة المرتبة عمودياً
    [SerializeField] private TMP_Text[] optionButtonTexts; // نصوص الأزرار الأربعة

    [Header("3. Mobile Controls (Joystick & Interact Button)")]
    [Tooltip("اسحب هنا عنصر MobileControls من الـ Hierarchy ليتم إخفاؤه أثناء الفيديو")]
    [SerializeField] private GameObject mobileControls;

    private Evidence activeEvidence;
    private bool isAnsweringQuiz = false;
    private Color[] originalOptionColors;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Case 2 may contain the manager but have empty serialized references.
        // Resolve the existing UI by name, including inactive objects.
        ResolveRuntimeReferences();

        // إخفاء اللوحات عند بدء التشغيل
        if (videoPanel != null) videoPanel.SetActive(false);
        if (quizPopupPanel != null) quizPopupPanel.SetActive(false);

        if (videoScreen == null && videoPanel != null)
        {
            videoScreen = videoPanel.GetComponentInChildren<RawImage>(true);
        }

        ResolveSkipButton();
        EnsureFullscreenVideoLayout();

        if (skipVideoBtn != null) 
            skipVideoBtn.onClick.AddListener(SkipVideoToQuiz);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // ربط أزرار الاختيارات العمودية برمجياً وحفظ ألوانها الأصلية
        if (optionButtons != null)
        {
            originalOptionColors = new Color[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    Image btnImg = optionButtons[i].GetComponent<Image>();
                    if (btnImg != null) originalOptionColors[i] = btnImg.color;
                    else originalOptionColors[i] = Color.white;

                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (videoPanel == null) videoPanel = FindObjectByNames("VideoPanel", "EvidenceVideoPanel", "EvidenceVideo");
        if (quizPopupPanel == null) quizPopupPanel = FindObjectByNames("QuizPopupPanel", "EvidenceQuizPanel", "QuestionPanel");
        if (mobileControls == null) mobileControls = FindObjectByNames("MobileControls");

        if (videoPlayer == null && videoPanel != null)
            videoPlayer = videoPanel.GetComponentInChildren<VideoPlayer>(true);
        if (videoScreen == null && videoPanel != null)
            videoScreen = videoPanel.GetComponentInChildren<RawImage>(true);
        if (skipVideoBtn == null && videoPanel != null)
            skipVideoBtn = FindButtonInTree(videoPanel.transform, "skip");

        if (questionText == null && quizPopupPanel != null)
        {
            questionText = FindTextInTree(quizPopupPanel.transform, "Txt_Question", "QuestionText", "Question");
            if (questionText == null) questionText = quizPopupPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (optionButtons == null || optionButtons.Length == 0 || !HasButton(optionButtons))
        {
            Button[] found = FindOptionButtons(quizPopupPanel != null ? quizPopupPanel.transform : null);
            if (found.Length > 0) optionButtons = found;
        }

        if (optionButtonTexts == null || optionButtonTexts.Length == 0 || !HasText(optionButtonTexts))
        {
            if (optionButtons != null)
            {
                optionButtonTexts = new TMP_Text[optionButtons.Length];
                for (int i = 0; i < optionButtons.Length; i++)
                    if (optionButtons[i] != null)
                        optionButtonTexts[i] = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
            }
        }

        Debug.Log($"[EvidenceFlow] UI resolved. VideoPanel={videoPanel != null}, VideoPlayer={videoPlayer != null}, Quiz={quizPopupPanel != null}, Options={optionButtons?.Length ?? 0}");
    }

    private static GameObject FindObjectByNames(params string[] names)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            foreach (string name in names)
                if (t.name == name) return t.gameObject;
        return null;
    }

    private static Button FindButtonInTree(Transform root, string contains)
    {
        if (root == null) return null;
        foreach (Button b in root.GetComponentsInChildren<Button>(true))
            if (b.name.ToLowerInvariant().Contains(contains)) return b;
        return null;
    }

    private static TMP_Text FindTextInTree(Transform root, params string[] names)
    {
        if (root == null) return null;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            foreach (string name in names)
                if (text.name == name) return text;
        return null;
    }

    private static Button[] FindOptionButtons(Transform root)
    {
        if (root == null) return new Button[0];
        var result = new System.Collections.Generic.List<Button>();
        foreach (Button b in root.GetComponentsInChildren<Button>(true))
            if (b.name.ToLowerInvariant().Contains("option")) result.Add(b);
        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result.ToArray();
    }

    private static bool HasButton(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return false;
        foreach (Button b in buttons) if (b != null) return true;
        return false;
    }

    private static bool HasText(TMP_Text[] texts)
    {
        if (texts == null || texts.Length == 0) return false;
        foreach (TMP_Text t in texts) if (t != null) return true;
        return false;
    }

    public static bool IsQuizOrVideoActive { get; private set; } = false;

    // تُستدعى عندما يضغط اللاعب على الدليل في الغرفة
    public void PlayEvidenceFlow(Evidence evidence)
    {
        if (evidence == null) return;
        ResolveRuntimeReferences();
        activeEvidence = evidence;
        IsQuizOrVideoActive = true;
        isAnsweringQuiz = false;

        // إتاحة مؤشر الماوس للتفاعل
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // إخفاء عناصر تحكم الجوال
        HideMobileControls();

        // 1. مسح الفريم القديم العالق في الـ RenderTexture
        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
        }

        if (videoScreen != null)
        {
            videoScreen.gameObject.SetActive(false);
        }

        // الخطوة 1 الإبداعية: فتح واستخدام أداة الـ 3D التفاعلية مباشرة أمام الدليل والكاميرا
        LaunchDirect3DToolExamination(evidence);
    }

    private void LaunchDirect3DToolExamination(Evidence evidence)
    {
        string evId = evidence.evidenceID != null ? evidence.evidenceID.ToUpperInvariant() : "";
        string eNameLower = evidence.evidenceName != null ? evidence.evidenceName.ToLowerInvariant() : "";
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        string kitName = "FORENSIC BIOCHEMICAL PROTOCOL";
        string actionInstruction = "Examine and analyze physical and biochemical markers.";
        ForensicToolType resolvedTool = ForensicToolType.ChemicalDropper;

        if (eNameLower.Contains("blood") || (evId == "EV_05" && sceneName.Contains("Case01")))
        {
            kitName = "ENZYMATIC BLOOD REAGENT DROPPER";
            actionInstruction = "Swab blood droplet with anti-A and anti-B agglutination reagent.";
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("cyanide") || eNameLower.Contains("mug") || (evId == "EV_01" && sceneName.Contains("Case02")))
        {
            kitName = "MASS SPECTROMETRY TOXIN ASSAY PIPETTE";
            actionInstruction = "Swab residue on the ceramic cup rim with alkaline picrate.";
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("keycard") || (evId == "EV_03" && sceneName.Contains("Case01")))
        {
            kitName = "RFID CONTACTLESS CHIP SCANNER PROBE";
            actionInstruction = "Interface micro-probe with NFC coil to extract badge authorization.";
            resolvedTool = ForensicToolType.CyberScannerProbe;
        }
        else if (eNameLower.Contains("flash") || eNameLower.Contains("drive") || (evId == "EV_02" && sceneName.Contains("Case02")))
        {
            kitName = "QUANTUM CIPHER BRUTE-FORCE PROBE";
            actionInstruction = "Mount secure hardware sandbox to decrypt hidden memory sectors.";
            resolvedTool = ForensicToolType.CyberScannerProbe;
        }
        else if (eNameLower.Contains("weapon") || eNameLower.Contains("gun") || (evId == "EV_02" && sceneName.Contains("Case01")))
        {
            kitName = "MAGNETIC DUSTING WAND & PRINT LIFTER";
            actionInstruction = "Brush magnetic iron flake powder across grip to lift latent friction ridges.";
            resolvedTool = ForensicToolType.MagneticPowderBrush;
        }
        else if (eNameLower.Contains("syringe") || eNameLower.Contains("vial") || (evId == "EV_03" && sceneName.Contains("Case02")))
        {
            kitName = "BIO-HAZARD CHEMICAL SAMPLER";
            actionInstruction = "Extract trace liquid residue from the broken needle.";
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("ledger") || (evId == "EV_01" && sceneName.Contains("Case01")))
        {
            kitName = "MULTI-SPECTRUM UV LUMINESCENT TORCH";
            actionInstruction = "Expose fragile ash fragments to 365nm narrow-band UV illumination.";
            resolvedTool = ForensicToolType.UvLightTorch;
        }
        else if (eNameLower.Contains("wiretap") || eNameLower.Contains("audio") || (evId == "EV_04" && sceneName.Contains("Case02")))
        {
            kitName = "ACOUSTIC HARMONIC NOISE FILTER";
            actionInstruction = "Run bandpass harmonic audio filter to match vocal frequency.";
            resolvedTool = ForensicToolType.AcousticFrequencyTuner;
        }
        else if (eNameLower.Contains("fabric") || (evId == "EV_04" && sceneName.Contains("Case01")))
        {
            kitName = "POLARIZED LIGHT UV TORCH";
            actionInstruction = "Magnify snagged fabric threads under polarized UV lenses.";
            resolvedTool = ForensicToolType.UvLightTorch;
        }
        else if (eNameLower.Contains("notebook") || (evId == "EV_05" && sceneName.Contains("Case02")))
        {
            kitName = "ELECTROSTATIC UV SCANNER";
            actionInstruction = "Apply electrostatic toner deposition to reconstruct indented cipher.";
            resolvedTool = ForensicToolType.UvLightTorch;
        }

        var toolController = FindAnyObjectByType<Forensic3DToolController>();
        if (toolController == null)
        {
            toolController = gameObject.AddComponent<Forensic3DToolController>();
        }

        toolController.Launch3DToolAnalysis(
            evidence,
            resolvedTool,
            kitName,
            actionInstruction,
            onComplete: () =>
            {
                StartVideoCutscene();
            }
        );
    }

    private void StartVideoCutscene()
    {
        HideForensicActionOverlay();

        // تشغيل فيديو الدليل السينمائي
        if (videoPanel != null)
        {
            videoPanel.SetActive(true);
            EnsureFullscreenVideoLayout();
        }
        
        if (videoPlayer != null && activeEvidence != null && activeEvidence.evidenceVideoClip != null)
        {
            videoPlayer.clip = activeEvidence.evidenceVideoClip;

            // انتظار جاهزية الفيديو لمنع وميض الفريم السابق
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
        }
        else if (quizPopupPanel != null)
        {
            ShowQuizPopup();
        }
        else
        {
            Debug.LogWarning("[EvidenceFlow] Video/quiz UI is missing; collecting evidence directly.");
            IsQuizOrVideoActive = false;
            if (activeEvidence != null) activeEvidence.CollectDirectly();
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        if (videoScreen != null)
        {
            videoScreen.gameObject.SetActive(true);
        }
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        ShowQuizPopup();
    }

    private void SkipVideoToQuiz()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        ShowQuizPopup();
    }

    private void ResolveSkipButton()
    {
        if (skipVideoBtn == null && videoPanel != null)
        {
            Button[] buttons = videoPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name.ToLowerInvariant().Contains("skip"))
                {
                    skipVideoBtn = button;
                    break;
                }
            }
        }

        if (skipVideoBtn != null)
        {
            skipVideoBtn.gameObject.SetActive(true);
            skipVideoBtn.transform.SetAsLastSibling();
        }
    }

    private void EnsureFullscreenVideoLayout()
    {
        Stretch(videoPanel != null ? videoPanel.transform as RectTransform : null);
        Stretch(videoScreen != null ? videoScreen.rectTransform : null);
    }

    private static void Stretch(RectTransform target)
    {
        if (target == null) return;
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    // الخطوة ب: إظهار لوحة الأسئلة العمودية وتعبئة الخيارات الصحيحة
    private void ShowQuizPopup()
    {
        if (videoPanel != null) videoPanel.SetActive(false);
        if (quizPopupPanel != null)
        {
            quizPopupPanel.SetActive(true);
            ApplyPolishedQuizLayout();
        }

        // تهيئة حالة الإجابة وتفعيل الأزرار لاختيار واحد فقط
        isAnsweringQuiz = false;
        ResetOptionButtonColors();
        SetOptionButtonsInteractable(true);

        if (activeEvidence != null)
        {
            if (questionText != null)
            {
                questionText.text = $"<color=#00E5FF><size=65%>// FORENSIC CLUE INQUIRY</size></color>\n<color=#FFD54F><b>{activeEvidence.quizQuestion}</b></color>";
                questionText.fontSize = 24f;
                questionText.alignment = TextAlignmentOptions.Center;
            }

            // تعبئة النصوص الأربعة تلقائياً وتنسيق الأزرار
            if (optionButtonTexts != null && activeEvidence.options != null)
            {
                for (int i = 0; i < optionButtonTexts.Length; i++)
                {
                    if (i < activeEvidence.options.Length)
                    {
                        if (optionButtonTexts[i] != null)
                        {
                            char optionLetter = (char)('A' + i);
                            optionButtonTexts[i].text = $"<color=#00E5FF><b>[{optionLetter}]</b></color>  <color=#ECEFF1>{activeEvidence.options[i]}</color>";
                            optionButtonTexts[i].fontSize = 20f;
                            optionButtonTexts[i].alignment = TextAlignmentOptions.MidlineLeft;
                        }
                        if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                            optionButtons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                            optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void ApplyPolishedQuizLayout()
    {
        if (quizPopupPanel == null) return;

        // 1. تلوين خلفية اللوحة بلون كحلي داكن شبه شفاف فخم
        Image bgImg = quizPopupPanel.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.color = new Color(0.03f, 0.05f, 0.08f, 0.96f);
        }

        // 2. ضبط موضع وحجم نص السؤال
        if (questionText != null)
        {
            RectTransform qRect = questionText.GetComponent<RectTransform>();
            if (qRect != null)
            {
                qRect.anchorMin = new Vector2(0.5f, 0.5f);
                qRect.anchorMax = new Vector2(0.5f, 0.5f);
                qRect.pivot = new Vector2(0.5f, 0.5f);
                qRect.anchoredPosition = new Vector2(0f, 220f);
                qRect.sizeDelta = new Vector2(1000f, 130f);
            }
        }

        // 3. ضبط حاوية أزرار الخيارات (Options Container) وتوسيطها
        Transform optionsContainer = quizPopupPanel.transform.Find("OptionsContainer");
        if (optionsContainer == null && optionButtons != null && optionButtons.Length > 0 && optionButtons[0] != null)
        {
            optionsContainer = optionButtons[0].transform.parent;
        }

        if (optionsContainer != null)
        {
            RectTransform optRect = optionsContainer.GetComponent<RectTransform>();
            if (optRect != null)
            {
                optRect.anchorMin = new Vector2(0.5f, 0.5f);
                optRect.anchorMax = new Vector2(0.5f, 0.5f);
                optRect.pivot = new Vector2(0.5f, 0.5f);
                optRect.anchoredPosition = new Vector2(0f, -60f);
                optRect.sizeDelta = new Vector2(850f, 350f);
            }

            VerticalLayoutGroup vlg = optionsContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 16f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
            }
        }

        // 4. تنسيق أزرار الخيارات لتكون بطاقات أنيقة بارتفاع مريح (60px) وScale صحيح
        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button btn = optionButtons[i];
                if (btn == null) continue;

                RectTransform bRect = btn.GetComponent<RectTransform>();
                if (bRect != null)
                {
                    bRect.localScale = Vector3.one;
                    bRect.sizeDelta = new Vector2(bRect.sizeDelta.x, 62f);
                }

                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = new Color(0.09f, 0.13f, 0.20f, 0.95f);
                }

                // ضبط هوامش النص داخل الزر
                if (optionButtonTexts != null && i < optionButtonTexts.Length && optionButtonTexts[i] != null)
                {
                    RectTransform txtRect = optionButtonTexts[i].GetComponent<RectTransform>();
                    if (txtRect != null)
                    {
                        txtRect.anchorMin = Vector2.zero;
                        txtRect.anchorMax = Vector2.one;
                        txtRect.offsetMin = new Vector2(25f, 0f);
                        txtRect.offsetMax = new Vector2(-25f, 0f);
                    }
                }
            }
        }
    }

    // الخطوة ج: التحقق من إجابة اللاعب (الخيار الصحيح أو الخاطئ) مع التغذية البصرية (Feedback)
    private void OnOptionSelected(int selectedIndex)
    {
        // منع أي اختيار إضافي أو مكرر بنفس اللحظة
        if (activeEvidence == null || isAnsweringQuiz) return;

        isAnsweringQuiz = true;
        SetOptionButtonsInteractable(false); // قفل فوري لجميع الأزرار الأربعة

        bool isCorrect = (selectedIndex == activeEvidence.correctOptionIndex);
        Button selectedBtn = (optionButtons != null && selectedIndex < optionButtons.Length) ? optionButtons[selectedIndex] : null;
        Button correctBtn = (optionButtons != null && activeEvidence.correctOptionIndex < optionButtons.Length) ? optionButtons[activeEvidence.correctOptionIndex] : null;

        StartCoroutine(HandleAnswerFeedback(selectedIndex, selectedBtn, isCorrect, correctBtn));
    }

    private void SetOptionButtonsInteractable(bool state)
    {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = state;
            }
        }
    }

    private IEnumerator HandleAnswerFeedback(int selectedIndex, Button selectedBtn, bool isCorrect, Button correctBtn)
    {
        Image selectedImg = selectedBtn != null ? selectedBtn.GetComponent<Image>() : null;

        if (isCorrect)
        {
            if (selectedImg != null) selectedImg.color = new Color(0.2f, 0.85f, 0.2f); // أخضر للإجابة الصحيحة
        }
        else
        {
            // إظهار لون أحمر فقط على الزر الخاطئ الذي اختاره اللاعب، مع عدم إظهار الإجابة الصحيحة نهائياً
            if (selectedImg != null) selectedImg.color = new Color(0.85f, 0.2f, 0.2f);
        }

        // مهلة قصيرة جداً (0.6 ثانية) ليشاهد أن خياره كان خاطئاً
        yield return new WaitForSeconds(0.6f);

        // إعادة ألوان الأزرار الأصلية
        ResetOptionButtonColors();

        if (isCorrect)
        {
            Debug.Log("<color=green>[SUCCESS] Correct Answer! Evidence processed.</color>");

            // إغلاق لوحة الأسئلة
            if (quizPopupPanel != null) quizPopupPanel.SetActive(false);

            ShowMobileControls();

            if (EvidenceManager.Instance != null)
            {
                EvidenceManager.Instance.CollectEvidence(activeEvidence);
            }

            if (CaseDossierManager.Instance != null)
            {
                CaseDossierManager.Instance.ProcessEvidenceCollected(activeEvidence);
            }

            if (activeEvidence != null)
            {
                activeEvidence.gameObject.SetActive(false);
            }

            IsQuizOrVideoActive = false;
            isAnsweringQuiz = false;
        }
        else
        {
            Debug.LogWarning("<color=red>[INCORRECT] Wrong answer! Re-check the evidence details.</color>");
            
            // خصم النقاط فوراً وتعديل الرانك مع تسجيل الخطأ ومكانه
            string evName = activeEvidence != null ? activeEvidence.evidenceName : "Unknown Clue";
            string chosenAnswer = "Option " + (selectedIndex + 1);
            if (optionButtonTexts != null && selectedIndex < optionButtonTexts.Length && optionButtonTexts[selectedIndex] != null)
            {
                chosenAnswer = optionButtonTexts[selectedIndex].text;
            }

            ScoreManager.EnsureInstance().DeductScoreForMistake(75, $"Clue Analysis Error: [{evName}] Selected: \"{chosenAnswer}\"");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("<color=#FF5252>INCORRECT ANALYSIS (-75 PTS) // RE-INSPECT EVIDENCE</color>", 3.0f);
            }

            // إغلاق نافذة الأسئلة مباشرة وإعادة تحكم اللاعب والماوس للتحقيق من جديد
            if (quizPopupPanel != null) quizPopupPanel.SetActive(false);

            ShowMobileControls();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsQuizOrVideoActive = false;
            isAnsweringQuiz = false;
            SetOptionButtonsInteractable(true);
        }
    }

    private void ResetOptionButtonColors()
    {
        if (optionButtons != null)
        {
            Color defaultCardCol = new Color(0.09f, 0.13f, 0.20f, 0.95f);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    Image img = optionButtons[i].GetComponent<Image>();
                    if (img != null) img.color = defaultCardCol;
                }
            }
        }
    }

    private void HideMobileControls()
    {
        if (mobileControls != null)
        {
            mobileControls.SetActive(false);
        }
        else
        {
            // محاولة العثور عليه تلقائياً إذا نسي المستخدم ربطه في الـ Inspector
            var found = GameObject.Find("MobileControls");
            if (found != null)
            {
                mobileControls = found;
                mobileControls.SetActive(false);
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(false);
        }

        // إخفاء زر اليد في اللاعب فوراً
        var interaction = FindAnyObjectByType<PlayerInteraction>();
        if (interaction != null && interaction.mobileInteractBtn != null)
        {
            interaction.mobileInteractBtn.SetActive(false);
        }
    }

    private void ShowMobileControls()
    {
        IsQuizOrVideoActive = false;

        if (mobileControls != null)
        {
            mobileControls.SetActive(true);
        }
        else
        {
            var found = GameObject.Find("MobileControls");
            if (found != null)
            {
                mobileControls = found;
                mobileControls.SetActive(true);
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(true);
        }
    }

    // =========================================================================
    //  نظام الفحص الجنائي الميداني التفاعلي (Interactive Forensic Action System)
    // =========================================================================

    private GameObject forensicActionPanel;
    private TMP_Text forensicTitleText;
    private TMP_Text forensicToolText;
    private TMP_Text forensicDescText;
    private Button forensicActionButton;
    private TMP_Text forensicActionButtonText;
    private GameObject forensicProgressBarObj;
    private Image forensicProgressFill;
    private TMP_Text forensicProgressStatus;
    private Coroutine forensicRoutine;

    private ForensicToolType currentToolType = ForensicToolType.ChemicalDropper;
    private string currentToolName = "";
    private string currentToolInstruction = "";

    private void ShowForensicActionOverlay(Evidence evidence)
    {
        CreateForensicActionUI();
        if (forensicActionPanel == null)
        {
            StartVideoCutscene();
            return;
        }

        forensicActionPanel.transform.SetAsLastSibling();
        forensicActionPanel.SetActive(true);

        if (forensicProgressBarObj != null) forensicProgressBarObj.SetActive(false);
        if (forensicActionButton != null)
        {
            forensicActionButton.gameObject.SetActive(true);
            forensicActionButton.interactable = true;
        }

        string evidenceNameUpper = evidence.evidenceName.ToUpperInvariant();
        string kitName = "FORENSIC ANALYSIS PROTOCOL";
        string actionVerb = "COMMENCE SCIENTIFIC SCAN";
        string actionInstruction = "Perform specialized physical and biochemical examination on the clue.";
        Color actionBtnColor = new Color(0.08f, 0.58f, 0.75f, 1f);

        string evId = evidence.evidenceID != null ? evidence.evidenceID.ToUpperInvariant() : "";
        string eNameLower = evidence.evidenceName.ToLowerInvariant();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        ForensicToolType resolvedTool = ForensicToolType.ChemicalDropper;

        if (eNameLower.Contains("blood") || (evId == "EV_05" && sceneName.Contains("Case01")))
        {
            kitName = "ENZYMATIC BLOOD REAGENT DROPPER";
            actionVerb = "USE 3D DROPPER // APPLY REAGENT";
            actionInstruction = "Swab blood droplet with anti-A and anti-B agglutination reagent to determine recipient blood group.";
            actionBtnColor = new Color(0.85f, 0.22f, 0.25f, 1f); // Lab Crimson
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("cyanide") || eNameLower.Contains("mug") || (evId == "EV_01" && sceneName.Contains("Case02")))
        {
            kitName = "MASS SPECTROMETRY TOXIN ASSAY PIPETTE";
            actionVerb = "USE 3D PIPETTE // TEST CYANIDE";
            actionInstruction = "Swab residue on the ceramic cup rim with alkaline picrate to identify lethal cyanide formulation.";
            actionBtnColor = new Color(0.18f, 0.72f, 0.52f, 1f); // Bio Emerald
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("keycard") || (evId == "EV_03" && sceneName.Contains("Case01")))
        {
            kitName = "RFID CONTACTLESS CHIP SCANNER PROBE";
            actionVerb = "USE 3D CYBER PROBE // EXTRACT LOGS";
            actionInstruction = "Interface micro-probe with contactless NFC coil to extract badge authentication and room entry timestamp.";
            actionBtnColor = new Color(0.05f, 0.68f, 0.92f, 1f); // Cyber Cyan
            resolvedTool = ForensicToolType.CyberScannerProbe;
        }
        else if (eNameLower.Contains("flash") || eNameLower.Contains("drive") || (evId == "EV_02" && sceneName.Contains("Case02")))
        {
            kitName = "QUANTUM CIPHER BRUTE-FORCE PROBE";
            actionVerb = "USE 3D PROBE // BYPASS ENCRYPTION";
            actionInstruction = "Mount secure hardware sandbox to decrypt hidden memory sectors and trace spoofed VPN routing logs.";
            actionBtnColor = new Color(0.12f, 0.55f, 0.95f, 1f); // Quantum Blue
            resolvedTool = ForensicToolType.CyberScannerProbe;
        }
        else if (eNameLower.Contains("weapon") || eNameLower.Contains("gun") || (evId == "EV_02" && sceneName.Contains("Case01")))
        {
            kitName = "MAGNETIC DUSTING WAND & PRINT LIFTER";
            actionVerb = "USE 3D MAGNETIC BRUSH // DUST PRINTS";
            actionInstruction = "Brush high-contrast magnetic iron flake powder across grip grooves to lift suspect latent friction ridges.";
            actionBtnColor = new Color(0.92f, 0.62f, 0.15f, 1f); // Forensic Gold
            resolvedTool = ForensicToolType.MagneticPowderBrush;
        }
        else if (eNameLower.Contains("syringe") || eNameLower.Contains("vial") || (evId == "EV_03" && sceneName.Contains("Case02")))
        {
            kitName = "BIO-HAZARD CHEMICAL SAMPLER";
            actionVerb = "USE 3D SAMPLER // EXTRACT LIQUID";
            actionInstruction = "Centrifuge trace liquid residue from the broken needle to verify laboratory restricted batch credentials.";
            actionBtnColor = new Color(0.68f, 0.35f, 0.92f, 1f); // Bio Violet
            resolvedTool = ForensicToolType.ChemicalDropper;
        }
        else if (eNameLower.Contains("ledger") || (evId == "EV_01" && sceneName.Contains("Case01")))
        {
            kitName = "MULTI-SPECTRUM UV LUMINESCENT TORCH";
            actionVerb = "USE 3D UV TORCH // SCAN CHARRED LAYERS";
            actionInstruction = "Expose fragile ash fragments to 365nm narrow-band UV illumination to reveal burned transaction ledgers.";
            actionBtnColor = new Color(0.38f, 0.45f, 0.95f, 1f); // UV Indigo
            resolvedTool = ForensicToolType.UvLightTorch;
        }
        else if (eNameLower.Contains("wiretap") || eNameLower.Contains("audio") || (evId == "EV_04" && sceneName.Contains("Case02")))
        {
            kitName = "ACOUSTIC HARMONIC NOISE FILTER";
            actionVerb = "USE 3D AUDIO TUNER // FILTER FREQUENCY";
            actionInstruction = "Run bandpass audio harmonic filter across intercepted transmission to match suspect biometric vocal frequency.";
            actionBtnColor = new Color(0.92f, 0.35f, 0.65f, 1f); // Acoustic Rose
            resolvedTool = ForensicToolType.AcousticFrequencyTuner;
        }
        else if (eNameLower.Contains("fabric") || (evId == "EV_04" && sceneName.Contains("Case01")))
        {
            kitName = "POLARIZED LIGHT UV TORCH";
            actionVerb = "USE 3D UV BEAM // EXAMINE FIBERS";
            actionInstruction = "Magnify snagged red fabric threads under polarized UV lenses to match textile grade to suspect clothing weave.";
            actionBtnColor = new Color(0.15f, 0.82f, 0.68f, 1f); // Optical Teal
            resolvedTool = ForensicToolType.UvLightTorch;
        }
        else if (eNameLower.Contains("notebook") || (evId == "EV_05" && sceneName.Contains("Case02")))
        {
            kitName = "ELECTROSTATIC UV SCANNER";
            actionVerb = "USE 3D UV SCANNER // REVEAL INDENTATIONS";
            actionInstruction = "Apply electrostatic toner deposition across compressed paper to reconstruct indented cipher formula.";
            actionBtnColor = new Color(0.92f, 0.52f, 0.18f, 1f); // Analysis Amber
            resolvedTool = ForensicToolType.UvLightTorch;
        }

        currentToolType = resolvedTool;
        currentToolName = kitName;
        currentToolInstruction = actionInstruction;

        if (forensicActionButton != null)
        {
            Image btnImg = forensicActionButton.GetComponent<Image>();
            if (btnImg != null) btnImg.color = actionBtnColor;
        }

        if (forensicTitleText != null) forensicTitleText.text = $"<color=#00E5FF><b>EXAMINATION:</b></color> <color=#FFD54F>{evidenceNameUpper}</color>";
        if (forensicToolText != null) forensicToolText.text = $"<color=#69F0AE><b>APPARATUS:</b></color> {kitName}";
        if (forensicDescText != null) forensicDescText.text = actionInstruction;
        if (forensicActionButtonText != null) forensicActionButtonText.text = $"<b>{actionVerb}</b>";
    }

    private void HideForensicActionOverlay()
    {
        if (forensicRoutine != null)
        {
            StopCoroutine(forensicRoutine);
            forensicRoutine = null;
        }

        if (forensicActionPanel != null)
        {
            forensicActionPanel.SetActive(false);
        }
    }

    private void OnForensicActionButtonClicked()
    {
        if (forensicRoutine != null) return;

        // إخفاء لوحة الـ UI التمهيدية
        if (forensicActionPanel != null)
        {
            forensicActionPanel.SetActive(false);
        }

        // تشغيل نظام الـ 3D التفاعلي المجسم
        var toolController = FindAnyObjectByType<Forensic3DToolController>();
        if (toolController == null)
        {
            toolController = gameObject.AddComponent<Forensic3DToolController>();
        }

        toolController.Launch3DToolAnalysis(
            activeEvidence,
            currentToolType,
            currentToolName,
            currentToolInstruction,
            onComplete: () =>
            {
                // الانتقال إلى الفيديو السينمائي المختبري فور انتهاء فحص اللاعب
                StartVideoCutscene();
            }
        );
    }

    private void CreateForensicActionUI()
    {
        if (forensicActionPanel != null) return;

        Canvas targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas candidate in canvases)
            {
                if (candidate != null && candidate.isActiveAndEnabled && candidate.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = candidate;
                    break;
                }
            }
        }
        if (targetCanvas == null) return;

        // 1. حاوية تغطي الشاشة بالكامل مع حجب الإدخال
        forensicActionPanel = new GameObject("ForensicActionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        forensicActionPanel.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRt = forensicActionPanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bgImg = forensicActionPanel.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.07f, 0.95f); // تعتيم كحلي عميق

        // 2. بطاقة الفحص المخبري المركزية المتناسقة مع شاشات اللوبي
        GameObject dialogBox = new GameObject("ForensicDialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.transform.SetParent(forensicActionPanel.transform, false);

        RectTransform boxRt = dialogBox.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(800f, 440f);

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.color = new Color(0.07f, 0.10f, 0.15f, 0.98f); // نوار داكن نقي

        // خط علوي نيون مميز يطابق اللوبي
        GameObject topBar = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(dialogBox.transform, false);
        RectTransform barRt = topBar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.sizeDelta = new Vector2(0f, 6f);
        topBar.GetComponent<Image>().color = new Color(0.0f, 0.90f, 1.0f, 1f); // Neon Cyan

        // 3. عنوان الدليل المفحوص
        GameObject titleObj = new GameObject("ForensicTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(dialogBox.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.04f, 0.80f);
        tRt.anchorMax = new Vector2(0.96f, 0.96f);
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        forensicTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        forensicTitleText.alignment = TextAlignmentOptions.Center;
        forensicTitleText.fontSize = 24f;

        // 4. أداة الفحص المخبري
        GameObject toolObj = new GameObject("ForensicTool", typeof(RectTransform), typeof(TextMeshProUGUI));
        toolObj.transform.SetParent(dialogBox.transform, false);
        RectTransform toolRt = toolObj.GetComponent<RectTransform>();
        toolRt.anchorMin = new Vector2(0.05f, 0.65f);
        toolRt.anchorMax = new Vector2(0.95f, 0.78f);
        toolRt.offsetMin = Vector2.zero;
        toolRt.offsetMax = Vector2.zero;
        forensicToolText = toolObj.GetComponent<TextMeshProUGUI>();
        forensicToolText.alignment = TextAlignmentOptions.Center;
        forensicToolText.fontSize = 17f;
        forensicToolText.color = new Color(0.85f, 0.95f, 1f, 1f);

        // 5. وصف التعليمات والشرح العلمي
        GameObject descObj = new GameObject("ForensicDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(dialogBox.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.06f, 0.40f);
        descRt.anchorMax = new Vector2(0.94f, 0.62f);
        descRt.offsetMin = Vector2.zero;
        descRt.offsetMax = Vector2.zero;
        forensicDescText = descObj.GetComponent<TextMeshProUGUI>();
        forensicDescText.alignment = TextAlignmentOptions.Center;
        forensicDescText.fontSize = 15f;
        forensicDescText.lineSpacing = 14f;
        forensicDescText.color = new Color(0.80f, 0.85f, 0.92f, 1f);

        // 6. زر الإجراء التفاعلي الرئيسي
        GameObject actionBtnObj = new GameObject("Btn_ForensicAction", typeof(RectTransform), typeof(Image), typeof(Button));
        actionBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform aRt = actionBtnObj.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.14f, 0.17f);
        aRt.anchorMax = new Vector2(0.86f, 0.33f);
        aRt.offsetMin = Vector2.zero;
        aRt.offsetMax = Vector2.zero;

        Image aImg = actionBtnObj.GetComponent<Image>();
        aImg.color = new Color(0.08f, 0.58f, 0.75f, 1f);

        forensicActionButton = actionBtnObj.GetComponent<Button>();
        forensicActionButton.onClick.AddListener(OnForensicActionButtonClicked);

        GameObject aTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        aTxtObj.transform.SetParent(actionBtnObj.transform, false);
        RectTransform atRt = aTxtObj.GetComponent<RectTransform>();
        atRt.anchorMin = Vector2.zero;
        atRt.anchorMax = Vector2.one;
        atRt.offsetMin = Vector2.zero;
        atRt.offsetMax = Vector2.zero;
        forensicActionButtonText = aTxtObj.GetComponent<TextMeshProUGUI>();
        forensicActionButtonText.alignment = TextAlignmentOptions.Center;
        forensicActionButtonText.text = "<b>COMMENCE FORENSIC ANALYSIS</b>";
        forensicActionButtonText.fontSize = 17f;
        forensicActionButtonText.color = Color.white;

        // 7. شريط التقدم التفاعلي (Progress Bar)
        forensicProgressBarObj = new GameObject("ProgressBarContainer", typeof(RectTransform), typeof(Image));
        forensicProgressBarObj.transform.SetParent(dialogBox.transform, false);
        RectTransform barRootRt = forensicProgressBarObj.GetComponent<RectTransform>();
        barRootRt.anchorMin = new Vector2(0.14f, 0.17f);
        barRootRt.anchorMax = new Vector2(0.86f, 0.33f);
        barRootRt.offsetMin = Vector2.zero;
        barRootRt.offsetMax = Vector2.zero;
        forensicProgressBarObj.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.95f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(forensicProgressBarObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        forensicProgressFill = fillObj.GetComponent<Image>();
        forensicProgressFill.type = Image.Type.Filled;
        forensicProgressFill.fillMethod = Image.FillMethod.Horizontal;
        forensicProgressFill.color = new Color(0f, 0.85f, 0.95f, 1f);

        GameObject pStatusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        pStatusObj.transform.SetParent(forensicProgressBarObj.transform, false);
        RectTransform psRt = pStatusObj.GetComponent<RectTransform>();
        psRt.anchorMin = Vector2.zero;
        psRt.anchorMax = Vector2.one;
        psRt.offsetMin = Vector2.zero;
        psRt.offsetMax = Vector2.zero;
        forensicProgressStatus = pStatusObj.GetComponent<TextMeshProUGUI>();
        forensicProgressStatus.alignment = TextAlignmentOptions.Center;
        forensicProgressStatus.fontSize = 15f;
        forensicProgressStatus.fontStyle = FontStyles.Bold;
        forensicProgressStatus.color = Color.white;

        forensicProgressBarObj.SetActive(false);

        // 8. زر تخطي الفحص
        GameObject skipBtnObj = new GameObject("Btn_SkipForensic", typeof(RectTransform), typeof(Image), typeof(Button));
        skipBtnObj.transform.SetParent(dialogBox.transform, false);
        RectTransform sRt = skipBtnObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.32f, 0.04f);
        sRt.anchorMax = new Vector2(0.68f, 0.13f);
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;

        Image sImg = skipBtnObj.GetComponent<Image>();
        sImg.color = new Color(0.18f, 0.22f, 0.28f, 0.75f);

        Button skipBtn = skipBtnObj.GetComponent<Button>();
        skipBtn.onClick.AddListener(StartVideoCutscene);

        GameObject stObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        stObj.transform.SetParent(skipBtnObj.transform, false);
        RectTransform stRt = stObj.GetComponent<RectTransform>();
        stRt.anchorMin = Vector2.zero;
        stRt.anchorMax = Vector2.one;
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;
        TextMeshProUGUI sTxt = stObj.GetComponent<TextMeshProUGUI>();
        sTxt.text = "SKIP TO LAB VIDEO >>";
        sTxt.alignment = TextAlignmentOptions.Center;
        sTxt.fontSize = 13f;
        sTxt.color = new Color(0.75f, 0.82f, 0.90f, 1f);

        forensicActionPanel.SetActive(false);
    }
}
