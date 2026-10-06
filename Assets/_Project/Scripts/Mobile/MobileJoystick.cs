using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;

    public float handleRange = 100f;

    private Vector2 input;

    public Vector2 Input
    {
        get { return input; }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position
        );

        position.x /= joystickBackground.rect.width / 2f;
        position.y /= joystickBackground.rect.height / 2f;

        input = Vector2.ClampMagnitude(position, 1f);

        joystickHandle.anchoredPosition =
            input * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;

        joystickHandle.anchoredPosition = Vector2.zero;
    }
}