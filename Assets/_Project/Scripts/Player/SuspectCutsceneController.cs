using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SeparateInterrogationManager : MonoBehaviour
{
    [System.Serializable]
    public class SuspectData
    {
        public string suspectName;
        public Transform suspectObject;
        public Transform chairTarget;
        public Camera suspectCamera;
        public Sprite suspectPortrait;

        [Header("Detective Voices for THIS Suspect")]
        public AudioSource detectiveAskMale;
        public AudioSource detectiveAskFemale;

        [Header("Suspect Response")]
        public AudioSource suspectVoiceAudio;

        [Header("Dialogue Text")]
        public string detectiveDialogueText = "Please tell us what happened.";
        public string suspectDialogueText = "I have nothing to say!";

        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Animator animator;
        [HideInInspector] public Collider suspectCollider;
        [HideInInspector] public bool hasReached = false;
        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public bool isLeaving = false;
        [HideInInspector] public bool hasLeft = false;
        [HideInInspector] public bool isWaitingForInteraction = false;
        [HideInInspector] public float actionTime = 0f;
    }

    [Header("المتهم الأول (Evan)")]
    public SuspectData suspect1;

    [Header("المتهم الثاني (Nora)")]
    public SuspectData suspect2;

    // الإحداثيات الدقيقة للكرسي
    private readonly Vector3 chairSeatPosition = new Vector3(2.01f, 0.02f, -7.73f);
    private readonly Quaternion chairSeatRotation = Quaternion.Euler(0f, 146.4f, 0f);

    private int currentSuspectTurn = 1;
    private bool playerSetupComplete = false;
    private bool suspectsStarted = false;
    private bool isInterrogating = false;

    void Awake()
    {
        DisablePlayOnAwake(suspect1);
        DisablePlayOnAwake(suspect2);

        if (suspect1.suspectCamera != null && suspect1.suspectCamera != Camera.main) 
            suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null && suspect2.suspectCamera != Camera.main) 
            suspect2.suspectCamera.gameObject.SetActive(false);

        if (suspect1.suspectObject != null) suspect1.suspectObject.gameObject.SetActive(false);
        if (suspect2.suspectObject != null) suspect2.suspectObject.gameObject.SetActive(false);
    }

    private void DisablePlayOnAwake(SuspectData suspect)
    {
        if (suspect.detectiveAskMale != null) { suspect.detectiveAskMale.playOnAwake = false; suspect.detectiveAskMale.Stop(); }
        if (suspect.detectiveAskFemale != null) { suspect.detectiveAskFemale.playOnAwake = false; suspect.detectiveAskFemale.Stop(); }
        if (suspect.suspectVoiceAudio != null) { suspect.suspectVoiceAudio.playOnAwake = false; suspect.suspectVoiceAudio.Stop(); }
    }

    void Start()
    {
        InitializeSuspect(suspect1);
        InitializeSuspect(suspect2);

        // إظهار المتهمين فوراً بدلاً من انتظار الأدلة
        StartCoroutine(StartSuspectsEarly());
    }

    void InitializeSuspect(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            suspect.agent = suspect.suspectObject.GetComponent<NavMeshAgent>();
            suspect.animator = suspect.suspectObject.GetComponentInChildren<Animator>();
            suspect.suspectCollider = suspect.suspectObject.GetComponent<Collider>();
            suspect.initialPosition = suspect.suspectObject.position;

            if (suspect.animator != null)
            {
                suspect.animator.applyRootMotion = false;
            }

            if (suspect.agent != null)
            {
                suspect.agent.enabled = false;
            }
        }
    }

    private IEnumerator StartSuspectsEarly()
    {
        // تأخير بسيط جداً لضمان تحميل جميع العناصر
        yield return new WaitForSeconds(0.5f);

        playerSetupComplete = true;

        if (!suspectsStarted)
        {
            suspectsStarted = true;
            StartSuspectTurn(suspect1);
        }
    }

    void StartSuspectTurn(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            suspect.suspectObject.gameObject.SetActive(true);
        }
        suspect.isWaitingForInteraction = true;
    }

    public void StartInterrogationSequence()
    {
        if (isInterrogating) return;

        SuspectData target = (currentSuspectTurn == 1) ? suspect1 : suspect2;

        if (target.isWaitingForInteraction)
        {
            isInterrogating = true;
            target.isWaitingForInteraction = false;
            target.actionTime = Time.time;

            if (target.agent != null)
            {
                target.agent.enabled = true;
                target.agent.isStopped = false;
                target.agent.SetDestination(chairSeatPosition);

                if (target.animator != null)
                {
                    target.animator.SetBool("isWalking", true);
                    target.animator.SetFloat("Speed", 1.5f);
                    target.animator.SetFloat("Velocity", 1.5f);
                }
            }
        }
    }

    void Update()
    {
        if (!playerSetupComplete) return;

        if (currentSuspectTurn == 1)
        {
            if (!suspect1.isWaitingForInteraction && !suspect1.hasReached)
                CheckSuspectArrival(suspect1, 1);
        }
        else if (currentSuspectTurn == 2)
        {
            if (!suspect2.isWaitingForInteraction && !suspect2.hasReached)
                CheckSuspectArrival(suspect2, 2);
        }
    }

    void CheckSuspectArrival(SuspectData suspect, int suspectIndex)
    {
        if (Time.time - suspect.actionTime < 0.5f) return;

        bool hasArrived = false;
        
        if (suspect.agent != null && suspect.suspectObject != null)
        {
            float distance = Vector3.Distance(suspect.suspectObject.position, chairSeatPosition);
            if (!suspect.agent.pathPending)
            {
                if ((suspect.agent.hasPath && suspect.agent.remainingDistance <= suspect.agent.stoppingDistance + 0.25f) || distance < 1.0f)
                {
                    hasArrived = true;
                }
            }
        }
        else if (suspect.suspectObject != null)
        {
            // Fallback if agent is missing
            float distance = Vector3.Distance(suspect.suspectObject.position, chairSeatPosition);
            if (distance < 1.0f) hasArrived = true;
        }

        // حماية إضافية: إذا علق المتهم لأكثر من 6 ثوانٍ، اجبره على الوصول للكرسي!
        if (Time.time - suspect.actionTime > 6.0f)
        {
            hasArrived = true;
        }

        if (hasArrived)
        {
            suspect.hasReached = true;
            if (suspect.agent != null)
            {
                suspect.agent.isStopped = true;
                suspect.agent.enabled = false;
            }

            if (suspect.suspectCollider != null)
            {
                suspect.suspectCollider.enabled = false;
            }

                // تثبيت مكان الجلوس والدوران
                suspect.suspectObject.position = chairSeatPosition;
                suspect.suspectObject.rotation = chairSeatRotation;

                // تحويل الحركة إلى جلوس صامت (mixamo.com)
                if (suspect.animator != null)
                {
                    suspect.animator.SetBool("isWalking", false);
                    suspect.animator.SetFloat("Speed", 0f);
                    suspect.animator.SetFloat("Velocity", 0f);
                    suspect.animator.SetTrigger("ReachChair");
                }

                ActivateSuspectCamera(suspectIndex);
                StartCoroutine(PlayDialogueSequence(suspect));
            }
        }

    public void ActivateSuspectCamera(int suspectIndex)
    {
        if (suspect1.suspectCamera != null && suspect1.suspectCamera != Camera.main) 
            suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null && suspect2.suspectCamera != Camera.main) 
            suspect2.suspectCamera.gameObject.SetActive(false);

        SuspectData targetSuspect = (suspectIndex == 1) ? suspect1 : suspect2;
        if (targetSuspect.suspectCamera != null) targetSuspect.suspectCamera.gameObject.SetActive(true);
    }

    private IEnumerator PlayDialogueSequence(SuspectData suspect)
    {
        // تثبيت إضافي للجلوس لضمان عدم حدوث إزاحة من أي كليب
        suspect.suspectObject.position = chairSeatPosition;
        suspect.suspectObject.rotation = chairSeatRotation;

        // 1. المحقق يتكلم (المتهم جالس يستمع فقط بدون كلام)
        AudioSource detectiveVoice = (PlayerProfile.LocalPlayer != null && PlayerProfile.LocalPlayer.gender == DetectiveGender.Female)
            ? suspect.detectiveAskFemale
            : (suspect.detectiveAskMale != null ? suspect.detectiveAskMale : suspect.detectiveAskFemale);

        string detText = !string.IsNullOrEmpty(suspect.detectiveDialogueText) ? suspect.detectiveDialogueText : "Please tell us what happened.";
        float detectiveDuration = (detectiveVoice != null && detectiveVoice.clip != null) ? detectiveVoice.clip.length + 0.5f : 6.0f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"[DETECTIVE]: {detText}", detectiveDuration);
        }

        if (detectiveVoice != null && detectiveVoice.clip != null)
        {
            detectiveVoice.gameObject.SetActive(true);
            detectiveVoice.enabled = true;
            detectiveVoice.spatialBlend = 0f;
            detectiveVoice.volume = 1f;
            detectiveVoice.Play();
            yield return new WaitForSeconds(detectiveVoice.clip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        // 2. انتهاء كلام المحقق -> المتهم يبدأ بالكلام والحركة معاً
        if (suspect.animator != null)
        {
            suspect.animator.ResetTrigger("StopTalking");
            suspect.animator.SetTrigger("StartTalking");
        }

        string susName = !string.IsNullOrEmpty(suspect.suspectName) ? suspect.suspectName.ToUpper() : "SUSPECT";
        string susText = !string.IsNullOrEmpty(suspect.suspectDialogueText) ? suspect.suspectDialogueText : "I have nothing to say!";
        float suspectDuration = (suspect.suspectVoiceAudio != null && suspect.suspectVoiceAudio.clip != null) ? suspect.suspectVoiceAudio.clip.length + 0.5f : 6.0f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"[{susName}]: {susText}", suspectDuration);
        }

        if (suspect.suspectVoiceAudio != null && suspect.suspectVoiceAudio.clip != null)
        {
            suspect.suspectVoiceAudio.gameObject.SetActive(true);
            suspect.suspectVoiceAudio.enabled = true;
            suspect.suspectVoiceAudio.spatialBlend = 0f;
            suspect.suspectVoiceAudio.volume = 1f;
            suspect.suspectVoiceAudio.Play();
            yield return new WaitForSeconds(suspect.suspectVoiceAudio.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(3.5f);
        }

        // 3. انتهاء رد المتهم -> العودة للجلوس الصامت مباشرة
        if (suspect.animator != null)
        {
            suspect.animator.ResetTrigger("StartTalking");
            suspect.animator.SetTrigger("StopTalking");
        }

        yield return new WaitForSeconds(0.5f);

        // 4. حفظ الإفادة والتنبيه
        if (InterrogationUI.Instance != null)
        {
            InterrogationUI.Instance.SetInterrogationRecord(suspect.suspectName, suspect.suspectDialogueText, suspect.suspectPortrait);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnlockDossierAccess();
            
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld || UIManager.Instance.forceMobileUIInEditor)
            {
                UIManager.Instance.ShowOnScreenNotification("STATEMENT RECORDED! TAP [FILES] TO REVIEW CASE");
            }
            else
            {
                UIManager.Instance.ShowOnScreenNotification("STATEMENT RECORDED! PRESS [B] TO REVIEW CASE FILES");
            }
        }

        yield return new WaitForSeconds(3.0f);

        // الانتقال للمتهم التالي
        MakeSuspectLeave(suspect);
    }

    private void MakeSuspectLeave(SuspectData currentSuspect)
    {
        if (currentSuspect.suspectObject != null)
        {
            currentSuspect.suspectObject.gameObject.SetActive(false);
        }

        if (currentSuspectTurn == 1)
        {
            NextSuspect();
        }
    }

    public void NextSuspect()
    {
        isInterrogating = false;

        if (suspect1.suspectObject != null)
        {
            suspect1.suspectObject.gameObject.SetActive(false);
        }

        currentSuspectTurn = 2;
        StartSuspectTurn(suspect2);
    }

    // تم إزالة OnDestroy لأنه لم يعد هناك اشتراك في Events
    public void StartSuspectMovement(string nameOfSuspect)
    {
        StartInterrogationSequence();
    }
}