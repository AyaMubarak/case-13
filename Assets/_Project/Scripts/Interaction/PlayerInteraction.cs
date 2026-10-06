using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("نصف قطر دائرة التفاعل حول اللاعب")]
    public float interactionRadius = 2.5f;

    [Header("Mobile Controls UI")]
    public GameObject mobileInteractBtn;

    private IInteractable currentInteractable;

    private void Start()
    {
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
        // فحص الأجسام التفاعلية المحيطة باللاعب مع دعم Triggers
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, ~0, QueryTriggerInteraction.Collide);
        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            // تجاهل اللاعب نفسه
            if (hitCollider.gameObject == gameObject || hitCollider.transform.root == transform)
                continue;

            IInteractable interactable = hitCollider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hitCollider.GetComponent<IInteractable>();
            }

            if (interactable != null)
            {
                float dist = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
            currentInteractable = closestInteractable;
            // أضيفي هذا السطر هنا لرؤية النتيجة في الـ Console:
            Debug.Log("<color=green>تم العثور على دليل قريب جداً:</color> " + currentInteractable.InteractionPrompt);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowInteractionPrompt(currentInteractable.InteractionPrompt);
            }

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