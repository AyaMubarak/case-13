using UnityEngine;
using UnityEngine.UI;

// The labels and empty controls are painted into the background. Fit that
// artwork and its live controls together, rather than stretching them separately.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SettingsResponsiveLayout : MonoBehaviour
{
    [SerializeField] private RectTransform artwork;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private RectTransform toggleKnob;
    [SerializeField] private Image toggleTrack;

    private RectTransform panel;
    private Vector2 lastPanelSize;
    private Vector2 lastScreenSize;
    private Rect lastSafeArea;
    private bool lastToggleValue;
    private bool hasToggleValue;

    private void OnEnable()
    {
        panel = transform as RectTransform;
        RefreshLayout();
        UpdateToggle();
    }

    private void LateUpdate()
    {
        if (panel == null) panel = transform as RectTransform;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (screenSize != lastScreenSize || Screen.safeArea != lastSafeArea ||
            (panel != null && panel.rect.size != lastPanelSize))
            RefreshLayout();
        UpdateToggle();
    }

    public void RefreshLayout()
    {
        RefreshLayout(new Vector2(Screen.width, Screen.height), Screen.safeArea);
    }

    // The explicit viewport also lets automated layout checks cover notched
    // phones and tablets without changing the user's Game view or display.
    public void RefreshLayout(Vector2 screenSize, Rect safeArea)
    {
        if (panel == null) panel = transform as RectTransform;
        if (panel == null || artwork == null) return;
        lastScreenSize = screenSize;
        lastSafeArea = safeArea;
        lastPanelSize = panel.rect.size;

        Rect fitted = CalculateArtworkRect(panel.rect, safeArea, screenSize);
        artwork.anchorMin = artwork.anchorMax = new Vector2(0.5f, 0.5f);
        artwork.pivot = new Vector2(0.5f, 0.5f);
        artwork.localScale = Vector3.one;
        artwork.sizeDelta = fitted.size;
        artwork.anchoredPosition = fitted.center - panel.rect.center;
        UpdateToggle();
    }

    public static Rect CalculateArtworkRect(Rect panelRect, Rect safeArea, Vector2 screenSize)
    {
        const float artworkAspect = 3040f / 1408f;
        if (screenSize.x <= 0f || screenSize.y <= 0f ||
            panelRect.width <= 0f || panelRect.height <= 0f)
            return new Rect(panelRect.center, Vector2.zero);

        Rect screen = new Rect(Vector2.zero, screenSize);
        float left = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
        float bottom = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
        float right = Mathf.Clamp(safeArea.xMax, left, screenSize.x);
        float top = Mathf.Clamp(safeArea.yMax, bottom, screenSize.y);
        if (right <= left || top <= bottom)
        {
            left = screen.xMin;
            bottom = screen.yMin;
            right = screen.xMax;
            top = screen.yMax;
        }

        Rect available = Rect.MinMaxRect(
            panelRect.xMin + panelRect.width * left / screenSize.x,
            panelRect.yMin + panelRect.height * bottom / screenSize.y,
            panelRect.xMin + panelRect.width * right / screenSize.x,
            panelRect.yMin + panelRect.height * top / screenSize.y);
        float width = Mathf.Min(available.width, available.height * artworkAspect);
        Vector2 size = new Vector2(width, width / artworkAspect);
        return new Rect(available.center - size * 0.5f, size);
    }

    private void UpdateToggle()
    {
        if (invertYToggle == null || toggleKnob == null || toggleTrack == null) return;
        bool value = invertYToggle.isOn;
        if (hasToggleValue && lastToggleValue == value) return;
        hasToggleValue = true;
        lastToggleValue = value;

        // The knob remains visible in both states; Toggle.graphic is deliberately
        // unassigned so Unity does not fade the off-state knob away.
        toggleKnob.anchorMin = toggleKnob.anchorMax = new Vector2(value ? 0.78f : 0.22f, 0.5f);
        toggleKnob.anchoredPosition = Vector2.zero;
        toggleTrack.color = value
            ? new Color(0.64f, 0.40f, 0.12f, 1f)
            : new Color(0.14f, 0.135f, 0.16f, 1f);
    }
}
