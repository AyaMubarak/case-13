using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(Canvas))]
public class ResponsiveCanvasLayout : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    private CanvasScaler canvasScaler;
    private RectTransform canvasRect;
    private Rect lastSafeArea;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        EnsureCanvasInput();
        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasRect = transform as RectTransform;
        ConfigureCanvasScaler();
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void EnsureCanvasInput()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null && FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            ConfigureCanvasScaler();
        }

        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplySafeArea();
        }
    }

    private void ConfigureCanvasScaler()
    {
        if (canvasScaler == null) return;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        // Lobby and dossier screens are designed horizontally. On narrow phones, matching
        // the width keeps every panel inside the visible area; larger screens keep a balanced scale.
        float aspectRatio = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        canvasScaler.matchWidthOrHeight = aspectRatio < 1.5f ? 0f : 0.5f;
    }

    private void ApplySafeArea()
    {
        if (canvasRect == null) return;

        lastSafeArea = Screen.safeArea;
        RectTransform safeArea = FindSafeAreaContainer();
        if (safeArea == null) return;

        Vector2 anchorMin = lastSafeArea.position;
        Vector2 anchorMax = lastSafeArea.position + lastSafeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeArea.anchorMin = anchorMin;
        safeArea.anchorMax = anchorMax;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
    }

    private RectTransform FindSafeAreaContainer()
    {
        Transform existing = transform.Find("SafeArea");
        return existing as RectTransform;
    }
}
