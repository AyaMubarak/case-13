using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings Responsive UI:
/// - Ensures the Settings screen renders cleanly and accurately across all phones (21:9, 20:9, 19.5:9) and tablets (16:9, 16:10, 4:3).
/// - Uses the original clean detective board image (Settings_BG) as the full background.
/// - Aligns the 3 audio sliders (Master, Music, SFX) and percentage texts strictly to the printed labels on the chalkboard.
/// - Styles the slider track, orange fill, and crisp circular white knob so they never stretch or distort.
/// </summary>
public class SettingsResponsiveLayout : MonoBehaviour
{
    private Rect lastSafeArea;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private static Sprite cachedCircleKnobSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<SettingsResponsiveLayout>() != null) return;
        GameObject go = new GameObject("SettingsResponsiveLayout");
        go.AddComponent<SettingsResponsiveLayout>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        ApplyCleanLayout();
    }

    private void OnEnable()
    {
        ApplyCleanLayout();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyCleanLayout();
        }
    }

    public void ApplyCleanLayout()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        GameObject panelObj = FindObject("SettingsPanel");
        if (panelObj == null) panelObj = FindObject("InGameSettingsPanel");
        if (panelObj == null) return;

        // 1. إزالة أو إخفاء أي لوحات تزيينية يدوية قديمة (Decor, Boards, Tags, Headers)
        Transform oldDecor = panelObj.transform.Find("SettingsDecorRoot");
        if (oldDecor != null)
        {
            Destroy(oldDecor.gameObject);
        }

        HideUnwantedElement("Dropdown_Difficulty");
        HideUnwantedElement("Toggle_InvertY");
        HideUnwantedElement("Difficulty");
        HideUnwantedElement("InvertY");
        HideUnwantedElement("Invert Y Axis");
        HideUnwantedElement("Game_Tab");
        HideUnwantedElement("Btn_GameTab");
        HideUnwantedElement("Image"); // أي مستطيل أو صورة عشوائية قديمة

        foreach (Transform child in panelObj.GetComponentsInChildren<Transform>(true))
        {
            if (child == null) continue;
            string n = child.name.ToLowerInvariant();
            if (n.Contains("difficulty") || n.Contains("inverty") || n.Contains("invert y") || n.Contains("gametab") || n.Contains("decor"))
            {
                child.gameObject.SetActive(false);
            }
        }

        // 2. ضبط لوحة الإعدادات وتطبيق صورة الخلفية الأصلية لتملأ الشاشة بالكامل
        // نتأكد من أن اللوحة تتجاهل أي SafeArea مقصوصة على الأب إن وجدت لتملأ الشاشة كاملة
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localScale = Vector3.one;

            Image panelBg = panelObj.GetComponent<Image>();
            if (panelBg != null)
            {
                Sprite bgSprite = Resources.Load<Sprite>("Settings_BG");
                if (bgSprite != null)
                {
                    panelBg.sprite = bgSprite;
                    panelBg.color = Color.white;
                    panelBg.type = Image.Type.Simple;
                    // Keep the artwork stretched to the full panel so normalized anchors
                    // (0..1) always map to the exact same pixels on phones and tablets.
                    panelBg.preserveAspect = false;
                }
                else
                {
                    panelBg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
                }
                panelBg.raycastTarget = true;
            }
        }

        // 3. وضع السلايدرات الثلاثة بمحاذاة المراكز العمودية الدقيقة لليبلات المطبوعة على الصورة الأصلية:
        // Master Volume center: yNorm = 0.708f
        // Music Volume center:  yNorm = 0.631f
        // SFX Volume center:    yNorm = 0.556f
        SetupAudioSlider(panelObj, "Slider_Master", "Txt_MasterPercent", 0.708f);
        SetupAudioSlider(panelObj, "Slider_Music", "Txt_MusicPercent", 0.631f);
        SetupAudioSlider(panelObj, "Slider_SFX", "Txt_SFXPercent", 0.556f);

        // 4. زر الرجوع في الأسفل يسار بمحاذاة الزر المطبوع
        SetupBackButton(panelObj);
    }

    private void SetupAudioSlider(GameObject panelObj, string sliderName, string percentName, float yNorm)
    {
        GameObject sliderObj = FindObject(sliderName);
        if (sliderObj == null) return;

        // إعادة ربط السلايدر ليكون ابناً مباشراً للـ panelObj
        if (sliderObj.transform.parent != panelObj.transform)
        {
            sliderObj.transform.SetParent(panelObj.transform, false);
        }

        sliderObj.SetActive(true);
        sliderObj.transform.SetAsLastSibling();

        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        if (sliderRt != null)
        {
            // يبدأ بعد نهاية نص Master Volume (الذي ينتهي عند 0.518) وينتهي قبل النسبة المئوية داخل اللوح الأسود
            sliderRt.anchorMin = new Vector2(0.555f, yNorm);
            sliderRt.anchorMax = new Vector2(0.718f, yNorm);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.anchoredPosition = Vector2.zero;
            // ارتفاع ثابت بمنطق Canvas المرجعي (1920x1080) لضمان التناسب التام على كل الهواتف والتابلتات
            sliderRt.sizeDelta = new Vector2(0f, 28f);
            sliderRt.localScale = Vector3.one;

            // ضبط خلفية مسار السلايدر (Track Background) لتكون رفيعة وأنيقة
            Transform bgTr = sliderObj.transform.Find("Background");
            if (bgTr != null)
            {
                RectTransform bgRt = bgTr.GetComponent<RectTransform>();
                if (bgRt != null)
                {
                    bgRt.anchorMin = new Vector2(0f, 0.5f);
                    bgRt.anchorMax = new Vector2(1f, 0.5f);
                    bgRt.pivot = new Vector2(0.5f, 0.5f);
                    bgRt.anchoredPosition = Vector2.zero;
                    bgRt.sizeDelta = new Vector2(0f, 8f); // سمك خط المسار 8 بكسل متناسق
                }
                Image bgImg = bgTr.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = new Color(0.85f, 0.87f, 0.90f, 0.85f);
                }
            }

            // ضبط منطقة التعبئة (Fill Area)
            Transform fillAreaTr = sliderObj.transform.Find("Fill Area");
            if (fillAreaTr != null)
            {
                RectTransform faRt = fillAreaTr.GetComponent<RectTransform>();
                if (faRt != null)
                {
                    faRt.anchorMin = new Vector2(0f, 0.5f);
                    faRt.anchorMax = new Vector2(1f, 0.5f);
                    faRt.pivot = new Vector2(0.5f, 0.5f);
                    faRt.anchoredPosition = Vector2.zero;
                    faRt.sizeDelta = new Vector2(-8f, 8f);
                }
            }

            // ضبط منطقة مقبض السحب (Handle Slide Area)
            Transform handleAreaTr = sliderObj.transform.Find("Handle Slide Area");
            if (handleAreaTr != null)
            {
                RectTransform haRt = handleAreaTr.GetComponent<RectTransform>();
                if (haRt != null)
                {
                    haRt.anchorMin = new Vector2(0f, 0.5f);
                    haRt.anchorMax = new Vector2(1f, 0.5f);
                    haRt.pivot = new Vector2(0.5f, 0.5f);
                    haRt.anchoredPosition = Vector2.zero;
                    haRt.sizeDelta = new Vector2(-10f, 24f);
                }
            }

            Slider sl = sliderObj.GetComponent<Slider>();
            if (sl != null)
            {
                // مسار التعبئة بلون برتقالي ذهبي أنيق
                if (sl.fillRect != null)
                {
                    sl.fillRect.anchorMin = new Vector2(0f, 0f);
                    sl.fillRect.anchorMax = new Vector2(sl.fillRect.anchorMax.x, 1f);
                    sl.fillRect.offsetMin = Vector2.zero;
                    sl.fillRect.offsetMax = Vector2.zero;

                    Image fill = sl.fillRect.GetComponent<Image>();
                    if (fill != null) fill.color = new Color(0.95f, 0.60f, 0.14f, 1f);
                }

                // مقبض السحب (Handle) دائري أبيض متناسق تماماً لا يتمدد عمودياً
                if (sl.handleRect != null)
                {
                    sl.handleRect.anchorMin = new Vector2(sl.handleRect.anchorMin.x, 0.5f);
                    sl.handleRect.anchorMax = new Vector2(sl.handleRect.anchorMax.x, 0.5f);
                    sl.handleRect.pivot = new Vector2(0.5f, 0.5f);
                    sl.handleRect.anchoredPosition = Vector2.zero;
                    sl.handleRect.sizeDelta = new Vector2(22f, 22f); // دائري متساوي العرض والارتفاع

                    Image handle = sl.handleRect.GetComponent<Image>();
                    if (handle != null)
                    {
                        handle.sprite = GetCircleKnobSprite();
                        handle.type = Image.Type.Simple;
                        handle.preserveAspect = true;
                        handle.color = Color.white;
                    }
                }
            }
        }

        // إخفاء أي ليبل نصي قديم لأن الليبل مطبوع أصلاً على الصورة
        Transform oldLabel = panelObj.transform.Find(sliderName + "_Label");
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);

        // وضع رقم النسبة المئوية بجانب السلايدر مباشرة داخل إطار اللوح الأسود (ينتهي قبل 0.77)
        GameObject percentObj = FindObject(percentName);
        if (percentObj != null)
        {
            if (percentObj.transform.parent != panelObj.transform)
            {
                percentObj.transform.SetParent(panelObj.transform, false);
            }

            percentObj.SetActive(true);
            percentObj.transform.SetAsLastSibling();

            RectTransform pRt = percentObj.GetComponent<RectTransform>();
            if (pRt != null)
            {
                pRt.anchorMin = new Vector2(0.724f, yNorm);
                pRt.anchorMax = new Vector2(0.768f, yNorm);
                pRt.pivot = new Vector2(0f, 0.5f);
                pRt.anchoredPosition = Vector2.zero;
                pRt.sizeDelta = new Vector2(0f, 36f);
                pRt.localScale = Vector3.one;
            }

            TextMeshProUGUI pTmp = percentObj.GetComponent<TextMeshProUGUI>();
            if (pTmp != null)
            {
                pTmp.alignment = TextAlignmentOptions.MidlineLeft;
                pTmp.enableAutoSizing = true;
                pTmp.fontSizeMin = 12f;
                pTmp.fontSizeMax = 22f;
                pTmp.fontSize = 20f;
                pTmp.fontStyle = FontStyles.Bold;
                pTmp.color = new Color(0.96f, 0.68f, 0.22f, 1f); // برتقالي ذهبي متناسق
                pTmp.raycastTarget = false;
            }
        }
    }

    private static Sprite GetCircleKnobSprite()
    {
        if (cachedCircleKnobSprite != null) return cachedCircleKnobSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 1.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        cachedCircleKnobSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleKnobSprite;
    }

    private void SetupBackButton(GameObject panelObj)
    {
        GameObject backObj = FindObject("Btn_SettingsBack");
        if (backObj == null) backObj = FindObject("Btn_Back");
        if (backObj != null && backObj.transform.IsChildOf(panelObj.transform))
        {
            backObj.SetActive(true);
            backObj.transform.SetAsLastSibling();

            RectTransform bRt = backObj.GetComponent<RectTransform>();
            if (bRt != null)
            {
                bRt.anchorMin = new Vector2(0.025f, 0.025f);
                bRt.anchorMax = new Vector2(0.175f, 0.125f);
                bRt.pivot = new Vector2(0f, 0f);
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                bRt.localScale = Vector3.one;

                Image btnImg = backObj.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = Color.clear; // شفاف ليتطابق مع زر Back المرسوم على الصورة
                    btnImg.raycastTarget = true;
                }

                // إخفاء النص المكرر إن كان زر الباك مطبوعاً بالفعل
                TextMeshProUGUI tmp = backObj.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = string.Empty;
                    tmp.enabled = false;
                }
                Text leg = backObj.GetComponentInChildren<Text>(true);
                if (leg != null)
                {
                    leg.text = string.Empty;
                    leg.enabled = false;
                }
            }
        }
    }

    private static void HideUnwantedElement(string objectName)
    {
        GameObject go = FindObject(objectName);
        if (go != null)
        {
            go.SetActive(false);
        }
    }

    private static GameObject FindObject(string objectName)
    {
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go != null && go.name.Trim().Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return go;
        }
        return null;
    }
}
