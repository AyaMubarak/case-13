using UnityEngine;
using UnityEngine.AI;
using System;

public class SeparateInterrogationManager : MonoBehaviour
{
    [System.Serializable]
    public class SuspectData
    {
        public string suspectName;
        public Transform suspectObject;
        public Transform chairTarget;
        public Camera suspectCamera;
        public AudioSource suspectVoiceAudio; // الصوت الخاص بالمتهم
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Animator animator;
        [HideInInspector] public bool hasReached = false;
        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public bool isLeaving = false;
        [HideInInspector] public bool hasLeft = false;
        [HideInInspector] public bool isWaitingForInteraction = false;
        [HideInInspector] public float actionTime = 0f;
    }

    [Header("المتهم الأول (مثلاً Evan)")]
    public SuspectData suspect1;

    [Header("المتهم الثاني (مثلاً Nora)")]
    public SuspectData suspect2;

    [Header("أصوات المحقق (حسب الجنس)")]
    public AudioSource detectiveAudioMale;   // صوت المحقق (ذكر)
    public AudioSource detectiveAudioFemale; // صوت المحقق (أنثى)

    private int currentSuspectTurn = 1; // 1 = دور المتهم الأول, 2 = دور المتهم الثاني
    private bool playerSetupComplete = false; // يتم تعيينها بعد انتهاء اختيار اللاعب
    private bool suspectsStarted = false; // تتبع ما إذا كنا قد بدأنا الحركة

    void Start()
    {
        InitializeSuspect(suspect1);
        InitializeSuspect(suspect2);

        // إغلاق جميع الكاميرات والمتهمين في البداية حتى يتم جمع الأدلة
        if (suspect1.suspectCamera != null && suspect1.suspectCamera != Camera.main) 
            suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null && suspect2.suspectCamera != Camera.main) 
            suspect2.suspectCamera.gameObject.SetActive(false);
        if (suspect1.suspectObject != null) suspect1.suspectObject.gameObject.SetActive(false);
        if (suspect2.suspectObject != null) suspect2.suspectObject.gameObject.SetActive(false);

        // الاستماع لحدث جمع كل الأدلة (لا يدخلون إلا بعد جمعها)
        EvidenceManager.OnAllEvidenceCollected += OnAllEvidenceCollected;

        // إذا كنت تستخدم event مختلف، قم بتعديل هذا السطر
        // مثلاً: PlayerProfile.LocalPlayer.OnProfileChanged += OnPlayerSetupCompleted;
    }

    void InitializeSuspect(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            suspect.agent = suspect.suspectObject.GetComponent<NavMeshAgent>();
            suspect.animator = suspect.suspectObject.GetComponent<Animator>();

            // حفظ مكان الباب أو المكان الابتدائي ليعود إليه لاحقاً
            suspect.initialPosition = suspect.suspectObject.position;

            // التأكد من أن المتهم مغلق وغير متحرك في البداية
            if (suspect.agent != null)
            {
                suspect.agent.enabled = false;
            }
        }
    }

    // يتم استدعاء هذه الدالة عندما يجمع اللاعب كل الأدلة
    public void OnAllEvidenceCollected()
    {
        Debug.Log("[INTERROGATION] All evidence collected - Suspects are arriving!");
        
        playerSetupComplete = true;

        if (!suspectsStarted)
        {
            suspectsStarted = true;
            // بدء دخول المتهم الأول فقط
            StartSuspectTurn(suspect1);
        }
    }

    void StartSuspectTurn(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            // إظهار هذا المتهم فقط
            suspect.suspectObject.gameObject.SetActive(true);
        }

        // الانتظار حتى يتفاعل المحقق
        suspect.isWaitingForInteraction = true;
    }

    public void StartSuspectMovement(string nameOfSuspect)
    {
        if (currentSuspectTurn == 1 && suspect1.suspectName == nameOfSuspect && suspect1.isWaitingForInteraction)
        {
            suspect1.isWaitingForInteraction = false;
            suspect1.actionTime = Time.time;
            if (suspect1.agent != null && suspect1.chairTarget != null)
            {
                suspect1.agent.enabled = true;
                suspect1.agent.SetDestination(suspect1.chairTarget.position);
            }
        }
        else if (currentSuspectTurn == 2 && suspect2.suspectName == nameOfSuspect && suspect2.isWaitingForInteraction)
        {
            suspect2.isWaitingForInteraction = false;
            suspect2.actionTime = Time.time;
            if (suspect2.agent != null && suspect2.chairTarget != null)
            {
                suspect2.agent.enabled = true;
                suspect2.agent.SetDestination(suspect2.chairTarget.position);
            }
        }
    }

    void Update()
    {
        // لا تفعل أي حركة للمتهمين إذا لم ينته اللاعب من الاختيار
        if (!playerSetupComplete)
            return;

        // --- متابعة دور المتهم الأول ---
        if (currentSuspectTurn == 1)
        {
            if (!suspect1.isWaitingForInteraction && !suspect1.hasReached)
                CheckSuspectArrival(suspect1, 1);
            else if (suspect1.isLeaving && !suspect1.hasLeft)
                CheckSuspectLeaving(suspect1, 1);
        }
        // --- متابعة دور المتهم الثاني ---
        else if (currentSuspectTurn == 2)
        {
            if (!suspect2.isWaitingForInteraction && !suspect2.hasReached)
                CheckSuspectArrival(suspect2, 2);
            else if (suspect2.isLeaving && !suspect2.hasLeft)
                CheckSuspectLeaving(suspect2, 2);
        }
    }

    void CheckSuspectLeaving(SuspectData suspect, int suspectIndex)
    {
        if (Time.time - suspect.actionTime < 0.5f) return; // انتظار قليل لتجنب الوصول الفوري

        if (suspect.agent != null && !suspect.agent.pathPending)
        {
            if (suspect.agent.remainingDistance <= suspect.agent.stoppingDistance)
            {
                suspect.hasLeft = true;
                suspect.agent.isStopped = true;
                suspect.agent.enabled = false;
                
                if (suspect.animator != null)
                {
                    suspect.animator.SetTrigger("Idle"); // أو يمكنك استخدام متغير آخر لوقوفه بثبات
                }

                if (suspect.suspectObject != null)
                {
                    suspect.suspectObject.gameObject.SetActive(false); // يخفي نفسه بعد أن يصل للباب
                }

                // تلقائياً ينتقل للمتهم الثاني إذا كان هذا المتهم الأول
                if (suspectIndex == 1)
                {
                    NextSuspect();
                }
            }
        }
    }

    void CheckSuspectArrival(SuspectData suspect, int suspectIndex)
    {
        if (Time.time - suspect.actionTime < 0.5f) return; // انتظار قليل لتجنب الوصول الفوري

        if (suspect.agent != null && !suspect.agent.pathPending)
        {
            if (suspect.agent.remainingDistance <= suspect.agent.stoppingDistance)
            {
                suspect.hasReached = true;
                suspect.agent.isStopped = true;
                suspect.agent.enabled = false; // استقرار بالجلوس

                if (suspect.animator != null)
                {
                    suspect.animator.SetTrigger("ReachChair");
                }

                // تفعيل الكاميرا الخاصة بهذا المتهم وحده وتشكيل مشهده
                ActivateSuspectCamera(suspectIndex);
            }
        }
    }

    public void ActivateSuspectCamera(int suspectIndex)
    {
        // إيقاف كل الكاميرات أولاً (ما عدا الكاميرا الأساسية)
        if (suspect1.suspectCamera != null && suspect1.suspectCamera != Camera.main) 
            suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null && suspect2.suspectCamera != Camera.main) 
            suspect2.suspectCamera.gameObject.SetActive(false);

        // تفعيل كاميرا المتهم المطلوب
        SuspectData targetSuspect = (suspectIndex == 1) ? suspect1 : suspect2;
        if (targetSuspect.suspectCamera != null) targetSuspect.suspectCamera.gameObject.SetActive(true);

        // بدء تسلسل الحوار
        StartCoroutine(PlayDialogueSequence(targetSuspect, suspectIndex));
    }

    private System.Collections.IEnumerator PlayDialogueSequence(SuspectData suspect, int suspectIndex)
    {
        // 1. تشغيل صوت المحقق أولاً بناءً على الجنس
        AudioSource detectiveVoice = GetDetectiveVoice();
        if (detectiveVoice != null && detectiveVoice.clip != null)
        {
            detectiveVoice.Play();
            yield return new WaitForSeconds(detectiveVoice.clip.length + 0.5f); // انتظار حتى ينتهي الصوت مع نصف ثانية إضافية
        }

        // 2. تشغيل صوت المتهم بعد الانتهاء
        if (suspect.suspectVoiceAudio != null && suspect.suspectVoiceAudio.clip != null)
        {
            suspect.suspectVoiceAudio.Play();
            yield return new WaitForSeconds(suspect.suspectVoiceAudio.clip.length + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(3f); // انتظار افتراضي لو مفيش صوت
        }

        // 3. المشي للباب
        MakeSuspectLeave(suspect);
    }

    private void MakeSuspectLeave(SuspectData suspect)
    {
        if (suspect.animator != null)
        {
            suspect.animator.SetTrigger("StandUp"); // تأكد أن لديك Trigger بهذا الاسم في الـ Animator
        }

        suspect.agent.enabled = true;
        suspect.agent.isStopped = false;
        suspect.agent.SetDestination(suspect.initialPosition);
        suspect.isLeaving = true;
        suspect.actionTime = Time.time;
    }

    private AudioSource GetDetectiveVoice()
    {
        // جلب صوت المحقق المناسب حسب الجنس المختار
        if (PlayerProfile.LocalPlayer != null)
        {
            if (PlayerProfile.LocalPlayer.gender == DetectiveGender.Male) return detectiveAudioMale;
            else return detectiveAudioFemale;
        }
        return detectiveAudioMale; // افتراضي
    }

    // دالة للانتقال اليدوي للدور التالي (مثلاً عند الضغط على زر أو انتهاء الحوار)
    public void NextSuspect()
    {
        // إخفاء المتهم الأول عند بدء دور المتهم الثاني
        if (suspect1.suspectObject != null)
        {
            suspect1.suspectObject.gameObject.SetActive(false);
        }

        currentSuspectTurn = 2;
        StartSuspectTurn(suspect2);
    }

    private void OnDestroy()
    {
        // فك الربط عند الحذف
        EvidenceManager.OnAllEvidenceCollected -= OnAllEvidenceCollected;
    }
}
