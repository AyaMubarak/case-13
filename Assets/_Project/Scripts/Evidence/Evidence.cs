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
        // التأكد من وجود كوليدر على كائن الدليل أو أطفاله ليتم رصده بنصف قطر تفاعل اللاعب
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.5f, 0.5f);
            box.isTrigger = true;
        }
    }

    public string InteractionPrompt
    {
        get
        {
            var profile = PlayerProfile.LocalInstance;
            if (profile != null && requiredRole != DetectiveRole.Any && profile.CurrentRole != requiredRole)
            {
                return $"[LOCKED] Requires {requiredRole}";
            }
            return $"{promptMessage} {evidenceName} ({requiredRole})";
        }
    }

    public void Interact()
    {
        // التحقق من دور اللاعب الحالي (PlayerProfile) مطابق لمتطلبات الدليل أم لا
        var profile = PlayerProfile.LocalInstance;
        if (profile != null && requiredRole != DetectiveRole.Any && profile.CurrentRole != requiredRole)
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
