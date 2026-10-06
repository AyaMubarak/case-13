using UnityEngine;
using UnityEngine.EventSystems;

public class MobileCameraLook : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Touch Settings")]
    public float sensitivity = 0.2f;

    [HideInInspector] public Vector2 lookDelta;

    private Vector2 lastTouchPosition;
    private bool isTouching = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isTouching = true;
        lastTouchPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTouching)
            return;

        Vector2 currentPosition = eventData.position;
        Vector2 delta = currentPosition - lastTouchPosition;
        lastTouchPosition = currentPosition;

        lookDelta = delta * sensitivity;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isTouching = false;
        lookDelta = Vector2.zero;
    }

    private void Update()
    {
        if (!isTouching)
        {
            lookDelta = Vector2.zero;
        }
    }
}