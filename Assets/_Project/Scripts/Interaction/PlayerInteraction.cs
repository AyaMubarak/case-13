using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("نصف قطر دائرة التفاعل حول اللاعب (مسافة قريبة ومتوازنة)")]
    public float interactionRadius = 1.35f;

    [Header("Mobile Controls UI")]
    public GameObject mobileInteractBtn;

    private IInteractable currentInteractable;

    private void Start()
    {
        // ضبط مسافة التفاعل إلى مسافة معقولة وقريبة (1.35m) تمنع التفاعل من بعيد وتمنع اختفاء التنبيه السريع
        interactionRadius = 1.35f;

        if (mobileInteractBtn != null)
        {
            Button btn = mobileInteractBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(TriggerMobileInteraction);
            }
            mobileInteractBtn.SetActive(false);
        }
    }

    private void Update()
    {
        if (CaseIntroCutscene.IsPlaying || EvidenceCutsceneManager.IsQuizOrVideoActive)
        {
            ClearCurrentInteraction();
            return;
        }

        // مركز فحص التفاعل يكون عند مستوى صدر/وسط اللاعب لضمان التقاط الأدلة الموضوعة على الطاولات والأرض
        Vector3 playerCenter = transform.position + Vector3.up * 0.9f;

        // فحص الأجسام التفاعلية المحيطة باللاعب مع دعم Triggers
        Collider[] hitColliders = Physics.OverlapSphere(playerCenter, interactionRadius, ~0, QueryTriggerInteraction.Collide);
        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            // تجاهل كوليدرات اللاعب نفسه
            if (hitCollider.gameObject == gameObject || hitCollider.transform.root == transform.root)
                continue;

            IInteractable interactable = hitCollider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hitCollider.GetComponent<IInteractable>();
            }

            if (interactable != null)
            {
                // حساب أقرب نقطة على سطح كوليدر الدليل بدلاً من الـ Pivot الداخلي
                Vector3 closestPointOnTarget = hitCollider.ClosestPoint(playerCenter);
                float dist = Vector3.Distance(playerCenter, closestPointOnTarget);

                if (dist <= interactionRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
            currentInteractable = closestInteractable;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowInteractionPrompt(currentInteractable.InteractionPrompt);
            }

            if (mobileInteractBtn != null && !mobileInteractBtn.activeSelf)
            {
                mobileInteractBtn.SetActive(true);
            }

            // التفاعل عند ضغط E على الكيبورد
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExecuteInteraction();
            }
        }
        else
        {
            ClearCurrentInteraction();
        }
    }

    public void TriggerMobileInteraction()
    {
        ExecuteInteraction();
    }

    private void ExecuteInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
            ClearCurrentInteraction();
        }
    }

    private void ClearCurrentInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable = null;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInteractionPrompt();
        }

        if (mobileInteractBtn != null && mobileInteractBtn.activeSelf)
        {
            mobileInteractBtn.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // رسم دائرة التفاعل في نافذة Scene للتأكد من مداها
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}