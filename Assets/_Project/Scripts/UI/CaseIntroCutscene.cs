using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;

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
    private GameplayMusicController gameplayMusic;

    // يتذكر المشاهد التي تم عرض أو تخطي الفيديو الخاص بها خلال الجلسة حتى لا يتكرر أبداً!
    private static readonly HashSet<string> completedIntroScenes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>True while the intro plays. Movement, camera look, interaction and evidence collection check this.</summary>
    public static bool IsPlaying { get; private set; }

    public static void ResetCompletedIntrosForNewMatch()
    {
        completedIntroScenes.Clear();
    }

    private static string CurrentSceneIntroRoomKey
    {
        get
        {
            bool isCase2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02");
            return isCase2 ? "IntroDone_Case02" : "IntroDone_Case01";
        }
    }

    private static bool HasIntroAlreadyFinishedInRoomOrSession(string sceneName)
    {
        if (completedIntroScenes.Contains(sceneName))
            return true;

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(CurrentSceneIntroRoomKey, out object doneObj) &&
                doneObj is bool done && done)
            {
                completedIntroScenes.Add(sceneName);
                return true;
            }
        }

        if (EvidenceManager.Instance != null && EvidenceManager.Instance.CollectedCount > 0)
        {
            completedIntroScenes.Add(sceneName);
            return true;
        }

        return false;
    }

    private void Awake()
    {
        gameplayMusic = FindAnyObjectByType<GameplayMusicController>(FindObjectsInactive.Include);

        ResolveRuntimeReferences();

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // A Case 1 intro can survive or exist as a deactivated duplicate in Case 2.
        // Never allow that old video to run or cover Case 2.
        if (activeScene.Contains("Case02") &&
            (gameObject.name == "CutscenePanel" ||
             (videoPlayer != null && videoPlayer.clip != null && !videoPlayer.clip.name.Contains("e41c4fb5"))))
        {
            if (videoPlayer != null)
            {
                videoPlayer.isLooping = false;
                videoPlayer.playOnAwake = false;
                videoPlayer.Stop();
                videoPlayer.enabled = false;
            }
            if (cutsceneUIPanel != null && cutsceneUIPanel.name == "CutscenePanel")
                cutsceneUIPanel.SetActive(false);
            enabled = false;
            return;
        }

        // إذا تم عرض الفيديو أو عمل Skip له مسبقاً في هذه القضية، نمنع تشغيله نهائياً!
        if (HasIntroAlreadyFinishedInRoomOrSession(activeScene))
        {
            ForceSilenceAndHideImmediately();
            return;
        }

        if (videoPlayer == null)
        {
            isCutsceneFinished = true;
            IsPlaying = false;
            if (gameplayMusic != null) gameplayMusic.ResumeAfterOverlay();
            return;
        }

        if (gameplayMusic != null) gameplayMusic.PauseForOverlay();

        // ضمان عدم التكرار التلقائي للفيديو أبداً
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;

        IsPlaying = true; // قبل أي Start في باقي السكربتات

        EnsureFullscreenLayout();
        ResolveSkipButton();

        if (skipButton != null)
        {
            BindSkipActionToButton(skipButton);
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEndReached;
            videoPlayer.loopPointReached += OnVideoEndReached;
        }
    }

    private void BindSkipActionToButton(Button btn)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(EndCutscene);
        btn.onClick.AddListener(EndCutscene);

        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener((_) => EndCutscene());
        trigger.triggers.Add(clickEntry);

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((_) => EndCutscene());
        trigger.triggers.Add(downEntry);
    }

    private void ForceSilenceAndHideImmediately()
    {
        isCutsceneFinished = true;
        IsPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
            videoPlayer.enabled = false;
            videoPlayer.gameObject.SetActive(false);
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }

        if (cutsceneUIPanel != null)
        {
            cutsceneUIPanel.SetActive(false);
        }

        string[] introRoots = { "Case2_IntroCanvas", "Case2_IntroPanel", "Case2_IntroVideo", "CutscenePanel" };
        foreach (string rootName in introRoots)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t != null && t.name == rootName)
                    t.gameObject.SetActive(false);
            }
        }

        if (gameplayMusic != null) gameplayMusic.ResumeAfterOverlay();
        ToggleGameplay(true);
        enabled = false;
    }

    private void ResolveRuntimeReferences()
    {
        if (cutsceneUIPanel == null) cutsceneUIPanel = FindNamedObject("Case2_IntroCanvas", "Case2_IntroPanel", "CutscenePanel");
        if (videoPlayer == null)
        {
            foreach (VideoPlayer player in FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include))
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
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            foreach (string name in names)
                if (t.name == name) return t.gameObject;
        return null;
    }

    private void Start()
    {
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (isCutsceneFinished || HasIntroAlreadyFinishedInRoomOrSession(activeScene))
        {
            EndCutscene();
            return;
        }

        if (videoPlayer == null || (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url)))
        {
            EndCutscene();
            return;
        }
        StartCutscene();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEndReached;
        }
        IsPlaying = false;
    }

    private void LateUpdate()
    {
        if (!IsPlaying || isCutsceneFinished) return;

        // إذا أنهى أحد الزملاء أو ضغط Skip أو تم تحديث حالة الغرفة أثناء العرض، ينتهي فوراً على كل الشاشات
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (HasIntroAlreadyFinishedInRoomOrSession(activeScene))
        {
            EndCutscene();
            return;
        }

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
        if (isCutsceneFinished) return;
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (HasIntroAlreadyFinishedInRoomOrSession(activeScene))
        {
            EndCutscene();
            return;
        }

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
            videoPlayer.isLooping = false;
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
        IsPlaying = false;

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        completedIntroScenes.Add(activeScene);

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { CurrentSceneIntroRoomKey, true }
            });
        }

        if (gameplayMusic != null) gameplayMusic.ResumeAfterOverlay();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEndReached;
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.enabled = false;
            videoPlayer.gameObject.SetActive(false);
        }

        // إخفاء واجهة الفيديو
        if (cutsceneUIPanel != null)
        {
            cutsceneUIPanel.SetActive(false);
        }

        // إغلاق وتعطيل كافة كائنات الفيديو الانترو في المشهد حتى لا يعيد أي مكون تشغيلها أبداً
        string[] introRoots = { "Case2_IntroCanvas", "Case2_IntroPanel", "Case2_IntroVideo", "CutscenePanel" };
        foreach (string rootName in introRoots)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t != null && t.name == rootName)
                    t.gameObject.SetActive(false);
            }
        }

        foreach (CaseIntroCutscene otherIntro in Object.FindObjectsByType<CaseIntroCutscene>(FindObjectsInactive.Include))
        {
            if (otherIntro != null)
            {
                otherIntro.isCutsceneFinished = true;
                otherIntro.enabled = false;
            }
        }

        foreach (VideoPlayer player in Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include))
        {
            if (player == null) continue;
            string n = player.gameObject.name.ToLowerInvariant();
            if (n.Contains("intro") || n == "case2_introvideoplayer" || n == "cutscenepanel")
            {
                player.isLooping = false;
                player.playOnAwake = false;
                player.Stop();
                player.clip = null;
                player.enabled = false;
                player.gameObject.SetActive(false);
            }
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }

        // تفعيل التحكم والبدء الفعلي باللعب
        ToggleGameplay(true);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartInvestigationTimer();
            UIManager.Instance.SetMobileControlsActive(true);
            if (UIManager.Instance.inGameHUD != null) UIManager.Instance.inGameHUD.SetActive(true);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        enabled = false;
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
        if (cutsceneUIPanel != null)
        {
            Button[] buttons = cutsceneUIPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name.ToLowerInvariant().Contains("skip"))
                {
                    if (skipButton == null)
                    {
                        skipButton = button;
                    }
                    else if (skipButton != button)
                    {
                        // Hide duplicate skip buttons in the scene
                        button.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Also search globally in the scene to suppress any other rogue skip buttons
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (b != null && b.name.ToLowerInvariant().Contains("skip") && b != skipButton)
            {
                if (b.transform.IsChildOf(transform) || (cutsceneUIPanel != null && b.transform.IsChildOf(cutsceneUIPanel.transform)))
                {
                    b.gameObject.SetActive(false);
                }
            }
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.transform.SetAsLastSibling();
            BindSkipActionToButton(skipButton);

            // ربط كل أزرار Skip الموجودة داخل لوحة الانترو بنفس الدالة لضمان عمل الزر من أول ضغطة
            if (cutsceneUIPanel != null)
            {
                foreach (Button b in cutsceneUIPanel.GetComponentsInChildren<Button>(true))
                {
                    if (b != null && b.name.ToLowerInvariant().Contains("skip"))
                    {
                        BindSkipActionToButton(b);
                    }
                }
            }
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
