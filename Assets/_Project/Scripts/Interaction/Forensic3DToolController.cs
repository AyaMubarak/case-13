using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InvestigationGame
{
    public enum ForensicToolType
    {
        ChemicalDropper,      // قطارة كاشف كيميائي (للدم والسموم)
        UvLightTorch,         // كشاف أشعة فوق بنفسجية UV (للأوراق المحروقة والرماد)
        CyberScannerProbe,    // مجس اختراق وفحص إلكتروني مشع (للبطاقات والفلاش ميموري)
        MagneticPowderBrush,  // فرشاة سحب وبصمات مغناطيسية ذهبية (للأسلحة والأدوات الحادة)
        AcousticFrequencyTuner // جهاز تنقية وتصفية ترددات صوتية (للأشرطة والتسجيلات)
    }

    /// <summary>
    /// نظام فحص جنائي ثلاثي الأبعاد تفاعلي خفيف الوزن جداً (0 MB حجم إضافي).
    /// يركب مجسمات 3D حقيقية أنيقة ومتحركة في العالم أمام الدليل أو كاميرا الفحص،
    /// مع إمكانية السحب والتوجيه المباشر باللمس ليقوم اللاعب بفحص الدليل بنفسه.
    /// </summary>
    public class Forensic3DToolController : MonoBehaviour
    {
        public static Forensic3DToolController Instance { get; private set; }

        private GameObject toolRootObject;
        private GameObject animatedTip;
        private Light toolPointLight;
        private ForensicToolType currentToolType;
        private System.Action onAnalysisCompleted;

        public bool IsInteracting => isInteracting;
        private bool isInteracting = false;
        private float interactionProgress = 0f;
        private const float REQUIRED_PROGRESS = 1.0f;

        // UI التوجيه
        private GameObject toolHudCanvas;
        private Image progressSliderBar;
        private TMP_Text hudInstructionText;
        private TMP_Text hudToolNameText;
        private Vector3 initialToolPos;
        private Quaternion initialToolRot;
        private Transform targetEvidenceTransform;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != null && Instance != this) Destroy(this);
        }

        /// <summary>
        /// بدء فحص ثلاثي الأبعاد تفاعلي بالأداة المحددة أمام الدليل
        /// </summary>
        public void Launch3DToolAnalysis(Evidence evidence, ForensicToolType toolType, string toolName, string instruction, System.Action onComplete)
        {
            currentToolType = toolType;
            onAnalysisCompleted = onComplete;
            interactionProgress = 0f;
            isInteracting = false;

            if (evidence != null)
            {
                targetEvidenceTransform = evidence.transform;
            }

            BuildForensic3DTool(toolType);
            CreateInteractiveHud(toolName, instruction);

            // تفعيل الماوس أو اللمس
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (toolRootObject == null || interactionProgress >= REQUIRED_PROGRESS) return;

            // كشف تفاعل اللاعب (سحب أو ضغط بالماوس أو لمس الشاشة)
            bool isPressing = Input.GetMouseButton(0) 
                           || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                           || (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
                           || Input.touchCount > 0;

            if (isPressing)
            {
                isInteracting = true;
                interactionProgress += Time.deltaTime * 0.5f; // شحن الفحص خلال ثانيتين من الضغط/السحب
                interactionProgress = Mathf.Clamp01(interactionProgress);

                // حركة ديناميكية ثلاثية الأبعاد للأداة أثناء عملها
                AnimateToolInAction(interactionProgress);

                if (progressSliderBar != null)
                {
                    progressSliderBar.fillAmount = interactionProgress;
                }

                if (hudInstructionText != null)
                {
                    int pct = Mathf.RoundToInt(interactionProgress * 100f);
                    hudInstructionText.text = $"<color=#00E5FF>ANALYZING IN REAL-TIME 3D...</color>  <b>{pct}%</b>";
                }

                if (interactionProgress >= REQUIRED_PROGRESS)
                {
                    StartCoroutine(FinishAnalysisRoutine());
                }
            }
            else
            {
                isInteracting = false;
                // اهتزاز طفيف للأداة في مكانها بانتظار لمس اللاعب
                if (toolRootObject != null)
                {
                    float idleFloat = Mathf.Sin(Time.time * 3f) * 0.015f;
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(0f, idleFloat, 0f);
                }
            }
        }

        private void AnimateToolInAction(float progress)
        {
            if (toolRootObject == null) return;

            switch (currentToolType)
            {
                case ForensicToolType.ChemicalDropper:
                    // تميل القطارة قليلاً للأمام وتنزل نقطة كيميائية مضيئة
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(Mathf.Sin(Time.time * 8f) * 0.02f, -0.06f, 0.05f);
                    toolRootObject.transform.localRotation = initialToolRot * Quaternion.Euler(20f, 0f, Mathf.Sin(Time.time * 10f) * 5f);
                    if (animatedTip != null)
                    {
                        float dropScale = Mathf.PingPong(Time.time * 4f, 1f);
                        animatedTip.transform.localScale = Vector3.one * (0.02f + dropScale * 0.03f);
                    }
                    break;

                case ForensicToolType.UvLightTorch:
                    // يمسح كشاف UV ذهاباً وإياباً بحزمة الضوء
                    float uvSweep = Mathf.Sin(Time.time * 5f) * 0.15f;
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(uvSweep, -0.02f, 0.04f);
                    toolRootObject.transform.localRotation = initialToolRot * Quaternion.Euler(15f, uvSweep * 30f, 0f);
                    if (toolPointLight != null)
                    {
                        toolPointLight.intensity = 3.5f + Mathf.PingPong(Time.time * 6f, 1.5f);
                    }
                    break;

                case ForensicToolType.CyberScannerProbe:
                    // يتحرك مجس الاختراق التكنولوجي بنبضات ليزرية
                    float cyberMove = Mathf.PingPong(Time.time * 0.8f, 0.12f) - 0.06f;
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(cyberMove, 0f, 0.08f);
                    if (toolPointLight != null)
                    {
                        toolPointLight.intensity = 2f + Mathf.Sin(Time.time * 15f) * 1.5f;
                    }
                    break;

                case ForensicToolType.MagneticPowderBrush:
                    // فرشاة المسحوق تدور وتمسح السطح بنعومة
                    float brushSweepX = Mathf.Sin(Time.time * 7f) * 0.10f;
                    float brushSweepZ = Mathf.Cos(Time.time * 7f) * 0.05f;
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(brushSweepX, -0.04f, brushSweepZ);
                    toolRootObject.transform.localRotation = initialToolRot * Quaternion.Euler(25f, Mathf.Sin(Time.time * 12f) * 20f, 0f);
                    break;

                case ForensicToolType.AcousticFrequencyTuner:
                    // تنقية وتذبذب التردد
                    float tunerJitter = (Mathf.PerlinNoise(Time.time * 10f, 0f) - 0.5f) * 0.03f;
                    toolRootObject.transform.localPosition = initialToolPos + new Vector3(tunerJitter, 0f, 0f);
                    break;
            }
        }

        private IEnumerator FinishAnalysisRoutine()
        {
            if (hudInstructionText != null)
            {
                hudInstructionText.text = "<color=#00E676><b>✔ 3D FORENSIC EXAMINATION COMPLETE!</b></color>";
            }

            // تنبيه موحد في الشريط العلوي، ويعمل في Case 1 وCase 2 وفي Solo وCo-op.
            if (UIManager.Instance != null)
            {
                string clueName = targetEvidenceTransform != null
                    ? targetEvidenceTransform.name.ToUpperInvariant()
                    : "EVIDENCE";
                UIManager.Instance.ShowOnScreenNotification($"FORENSIC ANALYSIS COMPLETE // {clueName}", 3.5f);
            }

            yield return new WaitForSeconds(0.6f);

            CleanupTool();

            onAnalysisCompleted?.Invoke();
        }

        public void CleanupTool()
        {
            if (toolRootObject != null)
            {
                Destroy(toolRootObject);
                toolRootObject = null;
            }

            if (toolHudCanvas != null)
            {
                Destroy(toolHudCanvas);
                toolHudCanvas = null;
            }
        }

        /// <summary>
        /// بناء مجسم 3D احترافي عالي الدقة دون أي مساحة تخزينية (Procedural High-Tech 3D Tools)
        /// مخصص لكل دليل مع خامات ولمعان نيون حديث
        /// </summary>
        private void BuildForensic3DTool(ForensicToolType toolType)
        {
            if (toolRootObject != null) Destroy(toolRootObject);

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            toolRootObject = new GameObject("Forensic_3D_Tool_Root");
            toolRootObject.transform.SetParent(mainCam.transform, false);

            // موقع الأداة في أسفل يمين شاشة الكاميرا ليراها اللاعب بوضوح ثلاثي الأبعاد
            initialToolPos = new Vector3(0.20f, -0.15f, 0.45f);
            initialToolRot = Quaternion.Euler(-10f, -25f, 5f);
            toolRootObject.transform.localPosition = initialToolPos;
            toolRootObject.transform.localRotation = initialToolRot;

            // ضبط طبقة الأداة على Default أو طبقة الكاميرا لضمان رؤيتها
            toolRootObject.layer = 0;

            // مادة لامعة أساسية بتقنية Standard Shader
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            switch (toolType)
            {
                case ForensicToolType.ChemicalDropper:
                    BuildChemicalDropperModel(standardShader);
                    break;

                case ForensicToolType.UvLightTorch:
                    BuildUvTorchModel(standardShader);
                    break;

                case ForensicToolType.CyberScannerProbe:
                    BuildCyberProbeModel(standardShader);
                    break;

                case ForensicToolType.MagneticPowderBrush:
                    BuildMagneticBrushModel(standardShader);
                    break;

                case ForensicToolType.AcousticFrequencyTuner:
                    BuildAcousticTunerModel(standardShader);
                    break;
            }
        }

        private void BuildChemicalDropperModel(Shader shader)
        {
            // 1. الأنبوب الزجاجي (Cylinder)
            GameObject glassBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glassBody.transform.SetParent(toolRootObject.transform, false);
            glassBody.transform.localScale = new Vector3(0.045f, 0.16f, 0.045f);
            glassBody.transform.localPosition = new Vector3(0f, 0f, 0f);
            StripColliders(glassBody);

            Material glassMat = new Material(shader);
            glassMat.color = new Color(0.85f, 0.95f, 1f, 0.6f);
            glassBody.GetComponent<Renderer>().material = glassMat;

            // 2. المحلول الكيميائي الداخلي (سائل ملون)
            GameObject liquidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquidObj.transform.SetParent(glassBody.transform, false);
            liquidObj.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
            liquidObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            StripColliders(liquidObj);

            Material liquidMat = new Material(shader);
            liquidMat.color = new Color(0.95f, 0.15f, 0.25f, 0.9f); // كاشف أحمر فاقع
            liquidObj.GetComponent<Renderer>().material = liquidMat;

            // 3. رأس السحب المطاطي العلوي (Bulb)
            GameObject rubberBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rubberBulb.transform.SetParent(toolRootObject.transform, false);
            rubberBulb.transform.localScale = new Vector3(0.075f, 0.10f, 0.075f);
            rubberBulb.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            StripColliders(rubberBulb);

            Material rubberMat = new Material(shader);
            rubberMat.color = new Color(0.12f, 0.12f, 0.15f);
            rubberBulb.GetComponent<Renderer>().material = rubberMat;

            // 4. رأس التقطير (Tip)
            animatedTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            animatedTip.transform.SetParent(toolRootObject.transform, false);
            animatedTip.transform.localScale = new Vector3(0.035f, 0.045f, 0.035f);
            animatedTip.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            StripColliders(animatedTip);

            animatedTip.GetComponent<Renderer>().material = liquidMat;

            // إضاءة كيميائية خافتة
            toolPointLight = animatedTip.AddComponent<Light>();
            toolPointLight.type = LightType.Point;
            toolPointLight.color = new Color(1f, 0.2f, 0.3f);
            toolPointLight.range = 0.5f;
            toolPointLight.intensity = 1.5f;
        }

        private void BuildUvTorchModel(Shader shader)
        {
            // جسم الكشاف المعدني التكتيكي
            GameObject torchBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchBody.transform.SetParent(toolRootObject.transform, false);
            torchBody.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
            torchBody.transform.localPosition = Vector3.zero;
            StripColliders(torchBody);

            Material metalMat = new Material(shader);
            metalMat.color = new Color(0.15f, 0.17f, 0.20f);
            torchBody.GetComponent<Renderer>().material = metalMat;

            // رأس الكشاف العريض
            GameObject torchHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchHead.transform.SetParent(toolRootObject.transform, false);
            torchHead.transform.localScale = new Vector3(0.085f, 0.04f, 0.085f);
            torchHead.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            StripColliders(torchHead);

            Material headMat = new Material(shader);
            headMat.color = new Color(0.25f, 0.28f, 0.35f);
            torchHead.GetComponent<Renderer>().material = headMat;

            // عدسة الأشعة فوق البنفسجية UV المشعة
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.transform.SetParent(torchHead.transform, false);
            lens.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);
            lens.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            StripColliders(lens);

            Material uvMat = new Material(shader);
            uvMat.color = new Color(0.6f, 0.1f, 1f, 1f);
            lens.GetComponent<Renderer>().material = uvMat;

            // ضوء UV نيون حقيقي ينعكس على البيئة
            toolPointLight = lens.AddComponent<Light>();
            toolPointLight.type = LightType.Spot;
            toolPointLight.spotAngle = 65f;
            toolPointLight.color = new Color(0.7f, 0.2f, 1f);
            toolPointLight.range = 2.5f;
            toolPointLight.intensity = 4.0f;
        }

        private void BuildCyberProbeModel(Shader shader)
        {
            // مقبض المجس التقني
            GameObject probeHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            probeHandle.transform.SetParent(toolRootObject.transform, false);
            probeHandle.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
            StripColliders(probeHandle);

            Material darkCarbon = new Material(shader);
            darkCarbon.color = new Color(0.08f, 0.09f, 0.12f);
            probeHandle.GetComponent<Renderer>().material = darkCarbon;

            // إبرة التوصيل والفحص النيون
            GameObject probeNeedle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            probeNeedle.transform.SetParent(toolRootObject.transform, false);
            probeNeedle.transform.localScale = new Vector3(0.012f, 0.09f, 0.012f);
            probeNeedle.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            StripColliders(probeNeedle);

            Material cyberGold = new Material(shader);
            cyberGold.color = new Color(0f, 0.95f, 1f); // Cyber Cyan
            probeNeedle.GetComponent<Renderer>().material = cyberGold;

            // حلقة بيانات ضوئية
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(toolRootObject.transform, false);
            ring.transform.localScale = new Vector3(0.048f, 0.015f, 0.048f);
            ring.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            StripColliders(ring);
            ring.GetComponent<Renderer>().material = cyberGold;

            toolPointLight = ring.AddComponent<Light>();
            toolPointLight.type = LightType.Point;
            toolPointLight.color = new Color(0f, 0.95f, 1f);
            toolPointLight.range = 1.0f;
            toolPointLight.intensity = 2.5f;
        }

        private void BuildMagneticBrushModel(Shader shader)
        {
            // مقبض الفرشاة التكتيكي
            GameObject wandHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wandHandle.transform.SetParent(toolRootObject.transform, false);
            wandHandle.transform.localScale = new Vector3(0.03f, 0.17f, 0.03f);
            StripColliders(wandHandle);

            Material wandMat = new Material(shader);
            wandMat.color = new Color(0.18f, 0.20f, 0.22f);
            wandHandle.GetComponent<Renderer>().material = wandMat;

            // رأس الفرشاة المغناطيسي اللامع بالبودرة
            GameObject brushHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushHead.transform.SetParent(toolRootObject.transform, false);
            brushHead.transform.localScale = new Vector3(0.065f, 0.09f, 0.065f);
            brushHead.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            StripColliders(brushHead);

            Material goldPowderMat = new Material(shader);
            goldPowderMat.color = new Color(0.95f, 0.75f, 0.25f);
            brushHead.GetComponent<Renderer>().material = goldPowderMat;

            toolPointLight = brushHead.AddComponent<Light>();
            toolPointLight.type = LightType.Point;
            toolPointLight.color = new Color(1f, 0.8f, 0.3f);
            toolPointLight.range = 0.8f;
            toolPointLight.intensity = 2.0f;
        }

        private void BuildAcousticTunerModel(Shader shader)
        {
            // جهاز تحليلي محمول
            GameObject tunerBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunerBody.transform.SetParent(toolRootObject.transform, false);
            tunerBody.transform.localScale = new Vector3(0.11f, 0.20f, 0.03f);
            StripColliders(tunerBody);

            Material bodyMat = new Material(shader);
            bodyMat.color = new Color(0.10f, 0.12f, 0.16f);
            tunerBody.GetComponent<Renderer>().material = bodyMat;

            // شاشة الطيف الترددي الصوتية
            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.transform.SetParent(toolRootObject.transform, false);
            screenObj.transform.localScale = new Vector3(0.09f, 0.12f, 1f);
            screenObj.transform.localPosition = new Vector3(0f, 0.02f, -0.018f);
            StripColliders(screenObj);

            Material screenMat = new Material(shader);
            screenMat.color = new Color(0.95f, 0.25f, 0.65f); // Rose Neon
            screenObj.GetComponent<Renderer>().material = screenMat;
        }

        private void StripColliders(GameObject go)
        {
            var colliders = go.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                Destroy(col);
            }
        }

        /// <summary>
        /// إنشاء شاشة HUD تكتيكية نيون مريحة تفاعلية لإرشاد اللاعب
        /// </summary>
        private void CreateInteractiveHud(string toolName, string instruction)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
                foreach (Canvas candidate in canvases)
                {
                    if (candidate != null && candidate.isActiveAndEnabled && candidate.renderMode != RenderMode.WorldSpace)
                    {
                        canvas = candidate;
                        break;
                    }
                }
            }
            if (canvas == null) return;

            toolHudCanvas = new GameObject("Forensic_3D_Tool_HUD", typeof(RectTransform));
            toolHudCanvas.transform.SetParent(canvas.transform, false);

            RectTransform hudRt = toolHudCanvas.GetComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;
            toolHudCanvas.transform.SetAsLastSibling();

            // لوحة إرشادية علوية أنيقة مطابقة للوبي
            GameObject topPanel = new GameObject("TopCard", typeof(RectTransform), typeof(Image));
            topPanel.transform.SetParent(toolHudCanvas.transform, false);
            RectTransform pRt = topPanel.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 1f);
            pRt.anchorMax = new Vector2(0.5f, 1f);
            pRt.pivot = new Vector2(0.5f, 1f);
            pRt.sizeDelta = new Vector2(650f, 95f);
            pRt.anchoredPosition = new Vector2(0f, -25f);

            Image pImg = topPanel.GetComponent<Image>();
            pImg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);

            // خط النيون العلوي
            GameObject cyanLine = new GameObject("CyanLine", typeof(RectTransform), typeof(Image));
            cyanLine.transform.SetParent(topPanel.transform, false);
            RectTransform lineRt = cyanLine.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 1f);
            lineRt.anchorMax = new Vector2(1f, 1f);
            lineRt.pivot = new Vector2(0.5f, 1f);
            lineRt.sizeDelta = new Vector2(0f, 3.5f);
            cyanLine.GetComponent<Image>().color = new Color(0f, 0.9f, 1f, 1f);

            // اسم الأداة
            GameObject titleObj = new GameObject("ToolName", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(topPanel.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.04f, 0.50f);
            tRt.anchorMax = new Vector2(0.96f, 0.95f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            hudToolNameText = titleObj.GetComponent<TextMeshProUGUI>();
            hudToolNameText.text = $"<color=#00E5FF>EQUIPPED 3D TOOL:</color> <b>{toolName.ToUpperInvariant()}</b>";
            hudToolNameText.fontSize = 20;
            hudToolNameText.alignment = TextAlignmentOptions.Center;

            // تعليمات التفاعل
            GameObject instObj = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
            instObj.transform.SetParent(topPanel.transform, false);
            RectTransform iRt = instObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.04f, 0.05f);
            iRt.anchorMax = new Vector2(0.96f, 0.50f);
            iRt.offsetMin = Vector2.zero;
            iRt.offsetMax = Vector2.zero;

            hudInstructionText = instObj.GetComponent<TextMeshProUGUI>();
            hudInstructionText.text = $"<b>PRESS & DRAG 3D TOOL TO ANALYZE CLUE</b>";
            hudInstructionText.fontSize = 15;
            hudInstructionText.color = new Color(1f, 0.85f, 0.3f);
            hudInstructionText.alignment = TextAlignmentOptions.Center;

            // شريط التقدم السفلي (Progress Bar)
            GameObject barBg = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(toolHudCanvas.transform, false);
            RectTransform bRt = barBg.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.sizeDelta = new Vector2(500f, 18f);
            bRt.anchoredPosition = new Vector2(0f, 35f);

            barBg.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.2f, 0.95f);

            GameObject barFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform fRt = barFill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2f, 2f);
            fRt.offsetMax = new Vector2(-2f, -2f);

            progressSliderBar = barFill.GetComponent<Image>();
            progressSliderBar.type = Image.Type.Filled;
            progressSliderBar.fillMethod = Image.FillMethod.Horizontal;
            progressSliderBar.fillAmount = 0f;
            progressSliderBar.color = new Color(0f, 0.9f, 1f, 1f);
        }
    }
}
