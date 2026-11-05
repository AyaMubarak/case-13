using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// شاشة اختيار طور التحقيق (Investigation Protocol Selection)
/// تدعم الانتقال السلس والسريع مع شاشة تنبيه انتظار فخمة (Loading Overlay Alert)
/// تمنع التجميد وتبين للاعب حالة الاتصال والتحميل بدقة وأناقة بصرية.
/// </summary>
public class GameModeSelectUI : MonoBehaviour
{
    public static GameModeSelectUI Instance { get; private set; }

    [Header("Panels Navigation")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject characterSelectPanel;

    private GameObject modePanelRoot;
    private int selectedMode = 0; // 0 = Solo, 1 = Co-op

    // مؤشر الإضاءة البصرية الخفيفة (Highlight) لإظهار أي وضع هو المختار حالياً
    private Image soloHighlight;
    private Image coopHighlight;

    // عناصر نافذة تنبيه الانتظار الأنيقة (Loading & Status Overlay)
    private GameObject waitingOverlay;
    private TextMeshProUGUI waitingTitleText;
    private TextMeshProUGUI waitingDescText;
    private TextMeshProUGUI waitingSpinnerDots;
    private Coroutine spinnerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
    }

    private void ResolvePanels()
    {
        if (mainMenuPanel == null)
        {
            MainMenuUI menu = FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (menu != null && menu.mainMenuPanel != null) mainMenuPanel = menu.mainMenuPanel;
        }

        if (characterSelectPanel == null)
        {
            CharacterSelectManager selector = FindAnyObjectByType<CharacterSelectManager>(FindObjectsInactive.Include);
            if (selector != null) characterSelectPanel = selector.gameObject;
            else
            {
                MainMenuUI menu = FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
                if (menu != null && menu.characterSelectPanel != null) characterSelectPanel = menu.characterSelectPanel;
            }
        }
    }

    public void Open()
    {
        ResolvePanels();
        if (modePanelRoot == null) BuildUI();
        if (modePanelRoot != null)
        {
            modePanelRoot.transform.SetAsLastSibling();
            modePanelRoot.SetActive(true);
            UpdateSelectionVisuals();
            HideWaitingAlert();
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
    }

    public void Close()
    {
        HideWaitingAlert();
        if (modePanelRoot != null) modePanelRoot.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void SelectSoloMode()
    {
        selectedMode = 0;
        UpdateSelectionVisuals();
        Debug.Log("<color=cyan>[GAME MODE] Solo Protocol Selected.</color>");
    }

    public void SelectCoopMode()
    {
        selectedMode = 1;
        UpdateSelectionVisuals();
        Debug.Log("<color=cyan>[GAME MODE] Team Co-Op Protocol Selected.</color>");
    }

    public void ExecuteSoloMode()
    {
        selectedMode = 0;
        UpdateSelectionVisuals();
        Debug.Log("<color=green>[GAME MODE] Direct Solo Entry. Preparing case scene...</color>");

        PlayerPrefs.SetInt("Game_Mode", 0); // 0 = Solo
        // كل جلسة لعب جديدة تبدأ من القضية الأولى، ولا ترث آخر قضية محفوظة.
        PlayerPrefs.SetString("Selected_Case", "Case01_Main");
        PlayerPrefs.SetString("Player_Name", "Lead Detective");
        PlayerPrefs.SetString("Player_Role", "Master Investigator");
        PlayerPrefs.SetString("Player_Gender", "Male");
        PlayerPrefs.Save();

        // إظهار تنبيه الانتظار فورا لمنع أي شعور بالتأخير أو التجمد
        ShowWaitingAlert(
            "INITIALIZING INVESTIGATION",
            "Loading crime scene and case evidence archives..."
        );

        string caseScene = PlayerPrefs.GetString("Selected_Case", "Case01_Main");
        StartCoroutine(LoadSceneAsyncRoutine(caseScene));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        // إعطاء إطار واحد للواجهة لترسم الـ Alert فوراً للمستخدم
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                if (waitingDescText != null)
                {
                    int progress = Mathf.Clamp((int)(asyncLoad.progress * 100), 10, 100);
                    waitingDescText.text = $"Deploying detective to crime scene... <b><color=#00E5FF>{progress}%</color></b>";
                }
                yield return null;
            }
        }
    }

    public void ExecuteCoopMode()
    {
        selectedMode = 1;
        UpdateSelectionVisuals();
        Debug.Log("<color=yellow>[GAME MODE] Co-Op selected. Navigating to Name & Role Selection...</color>");

        PlayerPrefs.SetInt("Game_Mode", 1); // 1 = Co-op
        // كل جلسة لعب جديدة تبدأ من القضية الأولى، ولا ترث آخر قضية محفوظة.
        PlayerPrefs.SetString("Selected_Case", "Case01_Main");
        PlayerPrefs.Save();

        // إظهار تنبيه الانتظار اللطيف ثم التبديل بسلاسة للشاشة التالية
        ShowWaitingAlert(
            "ESTABLISHING SECURE PROTOCOL",
            "Connecting to central squad network and detectives roster..."
        );

        StartCoroutine(TransitionToCoopRoutine());
    }

    private IEnumerator TransitionToCoopRoutine()
    {
        // إعطاء وميض بصري سلس ومريح لمدة 0.35 ثانية
        yield return new WaitForSecondsRealtime(0.35f);

        HideWaitingAlert();
        ResolvePanels();
        if (modePanelRoot != null) modePanelRoot.SetActive(false);

        if (characterSelectPanel != null)
        {
            characterSelectPanel.transform.SetAsLastSibling();
            characterSelectPanel.SetActive(true);
        }
        else
        {
            MainMenuUI menu = FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (menu != null) menu.OpenCharacterSelect();
        }
    }

    public void OnConfirmAndStartClicked()
    {
        if (selectedMode == 0)
        {
            ExecuteSoloMode();
        }
        else
        {
            ExecuteCoopMode();
        }
    }

    private void UpdateSelectionVisuals()
    {
        // إضاءة خفيفة وشفافة جداً على البطاقة النشطة ليعرف اللاعب أي كرت هو المحدد
        if (soloHighlight != null)
        {
            soloHighlight.color = (selectedMode == 0) ? new Color(1f, 0.65f, 0.1f, 0.15f) : Color.clear;
        }
        if (coopHighlight != null)
        {
            coopHighlight.color = (selectedMode == 1) ? new Color(0.18f, 0.82f, 1f, 0.15f) : Color.clear;
        }
    }

    // ==========================================
    // نافذة تنبيه الانتظار الأنيقة (WAITING ALERT)
    // ==========================================

    public void ShowWaitingAlert(string title, string description)
    {
        CreateWaitingAlertUI();
        if (waitingOverlay == null) return;

        if (waitingTitleText != null) waitingTitleText.text = $"<i><b><color=#00E5FF>{title}</color></b></i>";
        if (waitingDescText != null) waitingDescText.text = description;

        waitingOverlay.transform.SetAsLastSibling();
        waitingOverlay.SetActive(true);

        if (spinnerCoroutine != null) StopCoroutine(spinnerCoroutine);
        spinnerCoroutine = StartCoroutine(SpinnerAnimationRoutine());
    }

    public void HideWaitingAlert()
    {
        if (spinnerCoroutine != null)
        {
            StopCoroutine(spinnerCoroutine);
            spinnerCoroutine = null;
        }

        if (waitingOverlay != null)
        {
            waitingOverlay.SetActive(false);
        }
    }

    private IEnumerator SpinnerAnimationRoutine()
    {
        string[] dots = { "• • •", "•• • ••", "••• •••", "••••", "••• •••", "•• • ••" };
        int idx = 0;
        while (true)
        {
            if (waitingSpinnerDots != null)
            {
                waitingSpinnerDots.text = $"<color=#00E5FF>[ {dots[idx % dots.Length]} ]</color>";
            }
            idx++;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private void CreateWaitingAlertUI()
    {
        if (waitingOverlay != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (parentCanvas == null) return;

        // 1. طبقة التعتيم الخلفية السينمائية
        waitingOverlay = new GameObject("WaitingOverlayAlert", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        waitingOverlay.transform.SetParent(parentCanvas.transform, false);

        RectTransform overlayRect = waitingOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImg = waitingOverlay.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.04f, 0.07f, 0.88f); // داكن أنيق شبه شفاف

        // 2. بطاقة التنبيه المركزية (Alert Box)
        GameObject boxObj = new GameObject("AlertCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.transform.SetParent(waitingOverlay.transform, false);

        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(560f, 240f);

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f); // خلفية تقنية أنيقة

        // إطار نيون أزرق سماوي مضيء خفيف (Cyan Border Outline)
        GameObject borderObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(boxObj.transform, false);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2f, -2f);
        borderRect.offsetMax = new Vector2(2f, 2f);
        Image borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0f, 0.85f, 1f, 0.65f);
        borderObj.transform.SetAsFirstSibling();

        // 3. شريط الرأس والرمز الفخري (Header Accent)
        GameObject headerBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        headerBar.transform.SetParent(boxObj.transform, false);
        RectTransform headerRect = headerBar.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 4f);
        Image headerImg = headerBar.GetComponent<Image>();
        headerImg.color = new Color(0f, 0.9f, 1f, 1f);

        // 4. العنوان الرئيسي (Title)
        GameObject titleObj = new GameObject("WaitingTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.06f, 0.60f);
        titleRt.anchorMax = new Vector2(0.94f, 0.92f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        waitingTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        waitingTitleText.fontSize = 24f;
        waitingTitleText.alignment = TextAlignmentOptions.Center;
        waitingTitleText.text = "<b><color=#00E5FF>PROCESSING REQUEST</color></b>";

        // 5. النص التوضيحي للحالة (Status Description)
        GameObject descObj = new GameObject("WaitingDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(boxObj.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.06f, 0.28f);
        descRt.anchorMax = new Vector2(0.94f, 0.60f);
        descRt.offsetMin = Vector2.zero;
        descRt.offsetMax = Vector2.zero;

        waitingDescText = descObj.GetComponent<TextMeshProUGUI>();
        waitingDescText.fontSize = 17f;
        waitingDescText.alignment = TextAlignmentOptions.Center;
        waitingDescText.color = new Color(0.85f, 0.92f, 1f, 0.95f);
        waitingDescText.text = "Please wait while operation completes...";

        // 6. مؤشر نبضات الانتظار الأنيق (Pulsing Spinner Dots)
        GameObject dotsObj = new GameObject("WaitingDots", typeof(RectTransform), typeof(TextMeshProUGUI));
        dotsObj.transform.SetParent(boxObj.transform, false);
        RectTransform dotsRt = dotsObj.GetComponent<RectTransform>();
        dotsRt.anchorMin = new Vector2(0.06f, 0.08f);
        dotsRt.anchorMax = new Vector2(0.94f, 0.28f);
        dotsRt.offsetMin = Vector2.zero;
        dotsRt.offsetMax = Vector2.zero;

        waitingSpinnerDots = dotsObj.GetComponent<TextMeshProUGUI>();
        waitingSpinnerDots.fontSize = 20f;
        waitingSpinnerDots.alignment = TextAlignmentOptions.Center;
        waitingSpinnerDots.text = "<color=#00E5FF>[ • • • ]</color>";

        waitingOverlay.SetActive(false);
    }

    // ==========================================
    // بناء واجهة اختيار النمط (BUILD UI)
    // ==========================================

    private void BuildUI()
    {
        if (modePanelRoot != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (parentCanvas == null) return;

        // 1. الحاوية الجذرية وخلفية الصورة الأصلية بالكامل
        modePanelRoot = new GameObject("GameModeSelectPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modePanelRoot.transform.SetParent(parentCanvas.transform, false);

        RectTransform rootRect = modePanelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImg = modePanelRoot.GetComponent<Image>();
        // Use the supplied clear artwork as the background; only the transparent
        // Unity hit areas below remain interactive.
        Sprite bgSprite = Resources.Load<Sprite>("InvestigationProtocol_BG_Clear");
        if (bgSprite == null)
            bgSprite = Resources.Load<Sprite>("InvestigationProtocol_BG_HQ");
        if (bgSprite == null)
            bgSprite = Resources.Load<Sprite>("InvestigationProtocol_BG");
        if (bgSprite != null)
        {
            rootImg.sprite = bgSprite;
            rootImg.color = Color.white;
            rootImg.type = Image.Type.Simple;
            rootImg.preserveAspect = false;
        }
        else
        {
            rootImg.color = new Color(0.08f, 0.10f, 0.15f, 1f);
        }

        // 2. زر تفاعلي شفاف لكامل كرت SOLO INVESTIGATION
        GameObject soloCardObj = CreateTransparentButton(modePanelRoot.transform, "Btn_SoloCard_Area",
            new Vector2(0.10f, 0.18f), new Vector2(0.48f, 0.80f), SelectSoloMode);
        soloHighlight = soloCardObj.GetComponent<Image>();

        // 3. زر تفاعلي شفاف فوق زر START SOLO CASE المطبوع بالصورة
        CreateTransparentButton(modePanelRoot.transform, "Btn_StartSoloCase",
            new Vector2(0.13f, 0.22f), new Vector2(0.45f, 0.29f), ExecuteSoloMode);

        // 4. زر تفاعلي شفاف لكامل كرت TEAM CO-OP
        GameObject coopCardObj = CreateTransparentButton(modePanelRoot.transform, "Btn_CoopCard_Area",
            new Vector2(0.52f, 0.18f), new Vector2(0.90f, 0.80f), SelectCoopMode);
        coopHighlight = coopCardObj.GetComponent<Image>();

        // 5. زر تفاعلي شفاف فوق زر ENTER CO-OP LOBBY المطبوع بالصورة
        CreateTransparentButton(modePanelRoot.transform, "Btn_EnterCoopLobby",
            new Vector2(0.55f, 0.22f), new Vector2(0.87f, 0.29f), ExecuteCoopMode);

        // 6. زر تفاعلي شفاف فوق زر Back السفلي يسار
        CreateTransparentButton(modePanelRoot.transform, "Btn_Back_Area",
            new Vector2(0.04f, 0.035f), new Vector2(0.18f, 0.11f), Close);

        // 7. زر تفاعلي شفاف فوق زر BACK TO MAIN MENU في المنتصف بالأسفل
        CreateTransparentButton(modePanelRoot.transform, "Btn_BackToMainMenu_Area",
            new Vector2(0.36f, 0.035f), new Vector2(0.64f, 0.11f), Close);

        // 8. زر تفاعلي شفاف فوق زر CONFIRM & START السفلي يمين
        CreateTransparentButton(modePanelRoot.transform, "Btn_ConfirmAndStart_Area",
            new Vector2(0.75f, 0.035f), new Vector2(0.98f, 0.11f), OnConfirmAndStartClicked);

        CreateWaitingAlertUI();
        modePanelRoot.SetActive(false);
    }

    private GameObject CreateTransparentButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = Color.clear; // شفاف تماماً
        img.raycastTarget = true; // يستقبل اللمس والنقر بدقة

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f); // وميض خفيف جداً عند التمرير
        colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);     // وميض استجابة عند الضغط
        colors.selectedColor = Color.clear;
        btn.colors = colors;

        btn.onClick.AddListener(action);

        return btnObj;
    }
}
