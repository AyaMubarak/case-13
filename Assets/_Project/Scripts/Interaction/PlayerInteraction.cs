using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("نصف قطر دائرة التفاعل حول اللاعب")]
    public float interactionRadius = 1.35f;

    [Header("Mobile Controls UI")]
    public GameObject mobileInteractBtn;

    [Header("Camera Targeting")]
    [Tooltip("يفحص الدليل الموجود أمام الكاميرا حتى لو كان الـ pivot بعيداً عن جسم الدليل")]
    public float cameraInteractionDistance = 1.55f;

    private IInteractable currentInteractable;

    private void Start()
    {
        // Keep the hand prompt local to the evidence. This applies to both
        // Case 1 and Case 2 and prevents collecting from across the room.
        interactionRadius = Mathf.Clamp(interactionRadius, 1.1f, 1.5f);
        cameraInteractionDistance = Mathf.Clamp(cameraInteractionDistance, 1.25f, 1.7f);

        // Find the hand button automatically. The button may be inactive in the
        // authored mobile canvas, so GameObject.Find is not sufficient.
        if (mobileInteractBtn == null)
        {
            foreach (Button candidate in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                string n = candidate.name.ToLowerInvariant();
                if (n.Contains("interact") || n.Contains("hand") || n.Contains("collect") || n.Contains("investigate"))
                {
                    mobileInteractBtn = candidate.gameObject;
                    break;
                }
            }
        }

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

        // Prefer the object directly in front of the player's view. This fixes
        // evidence whose visual mesh/pivot is not centered on its root.
        IInteractable aimedInteractable = FindAimedInteractable();
        if (aimedInteractable != null)
        {
            SetCurrentInteraction(aimedInteractable);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                ExecuteInteraction();
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
            MeshCollider meshCollider = hitCollider as MeshCollider;
            if (meshCollider != null && !meshCollider.convex)
                continue;
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
            SetCurrentInteraction(closestInteractable);

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

    private IInteractable FindAimedInteractable()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, cameraInteractionDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null) return interactable;
        }
        return null;
    }

    private void SetCurrentInteraction(IInteractable interactable)
    {
        currentInteractable = interactable;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowInteractionPrompt(interactable.InteractionPrompt);
        if (mobileInteractBtn != null && !mobileInteractBtn.activeSelf)
            mobileInteractBtn.SetActive(true);
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
