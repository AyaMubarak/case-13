using UnityEngine;
using UnityEngine.AI;

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

    void Start()
    {
        InitializeSuspect(suspect1);
        InitializeSuspect(suspect2);

        // إغلاق جميع الكاميرات في البداية
        if (suspect1.suspectCamera != null) suspect1.suspectCamera.gameObject.SetActive(false);
        if (suspect2.suspectCamera != null) suspect2.suspectCamera.gameObject.SetActive(false);

        // بدء دخول المتهم الأول فقط في البداية
        StartSuspectTurn(suspect1);
    }

    void InitializeSuspect(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            suspect.agent = suspect.suspectObject.GetComponent<NavMeshAgent>();
            suspect.animator = suspect.suspectObject.GetComponent<Animator>();
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
}