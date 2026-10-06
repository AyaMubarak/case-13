using UnityEngine;

public enum EvidenceType
{
    Physical,
    Digital,
    Document
}

public class Evidence : MonoBehaviour, IInteractable
{
    [Header("Role Requirement")]
    [Tooltip("الدور المخول بجمع هذا الدليل")]
    public DetectiveRole requiredRole = DetectiveRole.Any;

    [Header("Evidence Details")]
    public string evidenceID = "";
    public string evidenceName = "";
    public EvidenceType type = EvidenceType.Physical;

    [TextArea(3, 5)]
    public string description = "";

    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Press [E] to Collect Evidence";

    public string InteractionPrompt
    {
        get
        {
            if (PlayerProfile.LocalPlayer != null &&
                requiredRole != DetectiveRole.Any &&
                PlayerProfile.LocalPlayer.currentRole != requiredRole)
            {
                return $"[LOCKED] Requires {requiredRole}";
            }
            return promptMessage;
        }
    }

    public void Interact()
    {
        // فحص مطابقة الدور مع تخصص المحقق
        if (PlayerProfile.LocalPlayer != null && requiredRole != DetectiveRole.Any)
        {
            if (PlayerProfile.LocalPlayer.currentRole != requiredRole)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOnScreenNotification($"ACCESS DENIED // REQUIRES {requiredRole.ToString().ToUpper()}");
                }
                return;
            }
        }

        Collect();
    }

    private void Collect()
    {
        Debug.Log($"<color=cyan>[EVIDENCE COLLECTED]</color> Name: {evidenceName}");

        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.CollectEvidence(this);
        }
        else
        {
            Debug.LogError("[DEBUG] EvidenceManager.Instance is NULL!");
        }

        gameObject.SetActive(false);
    }
}