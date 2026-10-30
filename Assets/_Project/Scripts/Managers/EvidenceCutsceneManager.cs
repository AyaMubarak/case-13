using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        // إتاحة مؤشر الماوس للتفاعل مع الفيديو وأزرار الأسئلة
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // إخفاء عناصر تحكم الجوال (الجويستيك واليد) لمنع التفاعل أو اللمس بالخطأ أثناء الفيديو والأسئلة
        HideMobileControls();

        // 1. مسح الفريم القديم العالق في الـ RenderTexture
        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
        }

        // إخفاء الـ Screen مبدئياً حتى لا تظهر لقطة الفيديو السابق
        if (videoScreen != null)
        {
            videoScreen.gameObject.SetActive(false);
        }

        // الخطوة أ: تشغيل فيديو الدليل السينمائي فوراً
        if (videoPanel != null)
        {
            videoPanel.SetActive(true);
            EnsureFullscreenVideoLayout();
        }
        
        if (videoPlayer != null && evidence != null && evidence.evidenceVideoClip != null)
        {
            videoPlayer.clip = evidence.evidenceVideoClip;

            // انتظار جاهزية الفيديو (Prepare) لمنع وميض الفريم السابق
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
        }
        else if (quizPopupPanel != null)
        {
            // لو لم يكن هناك فيديو، انتقل للأسئلة مباشرة
            ShowQuizPopup();
        }
        else
        {
            // لا نترك الدليل عالقاً إذا كان المشهد ناقصاً.
            Debug.LogWarning("[EvidenceFlow] Video/quiz UI is missing; collecting evidence directly.");
            IsQuizOrVideoActive = false;
            evidence.CollectDirectly();
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
        if (activeEvidence == null || isAnsweringQuiz) return;

        bool isCorrect = (selectedIndex == activeEvidence.correctOptionIndex);
        Button selectedBtn = (optionButtons != null && selectedIndex < optionButtons.Length) ? optionButtons[selectedIndex] : null;
        Button correctBtn = (optionButtons != null && activeEvidence.correctOptionIndex < optionButtons.Length) ? optionButtons[activeEvidence.correctOptionIndex] : null;

        StartCoroutine(HandleAnswerFeedback(selectedBtn, isCorrect, correctBtn));
    }

    private IEnumerator HandleAnswerFeedback(Button selectedBtn, bool isCorrect, Button correctBtn)
    {
        isAnsweringQuiz = true;

        Image selectedImg = selectedBtn != null ? selectedBtn.GetComponent<Image>() : null;
        Image correctImg = correctBtn != null ? correctBtn.GetComponent<Image>() : null;

        if (isCorrect)
        {
            if (selectedImg != null) selectedImg.color = new Color(0.2f, 0.85f, 0.2f); // أخضر
        }
        else
        {
            if (selectedImg != null) selectedImg.color = new Color(0.85f, 0.2f, 0.2f); // أحمر
            if (correctImg != null) correctImg.color = new Color(0.2f, 0.85f, 0.2f);  // إظهار الزر الصحيح بالأخضر
        }

        yield return new WaitForSeconds(1.2f);

        // إعادة ألوان الأزرار الأصلية
        ResetOptionButtonColors();

        if (isCorrect)
        {
            Debug.Log("<color=green>[SUCCESS] Correct Answer! Evidence processed.</color>");

            // إغلاق لوحة الأسئلة
            if (quizPopupPanel != null) quizPopupPanel.SetActive(false);

            // إعادة تفعيل عناصر تحكم الجوال عند انتهاء التدفق بنجاح
            ShowMobileControls();

            // إرسال الدليل لمدير الأدلة لتسجيله في الـ Inventory والعداد
            if (EvidenceManager.Instance != null)
            {
                EvidenceManager.Instance.CollectEvidence(activeEvidence);
            }

            // إرسال الدليل لمدير الملفات لاستبعاد المشتبه به وتحديث العداد
            if (CaseDossierManager.Instance != null)
            {
                CaseDossierManager.Instance.ProcessEvidenceCollected(activeEvidence);
            }

            // إخفاء كائن الدليل من الغرفة نهائياً
            if (activeEvidence != null)
            {
                activeEvidence.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("<color=red>[INCORRECT] Wrong answer! Re-check the evidence details.</color>");
            if (quizPopupPanel != null) quizPopupPanel.SetActive(false);
            ShowMobileControls();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("INCORRECT RESULT // RE-EXAMINE THE CLUE", 3.5f);
            }
        }

        isAnsweringQuiz = false;
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
}
