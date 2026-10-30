using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class CaseIntroCutscene : MonoBehaviour
{
    [Header("Video Setup")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private GameObject cutsceneUIPanel;

    [Header("Controls")]
    [SerializeField] private Button skipButton;

    [Header("Gameplay Transition")]
    [SerializeField] private MonoBehaviour[] scriptsToEnable; // مثل PlayerMovement, CaseBriefingUI

    private bool isCutsceneFinished = false;

    /// <summary>True while the intro plays. Movement, camera look, interaction and evidence collection check this.</summary>
    public static bool IsPlaying { get; private set; }

    private void Awake()
    {
        ResolveRuntimeReferences();

        // A Case 1 intro can survive in DontDestroyOnLoad when testing the
        // transition locally. Never allow that video to cover Case 2.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02") &&
            videoPlayer != null && videoPlayer.clip != null &&
            !videoPlayer.clip.name.Contains("e41c4fb5"))
        {
            videoPlayer.Stop();
            if (cutsceneUIPanel != null) cutsceneUIPanel.SetActive(false);
            enabled = false;
            IsPlaying = false;
            return;
        }

        if (videoPlayer == null)
        {
            isCutsceneFinished = true;
            IsPlaying = false;
            return;
        }

        IsPlaying = true; // قبل أي Start في باقي السكربتات

        EnsureFullscreenLayout();
        ResolveSkipButton();

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(EndCutscene);
        }

        if (videoPlayer != null)
        {
            // استدعاء إنهاء الفيديو تلقائياً عند وصوله للنهاية
            videoPlayer.loopPointReached += OnVideoEndReached;
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (cutsceneUIPanel == null) cutsceneUIPanel = FindNamedObject("Case2_IntroCanvas", "Case2_IntroPanel", "CutscenePanel");
        if (videoPlayer == null)
        {
            foreach (VideoPlayer player in FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (player.gameObject.name.ToLowerInvariant().Contains("intro") || player.gameObject.name.ToLowerInvariant().Contains("video"))
                {
                    videoPlayer = player;
                    break;
                }
            }
        }
        if (videoDisplay == null && videoPlayer != null)
            videoDisplay = videoPlayer.GetComponentInChildren<RawImage>(true);
    }

    private static GameObject FindNamedObject(params string[] names)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (string name in names)
                if (t.name == name) return t.gameObject;
        return null;
    }

    private void Start()
    {
        if (videoPlayer == null || (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url)))
        {
            EndCutscene();
            return;
        }
        StartCutscene();
    }

    private void OnDestroy()
    {
        IsPlaying = false;
    }

    private void LateUpdate()
    {
        if (!IsPlaying) return;

        // Some MP4 files do not reliably raise loopPointReached on Android/
        // Unity 6. Finish by time as a fallback so the player is never stuck.
        if (videoPlayer != null && videoPlayer.isPrepared && videoPlayer.length > 0.05 &&
            videoPlayer.time >= videoPlayer.length - 0.08)
        {
            EndCutscene();
            return;
        }

        // إبقاء الـ HUD وأزرار الموبايل مخفية والماوس حراً (للضغط على Skip) طوال العرض
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(false);
            if (UIManager.Instance.inGameHUD != null && UIManager.Instance.inGameHUD.activeSelf)
                UIManager.Instance.inGameHUD.SetActive(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartCutscene()
    {
        isCutsceneFinished = false;
        IsPlaying = true;

        // تعطيل حركة اللاعب وأي واجهات أخرى أثناء العرض
        ToggleGameplay(false);

        if (cutsceneUIPanel != null)
        {
            cutsceneUIPanel.SetActive(true);
            EnsureFullscreenLayout();
        }
        if (videoDisplay != null)
            videoDisplay.gameObject.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    private void OnVideoEndReached(VideoPlayer vp)
    {
        EndCutscene();
    }

    public void EndCutscene()
    {
        if (isCutsceneFinished) return;
        isCutsceneFinished = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // إخفاء واجهة الفيديو
        if (cutsceneUIPanel != null)
        {
            cutsceneUIPanel.SetActive(false);
        }

        // Also close any authored/duplicated Case 2 intro roots left by an
        // earlier setup pass. Otherwise the last video frame remains visible
        // above the gameplay HUD after the video ends.
        string[] introRoots = { "Case2_IntroCanvas", "Case2_IntroPanel", "Case2_IntroVideo" };
        foreach (string rootName in introRoots)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == rootName)
                    t.gameObject.SetActive(false);
            }
        }

        foreach (VideoPlayer player in Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = player.gameObject.name.ToLowerInvariant();
            if (n.Contains("intro") || n == "case2_introvideoplayer")
            {
                player.Stop();
                player.gameObject.SetActive(false);
            }
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }

        // تفعيل التحكم والبدء الفعلي باللعب
        IsPlaying = false;
        ToggleGameplay(true);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMobileControlsActive(true);
            if (UIManager.Instance.inGameHUD != null) UIManager.Instance.inGameHUD.SetActive(true);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ToggleGameplay(bool enableState)
    {
        if (scriptsToEnable != null)
        {
            foreach (var script in scriptsToEnable)
            {
                if (script != null) script.enabled = enableState;
            }
        }
    }

    private void ResolveSkipButton()
    {
        if (skipButton == null && cutsceneUIPanel != null)
        {
            Button[] buttons = cutsceneUIPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name.ToLowerInvariant().Contains("skip"))
                {
                    skipButton = button;
                    break;
                }
            }
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.transform.SetAsLastSibling();
        }
    }

    private void EnsureFullscreenLayout()
    {
        Canvas canvas = cutsceneUIPanel != null ? cutsceneUIPanel.GetComponentInParent<Canvas>(true) : null;
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            scaler.matchWidthOrHeight = aspect < 1.5f ? 0f : 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Stretch(cutsceneUIPanel != null ? cutsceneUIPanel.transform as RectTransform : null);
        Stretch(videoDisplay != null ? videoDisplay.rectTransform : null);

        if (videoDisplay != null)
        {
            videoDisplay.raycastTarget = false;

            // RawImage has no preserveAspect property. AspectRatioFitter is
            // the correct responsive equivalent for video textures.
            AspectRatioFitter aspect = videoDisplay.GetComponent<AspectRatioFitter>();
            if (aspect == null) aspect = videoDisplay.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 16f / 9f;
        }

        // Same layout as Case 1: white button, top-right, safe anchored margins.
        if (skipButton != null)
        {
            RectTransform buttonRect = skipButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(1f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(1f, 1f);
                buttonRect.anchoredPosition = new Vector2(-30f, -30f);
                buttonRect.sizeDelta = new Vector2(200f, 50f);
                buttonRect.localScale = Vector3.one;
            }

            Image image = skipButton.GetComponent<Image>();
            if (image != null) image.color = Color.white;

            TMP_Text tmp = skipButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null && skipButton.GetComponentInChildren<Text>(true) == null)
            {
                GameObject textObject = new GameObject("SkipLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(skipButton.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                tmp = textObject.GetComponent<TextMeshProUGUI>();
            }
            if (tmp != null)
            {
                tmp.text = "SKIP >>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 24f;
                tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                tmp.raycastTarget = false;
            }

            Text legacy = skipButton.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                // Do not render two labels on the same button. TMP is the
                // preferred label; hide the legacy UI.Text if both exist.
                if (tmp != null)
                {
                    legacy.text = string.Empty;
                    legacy.enabled = false;
                }
                else
                {
                    legacy.text = "SKIP >>";
                    legacy.alignment = TextAnchor.MiddleCenter;
                    legacy.fontSize = 24;
                    legacy.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    legacy.raycastTarget = false;
                }
            }
        }
    }

    private static void Stretch(RectTransform target)
    {
        if (target == null) return;
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
