using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SeparateInterrogationManager : MonoBehaviour
{
    public static SeparateInterrogationManager Instance { get; private set; }

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
        [TextArea(2, 4)] public string detectiveDialogueText = "Please tell us what happened.";
        [TextArea(2, 4)] public string suspectDialogueText = "I have nothing to say!";

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

    [Header("Interrogation Log UI Panel")]
    [SerializeField] private GameObject interrogationLogPanel;
    [SerializeField] private TMP_Text evanLogText;
    [SerializeField] private TMP_Text noraLogText;
    [SerializeField] private Button closeLogBtn;

    [Header("UI Buttons Control")]
    [Tooltip("اسحب هنا زر Btn_OpenTranscripts")]
    [SerializeField] private GameObject btnOpenTranscripts;
    [Tooltip("اسحب هنا زر Btn_Dossier_Mobile")]
    [SerializeField] private GameObject btnDossierMobile;

    // الإحداثيات الدقيقة للكرسي
    private readonly Vector3 chairSeatPosition = new Vector3(2.01f, 0.02f, -7.73f);
    private readonly Quaternion chairSeatRotation = Quaternion.Euler(0f, 146.4f, 0f);

    private int currentSuspectTurn = 1;
    private bool playerSetupComplete = false;
    private bool suspectsStarted = false;
    private bool isInterrogating = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // إخفاء زر أقوال الاستجواب في البداية حتى ينتهي المتهمان
        if (btnOpenTranscripts != null)
        {
            btnOpenTranscripts.SetActive(false);
            Button openBtn = btnOpenTranscripts.GetComponent<Button>();
            if (openBtn != null)
            {
                openBtn.onClick.RemoveAllListeners();
                openBtn.onClick.AddListener(OpenLogPanel);
            }
        }

        if (interrogationLogPanel != null) interrogationLogPanel.SetActive(false);
        if (closeLogBtn != null)
        {
            closeLogBtn.onClick.RemoveAllListeners();
            closeLogBtn.onClick.AddListener(CloseLogPanel);
        }

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

        // التأكد من إخفاء المتهمين تماماً في البداية وعدم إظهارهما
        if (suspect1.suspectObject != null) suspect1.suspectObject.gameObject.SetActive(false);
        if (suspect2.suspectObject != null) suspect2.suspectObject.gameObject.SetActive(false);

        // الاشتراك في حدث اكتمال جمع الـ 5 أدلة
        EvidenceManager.OnAllEvidenceCollected -= OnAllEvidenceGathered;
        EvidenceManager.OnAllEvidenceCollected += OnAllEvidenceGathered;

        // إذا كانت الأدلة مكتملة مسبقاً (مثلاً في حال اختبار)، نبدأ المتهمين
        if (EvidenceManager.Instance != null && EvidenceManager.Instance.IsCaseComplete())
        {
            OnAllEvidenceGathered();
        }
    }

    private void OnDestroy()
    {
        EvidenceManager.OnAllEvidenceCollected -= OnAllEvidenceGathered;
    }

    // تُستدعى حصراً فور جمع الـ 5 أدلة كاملة
    private void OnAllEvidenceGathered()
    {
        Debug.Log("<color=cyan>[INTERROGATION PHASE]</color> All 5 evidences collected! Starting Suspects Sequence.");
        StartCoroutine(StartSuspectsSequence());
    }

    void InitializeSuspect(SuspectData suspect)
    {
        if (suspect.suspectObject != null)
        {
            suspect.agent = suspect.suspectObject.GetComponent<NavMeshAgent>();
            suspect.animator = suspect.suspectObject.GetComponentInChildren<Animator>();
            suspect.suspectCollider = suspect.suspectObject.GetComponent<Collider>();
            suspect.initialPosition = suspect.suspectObject.position;

            if (suspect.suspectObject.GetComponentInChildren<IInteractable>(true) == null)
            {
                var interrogation = suspect.suspectObject.gameObject.AddComponent<SuspectInterrogation>();
                interrogation.suspectName = suspect.suspectName;
                interrogation.suspectPortrait = suspect.suspectPortrait;
                Debug.Log($"[Interrogation] Added missing SuspectInterrogation to {suspect.suspectName}");
            }

            // محاذاة المتهم بدقة مع مستوى الأرضية (Y = 0)
            Vector3 groundedPos = suspect.suspectObject.position;
            groundedPos.y = 0.0f;
            suspect.suspectObject.position = groundedPos;
            suspect.initialPosition = groundedPos;

            if (suspect.suspectCollider == null)
            {
                var capsule = suspect.suspectObject.gameObject.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.height = 1.8f;
                capsule.radius = 0.4f;
                suspect.suspectCollider = capsule;
            }

            if (suspect.animator != null)
            {
                suspect.animator.applyRootMotion = false;
            }

            if (suspect.agent != null)
            {
                suspect.agent.enabled = false;
            }

            // إخفاء المتهم
            suspect.suspectObject.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartSuspectsSequence()
    {
        yield return new WaitForSeconds(1.0f);

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
            Vector3 pos = suspect.suspectObject.position;
            pos.y = 0f;
            suspect.suspectObject.position = pos;

            if (suspect.agent != null)
            {
                suspect.agent.baseOffset = 0f;
            }

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

            Vector3 destination = (target.chairTarget != null) ? target.chairTarget.position : chairSeatPosition;
            destination.y = 0f;

            bool usedNavMesh = false;
            if (target.agent != null)
            {
                if (NavMesh.SamplePosition(target.suspectObject.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    try
                    {
                        target.agent.baseOffset = 0f;
                        target.agent.enabled = true;
                        if (target.agent.isOnNavMesh)
                        {
                            target.agent.isStopped = false;
                            target.agent.SetDestination(destination);
                            usedNavMesh = true;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Interrogation] NavMeshAgent exception: {ex.Message}. Falling back to direct movement.");
                        usedNavMesh = false;
                    }
                }

                if (!usedNavMesh)
                {
                    target.agent.enabled = false;
                }
            }

            if (target.animator != null)
            {
                target.animator.SetBool("isWalking", true);
                target.animator.SetFloat("Speed", 1.5f);
                target.animator.SetFloat("Velocity", 1.5f);
            }

            if (!usedNavMesh)
            {
                StartCoroutine(DirectMoveSuspectToChair(target, destination, (currentSuspectTurn == 1) ? 1 : 2));
            }
        }
    }

    private IEnumerator DirectMoveSuspectToChair(SuspectData suspect, Vector3 destination, int suspectIndex)
    {
        if (suspect.suspectObject == null) yield break;

        float moveSpeed = 1.8f;
        while (suspect.suspectObject != null && !suspect.hasReached)
        {
            Vector3 curPos = suspect.suspectObject.position;
            curPos.y = 0f;
            Vector3 diff = destination - curPos;
            diff.y = 0f;

            if (diff.magnitude <= 0.35f)
            {
                break;
            }

            if (diff.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(diff);
                suspect.suspectObject.rotation = Quaternion.Slerp(suspect.suspectObject.rotation, targetRot, Time.deltaTime * 6f);
            }

            Vector3 nextPos = Vector3.MoveTowards(curPos, destination, moveSpeed * Time.deltaTime);
            nextPos.y = 0f;
            suspect.suspectObject.position = nextPos;

            yield return null;
        }

        CheckSuspectArrival(suspect, suspectIndex);
    }

    void Update()
    {
        if (!playerSetupComplete) return;

        if (currentSuspectTurn == 1)
        {
            if (suspect1.suspectObject != null && suspect1.suspectObject.gameObject.activeInHierarchy && !suspect1.hasReached)
            {
                KeepSuspectGrounded(suspect1);
            }

            if (!suspect1.isWaitingForInteraction && !suspect1.hasReached)
            {
                CheckSuspectArrival(suspect1, 1);
            }
        }
        else if (currentSuspectTurn == 2)
        {
            if (suspect2.suspectObject != null && suspect2.suspectObject.gameObject.activeInHierarchy && !suspect2.hasReached)
            {
                KeepSuspectGrounded(suspect2);
            }

            if (!suspect2.isWaitingForInteraction && !suspect2.hasReached)
            {
                CheckSuspectArrival(suspect2, 2);
            }
        }
    }

    private void KeepSuspectGrounded(SuspectData suspect)
    {
        if (suspect.suspectObject == null) return;

        // تثبيت الارتفاع على 0 تماماً في الوقفة والمشي
        Vector3 pos = suspect.suspectObject.position;
        pos.y = 0.0f;
        suspect.suspectObject.position = pos;

        if (suspect.agent != null && suspect.agent.enabled)
        {
            suspect.agent.baseOffset = 0f;
        }
    }

    void CheckSuspectArrival(SuspectData suspect, int suspectIndex)
    {
        if (Time.time - suspect.actionTime < 0.5f) return;

        bool hasArrived = false;
        
        Vector3 targetChairPos = (suspect.chairTarget != null) ? suspect.chairTarget.position : chairSeatPosition;
        Quaternion targetChairRot = (suspect.chairTarget != null) ? suspect.chairTarget.rotation : chairSeatRotation;

        if (suspect.agent != null && suspect.agent.enabled && suspect.agent.isOnNavMesh && suspect.suspectObject != null)
        {
            float distance = Vector3.Distance(suspect.suspectObject.position, targetChairPos);
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
            float distance = Vector3.Distance(suspect.suspectObject.position, targetChairPos);
            if (distance < 1.0f) hasArrived = true;
        }

        if (Time.time - suspect.actionTime > 6.0f)
        {
            hasArrived = true;
        }

        if (hasArrived)
        {
            suspect.hasReached = true;
            if (suspect.agent != null)
            {
                if (suspect.agent.enabled && suspect.agent.isOnNavMesh) suspect.agent.isStopped = true;
                suspect.agent.enabled = false;
            }

            if (suspect.suspectCollider != null)
            {
                suspect.suspectCollider.enabled = false;
            }

            suspect.suspectObject.position = targetChairPos;
            suspect.suspectObject.rotation = targetChairRot;

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
        Vector3 finalChairPos = (suspect.chairTarget != null) ? suspect.chairTarget.position : chairSeatPosition;
        Quaternion finalChairRot = (suspect.chairTarget != null) ? suspect.chairTarget.rotation : chairSeatRotation;
        suspect.suspectObject.position = finalChairPos;
        suspect.suspectObject.rotation = finalChairRot;

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

        if (suspect.animator != null)
        {
            suspect.animator.ResetTrigger("StartTalking");
            suspect.animator.SetTrigger("StopTalking");
        }

        yield return new WaitForSeconds(0.5f);

        UpdateLogPanelForSuspect(suspect);

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

        MakeSuspectLeave(suspect);
    }

    private void UpdateLogPanelForSuspect(SuspectData suspect)
    {
        if (suspect == suspect1 || (suspect != null && suspect.suspectName.ToLower().Contains("evan")))
        {
            if (evanLogText != null)
            {
                evanLogText.text = $"<b>Question:</b> \"{suspect1.detectiveDialogueText}\"\n\n<b>Evan:</b> \"{suspect1.suspectDialogueText}\"";
            }
        }
        else if (suspect == suspect2 || (suspect != null && suspect.suspectName.ToLower().Contains("nora")))
        {
            if (noraLogText != null)
            {
                noraLogText.text = $"<b>Question:</b> \"{suspect2.detectiveDialogueText}\"\n\n<b>Nora:</b> \"{suspect2.suspectDialogueText}\"";
            }
        }
    }

    public void PlayEvanInterrogation(bool isPlayerMale)
    {
        if (isPlayerMale && suspect1.detectiveAskMale != null) suspect1.detectiveAskMale.Play();
        else if (!isPlayerMale && suspect1.detectiveAskFemale != null) suspect1.detectiveAskFemale.Play();

        if (suspect1.suspectVoiceAudio != null)
        {
            suspect1.suspectVoiceAudio.Play();
        }

        if (evanLogText != null)
        {
            evanLogText.text = $"<b>Question:</b> \"{suspect1.detectiveDialogueText}\"\n\n<b>Evan:</b> \"{suspect1.suspectDialogueText}\"";
        }

        OpenLogPanel();
    }

    public void PlayNoraInterrogation(bool isPlayerMale)
    {
        if (isPlayerMale && suspect2.detectiveAskMale != null) suspect2.detectiveAskMale.Play();
        else if (!isPlayerMale && suspect2.detectiveAskFemale != null) suspect2.detectiveAskFemale.Play();

        if (suspect2.suspectVoiceAudio != null)
        {
            suspect2.suspectVoiceAudio.Play();
        }

        if (noraLogText != null)
        {
            noraLogText.text = $"<b>Question:</b> \"{suspect2.detectiveDialogueText}\"\n\n<b>Nora:</b> \"{suspect2.suspectDialogueText}\"";
        }

        OpenLogPanel();
    }

    public void OpenLogPanel()
    {
        if (interrogationLogPanel != null)
        {
            Canvas rootCanvas = interrogationLogPanel.GetComponentInParent<Canvas>(true);
            if (rootCanvas != null && interrogationLogPanel.transform.parent != rootCanvas.transform)
            {
                interrogationLogPanel.transform.SetParent(rootCanvas.transform, false);
            }

            interrogationLogPanel.SetActive(true);
            interrogationLogPanel.transform.SetAsLastSibling();

            RefreshInterrogationLogUI();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMobileControlsActive(false);
                if (UIManager.Instance.inGameHUD != null) UIManager.Instance.inGameHUD.SetActive(false);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RefreshInterrogationLogUI()
    {
        if (interrogationLogPanel == null) return;

        // 1. إعادة ضبط أبعاد وخلفية لوحة الاستجواب الرئيسية مع تعتيم سينمائي كامل يمنع تسرب خلفية الغرفة
        RectTransform mainRt = interrogationLogPanel.GetComponent<RectTransform>();
        if (mainRt != null)
        {
            mainRt.anchorMin = Vector2.zero;
            mainRt.anchorMax = Vector2.one;
            mainRt.offsetMin = Vector2.zero;
            mainRt.offsetMax = Vector2.zero;
        }

        Image panelBg = interrogationLogPanel.GetComponent<Image>();
        if (panelBg != null)
        {
            panelBg.color = new Color(0.03f, 0.04f, 0.07f, 0.99f); // Solid Onyx Black
        }

        // 2. ربط وضبط زر الإغلاق (X)
        if (closeLogBtn != null)
        {
            closeLogBtn.onClick.RemoveAllListeners();
            closeLogBtn.onClick.AddListener(CloseLogPanel);
            closeLogBtn.transform.SetAsLastSibling();

            RectTransform closeRt = closeLogBtn.GetComponent<RectTransform>();
            if (closeRt != null)
            {
                closeRt.anchorMin = new Vector2(1f, 1f);
                closeRt.anchorMax = new Vector2(1f, 1f);
                closeRt.pivot = new Vector2(1f, 1f);
                closeRt.anchoredPosition = new Vector2(-25f, -25f);
                closeRt.sizeDelta = new Vector2(48f, 48f);
            }
        }

        // 3. تنسيق عنوان الشاشة الرئيسي بدقة عالية وتنسيق جنائي فاخر
        Transform titleTr = interrogationLogPanel.transform.Find("SUSPECT INTERROGATION TRANSCRIPTS");
        if (titleTr != null)
        {
            var titleText = titleTr.GetComponent<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = "<size=115%><color=#00E5FF><b>SUSPECTS DOSSIER</b></color> <color=#455A64>//</color> <color=#FFC107><b>FINAL CONFRONTATION</b></color></size>\n<size=75%><color=#90A4AE>REVIEW INTERROGATION TESTIMONY & SUBMIT FINAL ACCUSATION</color></size>";
                titleText.fontSize = 20f;
                titleText.alignment = TextAlignmentOptions.Center;
            }
            RectTransform titleRt = titleTr.GetComponent<RectTransform>();
            if (titleRt != null)
            {
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -20f);
                titleRt.sizeDelta = new Vector2(0f, 65f);
            }
        }

        // 4. إصلاح تخطيط وتنسيق بطاقات المتهمين داخل CardsContainer
        Transform cardsContainer = interrogationLogPanel.transform.Find("CardsContainer");
        if (cardsContainer != null)
        {
            RectTransform cardsRt = cardsContainer.GetComponent<RectTransform>();
            if (cardsRt != null)
            {
                cardsRt.anchorMin = new Vector2(0.04f, 0.04f);
                cardsRt.anchorMax = new Vector2(0.96f, 0.85f);
                cardsRt.offsetMin = Vector2.zero;
                cardsRt.offsetMax = Vector2.zero;
            }

            var hlg = cardsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childControlHeight = true;
                hlg.childControlWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.padding = new RectOffset(20, 20, 10, 10);
                hlg.spacing = 35;
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }

            SetupSuspectCardUI(cardsContainer.Find("Card_Evan"), suspect1);
            SetupSuspectCardUI(cardsContainer.Find("Card_Nora"), suspect2);

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsRt);
        }
    }

    private void SetupSuspectCardUI(Transform cardTransform, SuspectData suspect)
    {
        if (cardTransform == null || suspect == null) return;

        // مظهر وخلفية كرت المتهم بلون نيلي كحلي أنيق
        Image cardImg = cardTransform.GetComponent<Image>();
        if (cardImg != null)
        {
            cardImg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);
        }

        var vlg = cardTransform.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(22, 22, 16, 16);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
        }

        // 1. صورة المتهم (Portrait)
        Transform portraitTr = cardTransform.Find("Portrait_Evan");
        if (portraitTr != null)
        {
            RectTransform pRt = portraitTr.GetComponent<RectTransform>();
            if (pRt != null)
            {
                pRt.anchoredPosition = Vector2.zero;
                pRt.sizeDelta = new Vector2(0f, 135f);
            }
            var img = portraitTr.GetComponent<Image>();
            if (img != null)
            {
                if (suspect.suspectPortrait != null) img.sprite = suspect.suspectPortrait;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            var le = portraitTr.GetComponent<LayoutElement>();
            if (le == null) le = portraitTr.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 135f;
            le.minHeight = 120f;
            le.flexibleHeight = 0f;
        }

        // 2. اسم المتهم وشارة حالته (Name & Status Badge)
        Transform nameTr = cardTransform.Find("Txt_EvanName");
        if (nameTr != null)
        {
            RectTransform nRt = nameTr.GetComponent<RectTransform>();
            if (nRt != null)
            {
                nRt.anchoredPosition = Vector2.zero;
                nRt.sizeDelta = new Vector2(0f, 44f);
            }
            var txt = nameTr.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = $"<size=115%><color=#ECEFF1><b>{suspect.suspectName.ToUpper()}</b></color></size>\n<size=70%><color=#FFB74D>[PRIMARY SUSPECT // ALIBI CONTESTED]</color></size>";
                txt.fontSize = 17f;
                txt.alignment = TextAlignmentOptions.Center;
            }
            var le = nameTr.GetComponent<LayoutElement>();
            if (le == null) le = nameTr.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight = 40f;
            le.flexibleHeight = 0f;
        }

        // 3. الأقوال ونصوص الاستجواب (Statement) مع حل مشكلة المربعات عبر نصوص واضحة ومقروءة
        Transform statementTr = (suspect == suspect1) ? cardTransform.Find("Txt_EvanStatement") : cardTransform.Find("Txt_NoraStatement");
        if (statementTr == null) statementTr = cardTransform.Find("Txt_EvanStatement");
        if (statementTr != null)
        {
            RectTransform sRt = statementTr.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.anchoredPosition = Vector2.zero;
                sRt.sizeDelta = new Vector2(0f, 160f);
            }
            var txt = statementTr.GetComponent<TMP_Text>();
            if (txt != null)
            {
                // استخدام عناوين إنجليزية جنائية واضحة لتفادي المربعات الناتجة عن نقص خط الحروف العربية في الـ Font Asset
                txt.text = $"<color=#00E5FF><b>INVESTIGATOR:</b></color>\n" +
                           $"<color=#ECEFF1>\"{suspect.detectiveDialogueText}\"</color>\n\n" +
                           $"<color=#FFD54F><b>SUSPECT TESTIMONY:</b></color>\n" +
                           $"<color=#FFFFFF>\"{suspect.suspectDialogueText}\"</color>";
                txt.fontSize = 14.5f;
                txt.lineSpacing = 10f;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.TopLeft;
                txt.textWrappingMode = TextWrappingModes.Normal;
                txt.overflowMode = TextOverflowModes.Ellipsis;
            }
            var le = statementTr.GetComponent<LayoutElement>();
            if (le == null) le = statementTr.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 160f;
            le.minHeight = 140f;
            le.flexibleHeight = 1f;
        }

        // 4. ترتيب وبرمجة الأزرار: زر إعادة تشغيل الصوت + زر الاتهام
        Button[] childButtons = cardTransform.GetComponentsInChildren<Button>(true);
        Button replayBtn = null;
        Button accuseBtn = null;

        if (childButtons.Length > 0) replayBtn = childButtons[0];
        if (childButtons.Length > 1)
        {
            accuseBtn = childButtons[1];
        }
        else if (replayBtn != null)
        {
            // إنشاء زر اتهام مخصص إذا لم يكن موجوداً
            GameObject newAccuseBtnObj = Instantiate(replayBtn.gameObject, cardTransform);
            newAccuseBtnObj.name = "Btn_AccuseSuspect";
            accuseBtn = newAccuseBtnObj.GetComponent<Button>();
        }

        // ضبط وتنسيق زر إعادة تشغيل الصوت (Replay Audio) بدون رموز غريبة تسبب مربعات
        if (replayBtn != null)
        {
            RectTransform rRt = replayBtn.GetComponent<RectTransform>();
            if (rRt != null)
            {
                rRt.anchoredPosition = Vector2.zero;
                rRt.sizeDelta = new Vector2(0f, 40f);
            }
            replayBtn.onClick.RemoveAllListeners();
            replayBtn.onClick.AddListener(() =>
            {
                if (suspect.suspectVoiceAudio != null && suspect.suspectVoiceAudio.clip != null)
                {
                    suspect.suspectVoiceAudio.spatialBlend = 0f;
                    suspect.suspectVoiceAudio.volume = 1f;
                    suspect.suspectVoiceAudio.Play();
                }
            });

            Image repImg = replayBtn.GetComponent<Image>();
            if (repImg != null) repImg.color = new Color(0.12f, 0.28f, 0.46f, 0.95f);

            TMP_Text repTxt = replayBtn.GetComponentInChildren<TMP_Text>();
            if (repTxt != null)
            {
                repTxt.text = "<b>PLAY AUDIO TRANSCRIPT</b>";
                repTxt.fontSize = 13.5f;
                repTxt.color = Color.white;
                repTxt.alignment = TextAlignmentOptions.Center;
            }

            var le = replayBtn.GetComponent<LayoutElement>();
            if (le == null) le = replayBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
            le.flexibleHeight = 0f;
        }

        // ضبط وتنسيق زر توجيه الاتهام النهائي (Accuse Suspect) بلون أحمر قرمزي فخم وواضح
        if (accuseBtn != null)
        {
            RectTransform aRt = accuseBtn.GetComponent<RectTransform>();
            if (aRt != null)
            {
                aRt.anchoredPosition = Vector2.zero;
                aRt.sizeDelta = new Vector2(0f, 46f);
            }
            accuseBtn.onClick.RemoveAllListeners();
            accuseBtn.onClick.AddListener(() => OnAccuseSuspectClicked(suspect.suspectName));

            Image accImg = accuseBtn.GetComponent<Image>();
            if (accImg != null) accImg.color = new Color(0.85f, 0.20f, 0.20f, 1f);

            TMP_Text accTxt = accuseBtn.GetComponentInChildren<TMP_Text>();
            if (accTxt != null)
            {
                accTxt.text = $"<b>ACCUSE {suspect.suspectName.ToUpper()}</b>";
                accTxt.fontSize = 15f;
                accTxt.fontStyle = FontStyles.Bold;
                accTxt.color = Color.white;
                accTxt.alignment = TextAlignmentOptions.Center;
            }

            var le = accuseBtn.GetComponent<LayoutElement>();
            if (le == null) le = accuseBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 46f;
            le.minHeight = 46f;
            le.flexibleHeight = 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(cardTransform.GetComponent<RectTransform>());

        // إخفاء أي أزرار إضافية مكررة إن وجدت
        for (int i = 2; i < childButtons.Length; i++)
        {
            if (childButtons[i] != accuseBtn && childButtons[i] != replayBtn)
            {
                childButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnAccuseSuspectClicked(string accusedName)
    {
        Debug.Log($"<color=orange>[ACCUSATION]</color> Player accused: <b>{accusedName}</b>");
        
        GuiltySelectionBoard guiltyBoard = Object.FindAnyObjectByType<GuiltySelectionBoard>();
        if (guiltyBoard != null)
        {
            guiltyBoard.SelectSuspect(accusedName);
        }
        else
        {
            // فحص الاتهام (Nora Blake في القضية الأولى أو Elena Rostova في القضية الثانية)
            bool isGuilty = accusedName.ToLower().Contains("nora") || accusedName.ToLower().Contains("elena");
            if (isGuilty)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(100);
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOnScreenNotification($"CORRECT ACCUSATION! {accusedName.ToUpper()} APPREHENDED. CASE CLOSED!");
                }

                AudioClip winClip = Resources.Load<AudioClip>("Audio/meldix-success-340660");
                if (winClip != null && Camera.main != null)
                {
                    AudioSource.PlayClipAtPoint(winClip, Camera.main.transform.position);
                }

                StartCoroutine(ShowEndGameDelayed(1.8f));
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOnScreenNotification($"WRONG ACCUSATION! {accusedName.ToUpper()} IS INNOCENT.");
                }

                AudioClip errorClip = Resources.Load<AudioClip>("Audio/u_xg7ssi08yr-error-tone-10-363618");
                if (errorClip != null && Camera.main != null)
                {
                    AudioSource.PlayClipAtPoint(errorClip, Camera.main.transform.position);
                }
            }
        }

        CloseLogPanel();
    }

    private IEnumerator ShowEndGameDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEndGameScreen();
        }
    }

    public void CloseLogPanel()
    {
        if (interrogationLogPanel != null)
        {
            interrogationLogPanel.SetActive(false);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMobileControlsActive(true);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        else if (currentSuspectTurn == 2)
        {
            // عند انتهاء استجواب المتهم الثاني، تظهر أيقونة الأقوال مكان زر الملفات
            ShowTranscriptsButtonAtFilesPosition();
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

    private void ShowTranscriptsButtonAtFilesPosition()
    {
        if (btnOpenTranscripts != null)
        {
            RectTransform transcriptRect = btnOpenTranscripts.GetComponent<RectTransform>();

            // نقل مكان الزر ليطابق مكان زر ملفات القضية أو حقيبة الأدلة في شريط الـ HUD العلوي
            if (btnDossierMobile != null)
            {
                RectTransform dossierRect = btnDossierMobile.GetComponent<RectTransform>();

                if (dossierRect != null && transcriptRect != null)
                {
                    // توحيد الأب ليتبع نفس حاوية الـ UI
                    if (transcriptRect.parent != dossierRect.parent)
                    {
                        transcriptRect.SetParent(dossierRect.parent, false);
                    }

                    transcriptRect.anchorMin = dossierRect.anchorMin;
                    transcriptRect.anchorMax = dossierRect.anchorMax;
                    transcriptRect.pivot = dossierRect.pivot;
                    transcriptRect.anchoredPosition = dossierRect.anchoredPosition;
                    transcriptRect.localScale = dossierRect.localScale;
                    transcriptRect.sizeDelta = dossierRect.sizeDelta;
                }

                // إخفاء زر الملفات واستبداله بزر المتهمين
                btnDossierMobile.SetActive(false);
            }

            Button transBtn = btnOpenTranscripts.GetComponent<Button>();
            if (transBtn != null)
            {
                transBtn.onClick.RemoveAllListeners();
                transBtn.onClick.AddListener(OpenLogPanel);
            }

            // إظهار زر المتهمين
            btnOpenTranscripts.SetActive(true);
        }
    }

    public void StartSuspectMovement(string nameOfSuspect)
    {
        StartInterrogationSequence();
    }
}