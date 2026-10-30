using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the menu, role selection, lobby and settings UI inside the visible
/// area on phones and tablets. It only runs in menu scenes and never changes
/// gameplay canvases.
/// </summary>
public sealed class ResponsiveMenuUI : MonoBehaviour
{
    private static ResponsiveMenuUI instance;
    private Rect lastSafeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForMenuScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains("MainMenu") && !scene.name.Equals("Lobby")) return;
        if (FindAnyObjectByType<ResponsiveMenuUI>() != null) return;

        GameObject root = new GameObject("ResponsiveMenuUI");
        DontDestroyOnLoad(root);
        root.AddComponent<ResponsiveMenuUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyResponsiveLayout();
    }

    private void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("MainMenu") || scene.name.Equals("Lobby"))
            ApplyResponsiveLayout();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeAreas();
    }

    private void ApplyResponsiveLayout()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        ApplySafeAreas();
    }

    private void ApplySafeAreas()
    {
        lastSafeArea = Screen.safeArea;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;

            RectTransform root = canvas.transform as RectTransform;
            if (root == null) continue;

            // Keep full-screen backgrounds full bleed. Only panels that are
            // already explicitly marked as safe-area roots are inset.
            foreach (Transform child in root)
            {
                if (!child.name.Contains("SafeArea")) continue;
                RectTransform safe = child as RectTransform;
                if (safe == null) continue;

                Vector2 min = Screen.safeArea.position;
                Vector2 max = Screen.safeArea.position + Screen.safeArea.size;
                min.x /= Screen.width; min.y /= Screen.height;
                max.x /= Screen.width; max.y /= Screen.height;
                safe.anchorMin = min;
                safe.anchorMax = max;
                safe.offsetMin = Vector2.zero;
                safe.offsetMax = Vector2.zero;
            }
        }
    }
}
