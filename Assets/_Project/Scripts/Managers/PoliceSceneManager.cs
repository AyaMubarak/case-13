using UnityEngine;
using UnityEngine.AI;

public class PoliceSceneManager : MonoBehaviour
{
    public Transform chairTarget;       // اسحبي هنا كائن ChairTarget الخاص بهذا الكرسي
    public AudioSource voiceAudio;      // اسحبي هنا صوت الحوار الخاص بالشخصية

    private NavMeshAgent agent;
    private Animator animator;
    private bool hasReachedChair = false;
    private bool dialoguePlayed = false;

    private bool canStartMoving = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // ضبط الارتفاع تلقائياً لكي لا تطير الشخصية
        if (agent != null)
        {
            agent.enabled = false; // تعطيل حتى يبدأ دورها
        }

        RoleSelectionUI.OnPlayerSetupComplete += OnSetupComplete;
    }

    private void OnSetupComplete()
    {
        if (agent != null)
        {
            agent.enabled = true;
        }

        // البدء بالمشي نحو الكرسي
        if (chairTarget != null && agent != null)
        {
            agent.SetDestination(chairTarget.position);
        }
        canStartMoving = true;
    }

    private void OnDestroy()
    {
        RoleSelectionUI.OnPlayerSetupComplete -= OnSetupComplete;
    }

    void Update()
    {
        // التحقق إذا وصلت الشخصية للكرسي
        if (!hasReachedChair && agent != null && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                hasReachedChair = true;
                agent.isStopped = true; // إيقاف الحركة
                agent.enabled = false; // تعطيل الـ NavMesh لتستقر الشخصية بالجلوس

                if (animator != null)
                {
                    animator.SetTrigger("ReachChair"); // تحويل الأنيميشن إلى جلوس
                }
            }
        }

        // التفاعل عند الضغط على زر E (بعد الجلوس)
        if (hasReachedChair && !dialoguePlayed && Input.GetKeyDown(KeyCode.E))
        {
            PlayDialogueAndSound();
        }
    }

    void PlayDialogueAndSound()
    {
        dialoguePlayed = true;

        if (voiceAudio != null && !voiceAudio.isPlaying)
        {
            voiceAudio.Play(); // تشغيل الصوت الواقعي للحوار
            Debug.Log("تم الضغط على E وبدأ صوت الحوار!");
        }
    }
}