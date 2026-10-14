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

            if (target.agent != null)
            {
                target.agent.baseOffset = 0f;
                target.agent.enabled = true;
                target.agent.isStopped = false;
                Vector3 destination = chairSeatPosition;
                destination.y = 0f;
                target.agent.SetDestination(destination);

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
            float distance = Vector3.Distance(suspect.suspectObject.position, chairSeatPosition);
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
                suspect.agent.isStopped = true;
                suspect.agent.enabled = false;
            }

            if (suspect.suspectCollider != null)
            {
                suspect.suspectCollider.enabled = false;
            }

            suspect.suspectObject.position = chairSeatPosition;
            suspect.suspectObject.rotation = chairSeatRotation;

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
        suspect.suspectObject.position = chairSeatPosition;
        suspect.suspectObject.rotation = chairSeatRotation;

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
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RefreshInterrogationLogUI()
    {
        if (interrogationLogPanel == null) return;

        // 1. إعادة ضبط أبعاد لوحة الاستجواب الرئيسية
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
            panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
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
                closeRt.sizeDelta = new Vector2(50f, 50f);
            }
        }

        // 3. تنسيق وإخفاء أي نصوص متكررة أو خارج السياق
        Transform titleTr = interrogationLogPanel.transform.Find("SUSPECT INTERROGATION TRANSCRIPTS");
        if (titleTr != null)
        {
            var titleText = titleTr.GetComponent<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = "<color=#FFC107><b>SUSPECTS DOSSIER</b></color> <color=#546E7A>//</color> <color=#CFD8DC>TRANSCRIPTS & FINAL ACCUSATION</color>";
                titleText.fontSize = 22f;
                titleText.alignment = TextAlignmentOptions.Center;
            }
            RectTransform titleRt = titleTr.GetComponent<RectTransform>();
            if (titleRt != null)
            {
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -25f);
                titleRt.sizeDelta = new Vector2(0f, 50f);
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
                cardsRt.anchorMax = new Vector2(0.96f, 0.88f);
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
                hlg.padding = new RectOffset(20, 20, 15, 15);
                hlg.spacing = 30;
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

        // مظهر وخلفية الكرت
        Image cardImg = cardTransform.GetComponent<Image>();
        if (cardImg != null)
        {
            cardImg.color = new Color(0.10f, 0.13f, 0.19f, 0.98f);
        }

        var vlg = cardTransform.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(20, 20, 18, 18);
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
                pRt.sizeDelta = new Vector2(0f, 150f);
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
            le.preferredHeight = 150f;
            le.minHeight = 130f;
            le.flexibleHeight = 0f;
        }

        // 2. اسم المتهم (Name)
        Transform nameTr = cardTransform.Find("Txt_EvanName");
        if (nameTr != null)
        {
            RectTransform nRt = nameTr.GetComponent<RectTransform>();
            if (nRt != null)
            {
                nRt.anchoredPosition = Vector2.zero;
                nRt.sizeDelta = new Vector2(0f, 32f);
            }
            var txt = nameTr.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = $"<b>{suspect.suspectName}</b>";
                txt.fontSize = 20f;
                txt.color = new Color(1f, 0.85f, 0.25f);
                txt.alignment = TextAlignmentOptions.Center;
            }
            var le = nameTr.GetComponent<LayoutElement>();
            if (le == null) le = nameTr.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;
            le.minHeight = 30f;
            le.flexibleHeight = 0f;
        }

        // 3. الأقوال ونصوص الاستجواب (Statement)
        Transform statementTr = (suspect == suspect1) ? cardTransform.Find("Txt_EvanStatement") : cardTransform.Find("Txt_NoraStatement");
        if (statementTr == null) statementTr = cardTransform.Find("Txt_EvanStatement");
        if (statementTr != null)
        {
            RectTransform sRt = statementTr.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.anchoredPosition = Vector2.zero;
                sRt.sizeDelta = new Vector2(0f, 150f);
            }
            var txt = statementTr.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = $"<color=#FFD54F><b>سؤال المحقق:</b></color>\n\"{suspect.detectiveDialogueText}\"\n\n<color=#80D8FF><b>أقوال المتهم:</b></color>\n\"{suspect.suspectDialogueText}\"";
                txt.fontSize = 13.5f;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.TopLeft;
                txt.textWrappingMode = TextWrappingModes.Normal;
                txt.overflowMode = TextOverflowModes.Ellipsis;
            }
            var le = statementTr.GetComponent<LayoutElement>();
            if (le == null) le = statementTr.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 150f;
            le.minHeight = 120f;
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

        // ضبط وتنسيق زر إعادة تشغيل الصوت (Replay Audio)
        if (replayBtn != null)
        {
            RectTransform rRt = replayBtn.GetComponent<RectTransform>();
            if (rRt != null)
            {
                rRt.anchoredPosition = Vector2.zero;
                rRt.sizeDelta = new Vector2(0f, 38f);
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
            if (repImg != null) repImg.color = new Color(0.18f, 0.35f, 0.55f, 0.95f);

            TMP_Text repTxt = replayBtn.GetComponentInChildren<TMP_Text>();
            if (repTxt != null)
            {
                repTxt.text = "▶ Replay Audio";
                repTxt.fontSize = 14f;
                repTxt.color = Color.white;
                repTxt.alignment = TextAlignmentOptions.Center;
            }

            var le = replayBtn.GetComponent<LayoutElement>();
            if (le == null) le = replayBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;
            le.minHeight = 38f;
            le.flexibleHeight = 0f;
        }

        // ضبط وتنسيق زر توجيه الاتهام النهائي (Accuse Suspect)
        if (accuseBtn != null)
        {
            RectTransform aRt = accuseBtn.GetComponent<RectTransform>();
            if (aRt != null)
            {
                aRt.anchoredPosition = Vector2.zero;
                aRt.sizeDelta = new Vector2(0f, 42f);
            }
            accuseBtn.onClick.RemoveAllListeners();
            accuseBtn.onClick.AddListener(() => OnAccuseSuspectClicked(suspect.suspectName));

            Image accImg = accuseBtn.GetComponent<Image>();
            if (accImg != null) accImg.color = new Color(0.85f, 0.22f, 0.22f, 1f);

            TMP_Text accTxt = accuseBtn.GetComponentInChildren<TMP_Text>();
            if (accTxt != null)
            {
                accTxt.text = $"⚔ ACCUSE {suspect.suspectName.ToUpper()}";
                accTxt.fontSize = 15f;
                accTxt.fontStyle = FontStyles.Bold;
                accTxt.color = Color.white;
                accTxt.alignment = TextAlignmentOptions.Center;
            }

            var le = accuseBtn.GetComponent<LayoutElement>();
            if (le == null) le = accuseBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 42f;
            le.minHeight = 42f;
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
            // فحص الاتهام الافتراضي (Nora Blake هي القاتلة)
            if (accusedName.ToLower().Contains("nora"))
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowOnScreenNotification("CORRECT ACCUSATION! NORA BLAKE APPREHENDED. CASE CLOSED!");
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowOnScreenNotification("WRONG ACCUSATION! EVAN HART IS INNOCENT.");
            }
        }

        CloseLogPanel();
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