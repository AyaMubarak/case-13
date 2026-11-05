using UnityEngine;

public class CharacterVisualSelector : MonoBehaviour
{
    [Header("مرجع ملف اللاعب")]
    [SerializeField] private PlayerProfile profile;

    [Header("المجسمات النسائية (Female Models)")]
    [SerializeField] private GameObject femaleDetective;
    [SerializeField] private GameObject femaleDoctor;
    [SerializeField] private GameObject femaleTech;
    [SerializeField] private GameObject femaleInterrogator;

    [Header("المجسمات الرجالية (Male Models)")]
    [SerializeField] private GameObject maleDetective;
    [SerializeField] private GameObject maleDoctor;
    [SerializeField] private GameObject maleTech;
    [SerializeField] private GameObject maleInterrogator;

    [Header("متحكم الحركة المشترك (Shared Controller)")]
    [SerializeField] private RuntimeAnimatorController sharedAnimatorController;

    private static RuntimeAnimatorController cachedController;
    private static Material sharedCharacterMaterial;
    private GameObject currentActiveModel;

    public GameObject CurrentActiveModel => currentActiveModel;

    private void Awake()
    {
        if (profile == null)
            profile = GetComponent<PlayerProfile>();

        ResolveSharedController();
        ResolveModelReferences();
        EnsureAllModelsConfigured();
    }

    /// <summary>
    /// حل تلقائي لجميع مجسمات الشخصيات الـ 8 تحت Visuals إذا لم تكن مسندة يدوياً
    /// يدعم كلاً من التسميات القديمة والجديدة
    /// </summary>
    public void ResolveModelReferences()
    {
        Transform visualsRoot = transform.Find("Visuals");
        Transform searchRoot = visualsRoot != null ? visualsRoot : transform;

        if (femaleDetective == null)
            femaleDetective = FindModel(searchRoot, "Female_Detective_New", "Model_Female_Detective", "Female_Detective");
        if (femaleDoctor == null)
            femaleDoctor = FindModel(searchRoot, "Model_Female_Doctor", "Female_Doctor_New", "Female_Doctor");
        if (femaleTech == null)
            femaleTech = FindModel(searchRoot, "Model_Female_Tech", "Female_Tech_New", "Female_Tech");
        if (femaleInterrogator == null)
            femaleInterrogator = FindModel(searchRoot, "Model_Female_Interrogator", "Model_Female_Integroter", "Female_Interrogator");

        if (maleDetective == null)
            maleDetective = FindModel(searchRoot, "Detective_Male_New", "Model_Male_Detective", "Male_Detective_New", "Male_Detective");
        if (maleDoctor == null)
            maleDoctor = FindModel(searchRoot, "Male_Doctor_New", "Model_Male_Doctor", "Male_Doctor");
        if (maleTech == null)
            maleTech = FindModel(searchRoot, "Model_Male_Tech", "Male_Tech_New", "Male_Tech");
        if (maleInterrogator == null)
            maleInterrogator = FindModel(searchRoot, "Model_Male_Interrogator", "Model_Male_Integroter", "Male_Interrogator");
    }

    private GameObject FindModel(Transform root, params string[] possibleNames)
    {
        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        foreach (string targetName in possibleNames)
        {
            foreach (Transform t in allChildren)
            {
                if (t == root) continue;
                if (string.Equals(t.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return t.gameObject;
                }
            }
        }
        return null;
    }

    private void ResolveSharedController()
    {
        if (sharedAnimatorController == null)
        {
            if (cachedController == null)
            {
                cachedController = Resources.Load<RuntimeAnimatorController>("Characters/Animation/CharacterAnimation")
                                  ?? Resources.Load<RuntimeAnimatorController>("CharacterAnimation");
#if UNITY_EDITOR
                if (cachedController == null)
                {
                    cachedController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/_Project/Prefabs/Characters/Animation/CharacterAnimation.controller");
                }
#endif
            }
            sharedAnimatorController = cachedController;
        }
    }

    private void EnsureAllModelsConfigured()
    {
        GameObject[] models = new GameObject[]
        {
            femaleDetective, femaleDoctor, femaleTech, femaleInterrogator,
            maleDetective, maleDoctor, maleTech, maleInterrogator
        };

        foreach (GameObject model in models)
        {
            if (model == null) continue;
            EnsureModelAnimator(model);
            EnsureModelMaterials(model);
        }
    }

    private void EnsureModelAnimator(GameObject model)
    {
        if (model == null) return;

        // The imported character prefabs do not all share the same root
        // orientation. Some of them arrive rotated -90 degrees on X, which
        // makes the character lie on the floor in the gameplay scene.
        NormalizeModelTransform(model);

        // Do not force the shared humanoid controller onto imported models.
        // These avatars do not all use the same rig, and forcing that
        // controller deforms some of them into a sitting/cut-off shape.
        Animator anim = model.GetComponent<Animator>();
        if (anim != null)
        {
            if (anim.runtimeAnimatorController == null)
            {
                anim.enabled = false;
                return;
            }
            anim.applyRootMotion = false;
            // Preserve the controller embedded/assigned by the character
            // asset. Only disable root motion; each rig has its own skin and
            // animation mapping.
            anim.enabled = true;
        }
    }

    private void NormalizeModelTransform(GameObject model)
    {
        Transform modelTransform = model.transform;
        Vector3 savedScale = modelTransform.localScale;

        // All character meshes use the Player's Y axis as their up axis.
        modelTransform.localRotation = Quaternion.identity;
        modelTransform.localScale = savedScale;

        // Different source assets have different root offsets. Align the
        // lowest renderer point with the Visuals root so feet do not sink into
        // the floor (or float above it).
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float desiredBottom = modelTransform.parent != null ? modelTransform.parent.position.y : 0f;
        // Keep every imported avatar at a consistent gameplay height. The
        // source files use very different units (some are centimetres), which
        // is why two characters previously appeared as tiny pieces on the
        // floor and the others were missing from the camera view.
        float height = bounds.size.y;
        if (height > 0.001f)
        {
            float factor = 1.8f / height;
            modelTransform.localScale *= factor;
        }

        // Recalculate after scaling, then plant the feet on the Visuals root.
        renderers = model.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        desiredBottom = modelTransform.parent != null ? modelTransform.parent.position.y : 0f;
        float bottomOffset = desiredBottom - bounds.min.y;
        modelTransform.localPosition = new Vector3(modelTransform.localPosition.x,
            modelTransform.localPosition.y + bottomOffset, modelTransform.localPosition.z);
    }

    /// <summary>
    /// التأكد من أن جميع Renderers بالمجسم لديها Material و Shader صالحين
    /// لمنع ظهور الشيدر الوردي (Pink) أو المجسم الشفاف/الأبيض
    /// </summary>
    private void EnsureModelMaterials(GameObject model)
    {
        if (model == null) return;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.sharedMaterials;
            bool matsUpdated = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                bool invalidShader = m == null || m.shader == null ||
                    m.shader.name.Contains("InternalErrorShader") || m.shader.name.Contains("Pink");
                // Keep valid imported materials, even when they use the
                // Standard/glTF shader. Replacing them loses their original
                // skin, hair and clothing textures and makes the characters
                // look flat or almost black.
                if (invalidShader)
                {
                    Material replacement = CreateFallbackCharacterMaterial(GetFallbackColor(rend, model));
                    if (replacement != null)
                    {
                        // Preserve the texture/color from the original material
                        // when converting legacy/unsupported shaders to URP.
                        if (m != null)
                        {
                            Texture texture = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") :
                                (m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null);
                            if (texture != null)
                            {
                                if (replacement.HasProperty("_BaseMap")) replacement.SetTexture("_BaseMap", texture);
                                if (replacement.HasProperty("_MainTex")) replacement.SetTexture("_MainTex", texture);
                            }
                            Color color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") :
                                (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                            if (replacement.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", color);
                            if (replacement.HasProperty("_Color")) replacement.SetColor("_Color", color);
                        }
                        mats[i] = replacement;
                        matsUpdated = true;
                    }
                }
            }

            if (matsUpdated)
            {
                rend.sharedMaterials = mats;
            }
        }
    }

    private bool HasMaterialTexture(Material material)
    {
        return (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) ||
               (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null);
    }

    private Color GetMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    private bool IsNearlyWhite(Color color)
    {
        return color.r > 0.88f && color.g > 0.88f && color.b > 0.88f;
    }

    private Color GetFallbackColor(Renderer renderer, GameObject model)
    {
        string n = (renderer.name + " " + model.name).ToLowerInvariant();
        if (n.Contains("skin") || n.Contains("body") || n.Contains("face") || n.Contains("head"))
            return new Color(0.72f, 0.42f, 0.28f, 1f);
        if (n.Contains("hair") || n.Contains("shoe") || n.Contains("boot"))
            return new Color(0.06f, 0.08f, 0.1f, 1f);
        if (n.Contains("doctor") || n.Contains("female"))
            return new Color(0.25f, 0.55f, 0.8f, 1f);
        if (n.Contains("tech"))
            return new Color(0.2f, 0.75f, 0.55f, 1f);
        return new Color(0.55f, 0.18f, 0.12f, 1f);
    }

    private Material CreateFallbackCharacterMaterial(Color fallbackColor)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") 
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Standard");

        if (urpLit == null) return null;

        Material mat = new Material(urpLit);
        mat.name = "M_Character_Fallback";

        Texture2D baseTex = Resources.Load<Texture2D>("tripo_mat_d0bcade7_baseColor");
#if UNITY_EDITOR
        if (baseTex == null)
        {
            baseTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/tripo_mat_d0bcade7_baseColor.jpg");
        }
#endif
        if (baseTex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
        }
        else
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallbackColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallbackColor);
        }

        return mat;
    }

    private void OnEnable()
    {
        if (profile == null)
            profile = GetComponent<PlayerProfile>();

        if (profile != null)
            profile.OnProfileChanged += ApplySelectedModel;
    }

    private void OnDisable()
    {
        if (profile != null)
            profile.OnProfileChanged -= ApplySelectedModel;
    }

    private void Start()
    {
        ResolveSharedController();
        ResolveModelReferences();
        EnsureAllModelsConfigured();
        ApplySelectedModel();
    }

    public void ApplySelectedModel()
    {
        if (profile == null) return;

        ResolveModelReferences();
        HideAllModels();

        // Player bodies are intentionally hidden. Names and roles are shown
        // by RemotePlayerReplica nameplates instead.
    }

    public void SetCutsceneVisibility(bool visible)
    {
        if (visible)
        {
            ApplySelectedModel();
        }
        else
        {
            HideAllModels();
        }
    }

    public void HideAllModels()
    {
        // 1. إخفاء المراجع الـ 8 المحددة
        if (femaleDetective != null) femaleDetective.SetActive(false);
        if (femaleDoctor != null) femaleDoctor.SetActive(false);
        if (femaleTech != null) femaleTech.SetActive(false);
        if (femaleInterrogator != null) femaleInterrogator.SetActive(false);

        if (maleDetective != null) maleDetective.SetActive(false);
        if (maleDoctor != null) maleDoctor.SetActive(false);
        if (maleTech != null) maleTech.SetActive(false);
        if (maleInterrogator != null) maleInterrogator.SetActive(false);

        // 2. إخفاء أي مجسم آخر تحت Visuals أو تحت الكائن لمنع بقاء أي شخصية غير مختارة ظاهرة
        Transform visualsRoot = transform.Find("Visuals");
        if (visualsRoot != null)
        {
            for (int i = 0; i < visualsRoot.childCount; i++)
            {
                visualsRoot.GetChild(i).gameObject.SetActive(false);
            }
        }

        currentActiveModel = null;
    }

    private void Activate(GameObject model)
    {
        // إذا كان الموديل المطلوب غير موجود، نستخدم المحقق كبديل افتراضي
        GameObject targetModel = model;
        if (targetModel == null)
        {
            bool isFemale = profile != null && profile.gender.ToString().ToLower().Contains("female");
            targetModel = isFemale ? (femaleDetective ?? maleDetective) : (maleDetective ?? femaleDetective);
        }

        if (targetModel != null)
        {
            EnsureModelAnimator(targetModel);
            EnsureModelMaterials(targetModel);
            targetModel.SetActive(true);
            currentActiveModel = targetModel;

            // Ensure the complete model hierarchy is active. Imported FBX
            // files often contain inactive Armature/Mesh children.
            Transform node = targetModel.transform;
            while (node != null && node != transform)
            {
                node.gameObject.SetActive(true);
                node = node.parent;
            }

            foreach (Renderer renderer in targetModel.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // إذا كان هذا هو اللاعب المحلي، إخفاء الـ Meshes من الكاميرا الأولى ولكن مع إبقاء الظلال
        var networkPlayer = GetComponent<NetworkPlayerController>();
        if (networkPlayer != null && networkPlayer.IsOwner && targetModel != null)
        {
            Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }
}
