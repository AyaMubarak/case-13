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
    private int joystickPointerId = -1;

    public Vector2 Input
    {
        get { return input; }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // تجاهل أي لمسة تأتي من النصف الأيمن من الشاشة (مخصص للكاميرا فقط)
        if (eventData.position.x > Screen.width * 0.5f)
            return;

        if (joystickPointerId == -1)
        {
            joystickPointerId = eventData.pointerId;
            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // الاستجابة فقط لنفس الإصبع الذي بدأ تحريك الجويستيك
        if (eventData.pointerId != joystickPointerId)
            return;

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

        joystickHandle.anchoredPosition = input * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == joystickPointerId)
        {
            joystickPointerId = -1;
            input = Vector2.zero;
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }

    private void OnDisable()
    {
        joystickPointerId = -1;
        input = Vector2.zero;
        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
    }
}
