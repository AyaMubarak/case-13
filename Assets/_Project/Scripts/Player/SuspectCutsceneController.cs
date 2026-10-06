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

        // إغلاق جميع الكاميرات في البداية
        if (suspect1.suspectCamera != null) suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null) suspect2.suspectCamera.gameObject.SetActive(false);

        // الاستماع لحدث انتهاء اختيار اللاعب
        // ابحث عن GameStateManager أو استخدم onGameStart callback
        if (RoleSelectionUI.OnPlayerSetupComplete != null)
        {
            RoleSelectionUI.OnPlayerSetupComplete += OnPlayerSetupCompleted;
        }

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

    // يتم استدعاء هذه الدالة عندما ينتهي اللاعب من اختيار الدور والجنس والاسم
    public void OnPlayerSetupCompleted()
    {
        Debug.Log("[INTERROGATION] Player setup complete - Starting suspect animations");
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

        // تفعيل كاميرا المتهم المطلوب وتشكيل صوته
        if (suspectIndex == 1)
        {
            if (suspect1.suspectCamera != null) suspect1.suspectCamera.gameObject.SetActive(true);
            if (suspect1.suspectVoiceAudio != null) suspect1.suspectVoiceAudio.Play(); // تشغيل فويس المتهم الأول
        }
        else if (suspectIndex == 2)
        {
            if (suspect2.suspectCamera != null) suspect2.suspectCamera.gameObject.SetActive(true);
            if (suspect2.suspectVoiceAudio != null) suspect2.suspectVoiceAudio.Play(); // تشغيل فويس المتهم الثاني
        }
    }

    // دالة لتشغيل صوت المحقق (حسب الجنس: 1 للذكر، 2 للأنثى)
    public void PlayDetectiveVoice(int genderType)
    {
        if (genderType == 1 && detectiveAudioMale != null)
        {
            detectiveAudioMale.Play();
        }
        else if (genderType == 2 && detectiveAudioFemale != null)
        {
            detectiveAudioFemale.Play();
        }
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
        if (RoleSelectionUI.OnPlayerSetupComplete != null)
        {
            RoleSelectionUI.OnPlayerSetupComplete -= OnPlayerSetupCompleted;
        }
    }
}
