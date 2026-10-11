using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

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
        IsPlaying = true; // قبل أي Start في باقي السكربتات

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

    private void Start()
    {
        StartCutscene();
    }

    private void OnDestroy()
    {
        IsPlaying = false;
    }

    private void LateUpdate()
    {
        if (!IsPlaying) return;

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

        if (cutsceneUIPanel != null) cutsceneUIPanel.SetActive(true);

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
}
