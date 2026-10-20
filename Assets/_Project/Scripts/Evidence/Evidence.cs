using UnityEngine;
using UnityEngine.Video;

public enum EvidenceType
{
    Physical,
    Digital,
    Document
}

public class Evidence : MonoBehaviour, IInteractable
{
    [Header("Evidence Info")]
    public string evidenceID = "EV_01";
    public string evidenceName = "Blood Sample";
    public EvidenceType type = EvidenceType.Physical;
    [TextArea(2, 4)] public string description = "Suspicious blood drop found near crime scene.";

    [Header("Role Restriction")]
    public DetectiveRole requiredRole = DetectiveRole.Any; // الدور المطلوب لجمع هذا الدليل (مثلاً ForensicsExpert)

    [Header("Cinematic Video Clip")]
    public VideoClip evidenceVideoClip;

    [Header("Elimination & Quiz Data")]
    public string eliminatesSuspectID = "SUS_MARCUS";
    [TextArea(2, 4)] public string eliminationFinding = "Blood Type B+ does not match Marcus Vance (O-)";

    [Header("Quiz Options")]
    public string quizQuestion = "What is the detected blood type result?";
    public string[] options = { "A+", "B+", "O-", "AB+" };
    public int correctOptionIndex = 1; // رقم الخيار الصحيح (مثلاً B+)

    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Press [E] to Inspect";

    private void Awake()
    {
        // إزالة أي MeshCollider تالف بدون شبكة (null mesh) لمنع تعطل كشف التصادم
        foreach (var mc in GetComponentsInChildren<MeshCollider>(true))
        {
            if (mc != null && mc.sharedMesh == null)
            {
                Destroy(mc);
            }
        }

        // التأكد من وجود SphereCollider بنصف قطر مريح (0.5m) لضمان التفاعل من فوق الطاولات
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
        {
            sphere = gameObject.AddComponent<SphereCollider>();
        }
        sphere.isTrigger = true;
        sphere.radius = Mathf.Max(sphere.radius, 0.5f);
    }

    public bool CanInteractWithCurrentRole()
    {
        if (requiredRole == DetectiveRole.Any) return true;

        var profile = PlayerProfile.LocalInstance;
        if (profile == null) return false;

        return profile.CurrentRole == requiredRole;
    }

    public string InteractionPrompt
    {
        get
        {
            if (!CanInteractWithCurrentRole())
            {
                return $"[LOCKED] Requires {requiredRole}";
            }
            return $"{promptMessage} {evidenceName}";
        }
    }

    public void Interact()
    {
        // التحقق من صلاحية الدور
        if (!CanInteractWithCurrentRole())
        {
            Debug.LogWarning($"<color=red>Access Denied!</color> This evidence requires role: {requiredRole}");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification($"ACCESS DENIED // REQUIRES {requiredRole.ToString().ToUpper()}");
            }
            return;
        }

        // استدعاء مدير الفيديوهات والأسئلة فور الضغط على الدليل
        if (EvidenceCutsceneManager.Instance != null)
        {
            EvidenceCutsceneManager.Instance.PlayEvidenceFlow(this);
        }
        else
        {
            CollectDirectly();
        }
    }

    public void CollectDirectly()
    {
        Debug.Log($"<color=cyan>[EVIDENCE COLLECTED]</color> Name: {evidenceName}");

        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.CollectEvidence(this);
        }

        if (CaseDossierManager.Instance != null)
        {
            CaseDossierManager.Instance.ProcessEvidenceCollected(this);
        }

        gameObject.SetActive(false);
    }
}
