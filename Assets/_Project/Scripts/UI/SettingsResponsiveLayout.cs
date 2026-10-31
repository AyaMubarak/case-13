using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Responsive layout for the existing SettingsPanel. It only changes RectTransforms;
/// the existing SettingsManager event bindings remain untouched.
/// </summary>
public sealed class SettingsResponsiveLayout : MonoBehaviour
{
    private Rect lastSafeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!SceneManager.GetActiveScene().name.Contains("MainMenu")) return;
        if (FindAnyObjectByType<SettingsResponsiveLayout>() != null) return;
        new GameObject("SettingsResponsiveLayout").AddComponent<SettingsResponsiveLayout>();
    }

    private void Awake()
    {
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea) Apply();
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        GameObject panelObject = Find("SettingsPanel");
        if (panelObject == null) return;

        RectTransform panel = panelObject.transform as RectTransform;
        if (panel == null) return;

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        bool narrow = aspect < 1.35f;
        panel.sizeDelta = narrow ? new Vector2(900f, 900f) : new Vector2(1420f, 820f);

        Place("Dropdown_Difficulty", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(narrow ? 70f : 250f, narrow ? 270f : 250f), new Vector2(narrow ? 520f : 480f, 72f));
        Place("Toggle_InvertY", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(narrow ? 70f : 250f, narrow ? 170f : 145f), new Vector2(90f, 58f));

        Place("Slider_Master", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(narrow ? 70f : 270f, narrow ? -40f : -70f), new Vector2(narrow ? 560f : 520f, 42f));
        Place("Slider_Music", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(narrow ? 70f : 270f, narrow ? -130f : -170f), new Vector2(narrow ? 560f : 520f, 42f));
        Place("Slider_SFX", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(narrow ? 70f : 270f, narrow ? -220f : -270f), new Vector2(narrow ? 560f : 520f, 42f));

        GameObject back = Find("Btn_SettingsBack");
        if (back != null)
        {
            RectTransform rect = back.transform as RectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(48f, 36f);
            rect.sizeDelta = new Vector2(narrow ? 260f : 300f, 84f);
        }
    }

    private static GameObject Find(string objectName)
    {
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            if (go != null && go.name.Trim() == objectName) return go;
        return null;
    }

    private static void Place(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject go = Find(objectName);
        if (go == null) return;
        RectTransform rect = go.transform as RectTransform;
        if (rect == null) return;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}
