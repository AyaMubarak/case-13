using UnityEngine;
using UnityEngine.AI;

public class PoliceSceneManager : MonoBehaviour
{
    [Header("References")]
    public Transform chairTarget;
    public AudioSource voiceAudio;
    public Transform investigator;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionDistance = 2.5f;

    private NavMeshAgent agent;
    private Animator animator;

    private bool interactionStarted;
    private bool hasReachedChair;
    private bool dialogueStarted;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (voiceAudio != null)
        {
            voiceAudio.playOnAwake = false;
            voiceAudio.loop = false;
            voiceAudio.Stop();
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    void Update()
    {
        if (!interactionStarted)
        {
            CheckInteraction();
            return;
        }

        MoveSuspectToChair();
    }

    void CheckInteraction()
    {
        if (investigator == null || chairTarget == null) return;

        float distance = Vector3.Distance(investigator.position, transform.position);

        if (distance <= interactionDistance && Input.GetKeyDown(interactKey))
        {
            interactionStarted = true;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(chairTarget.position);

                // تشغيل أنيميشن المشي
                if (animator != null)
                    animator.SetBool("isWalking", true);
            }
        }
    }

void MoveSuspectToChair()
    {
        if (hasReachedChair || agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending)
            return;

        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            hasReachedChair = true;
            agent.isStopped = true;
            agent.enabled = false; // تعطيل الـ Agent لتثبيت وضعية الجلوس

            // 1. تثبيت الموقع على مقعد الكرسي
            transform.position = chairTarget.position;

            // 2. توجيه وجه الشخصية تلقائياً نحو المحقق بدون ميلان
            if (investigator != null)
            {
                Vector3 lookDirection = investigator.position - transform.position;
                lookDirection.y = 0; // ليبقى الجسم مستقيماً
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }

            // 3. إيقاف المشي وتشغيل الجلوس
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
                animator.SetTrigger("Sit");
            }

            StartDialogue();
        }
    }

    void StartDialogue()
    {
        if (dialogueStarted) return;
        dialogueStarted = true;

        if (voiceAudio != null && !voiceAudio.isPlaying)
            voiceAudio.Play();
    }
}