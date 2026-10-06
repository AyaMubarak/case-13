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
        if (suspect1.suspectCamera != null) suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null) suspect2.suspectCamera.gameObject.SetActive(false);
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
        
        // إظهار المتهمين الآن
        if (suspect1.suspectObject != null) suspect1.suspectObject.gameObject.SetActive(true);
        if (suspect2.suspectObject != null) suspect2.suspectObject.gameObject.SetActive(true);

        playerSetupComplete = true;

        if (!suspectsStarted)
        {
            suspectsStarted = true;
            // بدء دخول المتهم الأول فقط بعد اكتمال اختيار اللاعب
            StartSuspectTurn(suspect1);
        }
    }

    void StartSuspectTurn(SuspectData suspect)
    {
        if (suspect.agent != null && suspect.chairTarget != null)
        {
            suspect.agent.enabled = true;
            suspect.agent.SetDestination(suspect.chairTarget.position);
        }
    }

    void Update()
    {
        // لا تفعل أي حركة للمتهمين إذا لم ينته اللاعب من الاختيار
        if (!playerSetupComplete)
            return;

        // --- متابعة دور المتهم الأول ---
        if (currentSuspectTurn == 1 && !suspect1.hasReached)
        {
            CheckSuspectArrival(suspect1, 1);
        }
        // --- متابعة دور المتهم الثاني ---
        else if (currentSuspectTurn == 2 && !suspect2.hasReached)
        {
            CheckSuspectArrival(suspect2, 2);
        }
    }

    void CheckSuspectArrival(SuspectData suspect, int suspectIndex)
    {
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
        // إيقاف كل الكاميرات أولاً
        if (suspect1.suspectCamera != null) suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null) suspect2.suspectCamera.gameObject.SetActive(false);

        // تفعيل كاميرا المتهم المطلوب
        SuspectData targetSuspect = (suspectIndex == 1) ? suspect1 : suspect2;
        if (targetSuspect.suspectCamera != null) targetSuspect.suspectCamera.gameObject.SetActive(true);

        // بدء تسلسل الحوار
        StartCoroutine(PlayDialogueSequence(targetSuspect));
    }

    private System.Collections.IEnumerator PlayDialogueSequence(SuspectData suspect)
    {
        // 1. تشغيل صوت المحقق أولاً بناءً على الجنس
        AudioSource detectiveVoice = GetDetectiveVoice();
        if (detectiveVoice != null && detectiveVoice.clip != null)
        {
            detectiveVoice.Play();
            yield return new WaitForSeconds(detectiveVoice.clip.length + 0.5f); // انتظار حتى ينتهي الصوت مع نصف ثانية إضافية
        }

        // 2. تشغيل صوت المتهم بعد الانتهاء
        if (suspect.suspectVoiceAudio != null)
        {
            suspect.suspectVoiceAudio.Play();
        }
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
        currentSuspectTurn = 2;
        StartSuspectTurn(suspect2);
    }

    private void OnDestroy()
    {
        // فك الربط عند الحذف
        EvidenceManager.OnAllEvidenceCollected -= OnAllEvidenceCollected;
    }
}
