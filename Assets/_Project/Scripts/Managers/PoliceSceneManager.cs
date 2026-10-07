using UnityEngine;
using UnityEngine.AI;

public class PoliceSceneManager : MonoBehaviour
{
    public Transform chairTarget;
    public AudioSource voiceAudio;

    private NavMeshAgent agent;
    private Animator animator;

    private bool hasReachedChair = false;
    private bool dialoguePlayed = false;
    private bool canStartMoving = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // تعطيل الـAgent حتى يتم اختيار الشخصية/الدور
        if (agent != null)
        {
            agent.enabled = false;
        }

        RoleSelectionUI.OnPlayerSetupComplete += OnSetupComplete;
    }

    private void OnSetupComplete()
    {
        if (agent == null)
            return;

        // تفعيل الـAgent
        agent.enabled = true;

        // نتأكد أن الـAgent موجود فعلياً على NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "[PoliceSceneManager] NavMeshAgent is enabled but is NOT on a NavMesh."
            );

            canStartMoving = false;
            return;
        }

        canStartMoving = true;

        // المشي نحو الكرسي
        if (chairTarget != null)
        {
            agent.isStopped = false;
            agent.SetDestination(chairTarget.position);

            Debug.Log("[PoliceSceneManager] Moving to chair.");
        }
    }

    private void OnDestroy()
    {
        RoleSelectionUI.OnPlayerSetupComplete -= OnSetupComplete;
    }

    void Update()
    {
        // لا نحاول استخدام NavMeshAgent قبل أن يصبح جاهزاً
        if (canStartMoving &&
            !hasReachedChair &&
            agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            // ننتظر حتى يتم حساب المسار
            if (!agent.pathPending)
            {
                // نتأكد أن هناك مسار فعلي
                if (agent.hasPath &&
                    agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasReachedChair = true;

                    agent.isStopped = true;

                    // تعطيل الـAgent بعد الوصول
                    agent.enabled = false;

                    canStartMoving = false;

                    // تشغيل أنيميشن الجلوس
                    if (animator != null)
                    {
                        animator.SetTrigger("ReachChair");
                    }

                    Debug.Log("[PoliceSceneManager] Character reached chair.");
                }
            }
        }

        // التفاعل عند الضغط على E بعد الجلوس
        if (hasReachedChair &&
            !dialoguePlayed &&
            Input.GetKeyDown(KeyCode.E))
        {
            PlayDialogueAndSound();
        }
    }

    void PlayDialogueAndSound()
    {
        dialoguePlayed = true;

        if (voiceAudio != null && !voiceAudio.isPlaying)
        {
            voiceAudio.Play();

            Debug.Log("تم الضغط على E وبدأ صوت الحوار!");
        }
    }
}