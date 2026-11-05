using UnityEngine;
using TMPro;

/// <summary>Visual-only third-person representation of a teammate in the crime scene.</summary>
public class RemotePlayerReplica : MonoBehaviour
{
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Animator[] animators;
    private float movementSpeed;
    private Transform nameplateTransform;
    private Camera mainCamera;

    public void Initialize(Vector3 position, Quaternion rotation, string playerName, string role, string gender)
    {
        transform.SetPositionAndRotation(position, rotation);
        targetPosition = position;
        targetRotation = rotation;
        gameObject.name = $"[Teammate] {playerName}";

        SetupVisual(role, gender);
        CreateFloatingNameplate(playerName, role);

        mainCamera = Camera.main;
    }

    public void ApplyNetworkState(Vector3 position, Quaternion rotation, float speed)
    {
        targetPosition = position;
        targetRotation = rotation;
        movementSpeed = speed;
    }

    private void Update()
    {
        // تحريك ودوران سلس للزميل في أرجاء الغرفة
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 14f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 14f);

        // تحديث أنيميشن المشي والوقوف (Walk / Idle)
        bool isWalking = movementSpeed > 0.05f;
        float animSpeed = isWalking ? 1f : 0f;

        if (animators != null)
        {
            for (int i = 0; i < animators.Length; i++)
            {
                Animator anim = animators[i];
                if (anim == null || !anim.gameObject.activeInHierarchy) continue;

                SetFloatIfPresent(anim, "Speed", animSpeed);
                SetFloatIfPresent(anim, "speed", animSpeed);
                SetBoolIfPresent(anim, "IsWalking", isWalking);
                SetBoolIfPresent(anim, "isWalking", isWalking);
            }
        }

        // جعل لوحة الاسم تتجه دائماً نحو كاميرا اللاعب
        if (nameplateTransform != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                nameplateTransform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void SetupVisual(string role, string gender)
    {
        // إزالة مكون التحكم الشبكي قبل تفعيل النموذج حتى لا يعامله كأنه اللاعب المحلي
        // ويحوّل الـ renderers إلى ShadowsOnly.
        var netPlayer = GetComponent<NetworkPlayerController>();
        if (netPlayer != null) DestroyImmediate(netPlayer);

        // إخفاء جميع مجسمات اللاعب الآخر. نحتفظ بالمعلومات الشبكية ولوحة الاسم
        // فقط، ولا نعرض أي شخصية ثلاثية الأبعاد.
        CharacterVisualSelector visualSelector = GetComponent<CharacterVisualSelector>();
        if (visualSelector != null)
        {
            visualSelector.HideAllModels();
            visualSelector.enabled = false;
        }

        // إخفاء أي مجسمات تحت Visuals بشكل قاطع
        Transform visualsRoot = transform.Find("Visuals");

        if (visualsRoot != null)
        {
            for (int i = 0; i < visualsRoot.childCount; i++)
            {
                GameObject childObj = visualsRoot.GetChild(i).gameObject;
                childObj.SetActive(false);
            }
        }

        // لا توجد شخصية مرئية؛ لا حاجة لتحديث أنيميشن المجسم.
        animators = GetComponentsInChildren<Animator>(false);
        foreach (Animator anim in animators)
        {
            if (anim != null)
            {
                anim.enabled = true;
                anim.applyRootMotion = false;
            }
        }

        // نسخة الزميل للعرض والحركة فقط: لا كاميرا، لا تفاعل، ولا إدخال محلي.
        foreach (Camera camera in GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (AudioListener listener in GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
    }

    private void CreateFloatingNameplate(string playerName, string role)
    {
        GameObject nameplateObj = new GameObject("TeammateNameplate");
        nameplateObj.transform.SetParent(transform, false);
        nameplateObj.transform.localPosition = new Vector3(0f, 2.15f, 0f); // فوق رأس الشخصية
        nameplateTransform = nameplateObj.transform;

        TextMeshPro text = nameplateObj.AddComponent<TextMeshPro>();
        Color roleColor = GetRoleColor(role);
        string roleHex = ColorUtility.ToHtmlStringRGB(roleColor);
        text.text = $"<color=#{roleHex}><b>● {playerName.ToUpperInvariant()}</b></color>\n<size=70%><color=#{roleHex}>[{role.ToUpperInvariant()}]</color></size>";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2.4f;
        text.isOverlay = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.22f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
    }

    private static Color GetRoleColor(string role)
    {
        string value = string.IsNullOrEmpty(role) ? string.Empty : role.ToLowerInvariant();
        if (value.Contains("forensic") || value.Contains("doctor"))
            return new Color(1f, 0.72f, 0.12f); // Forensics / gold
        if (value.Contains("digital") || value.Contains("tech") || value.Contains("analyst"))
            return new Color(0.15f, 0.85f, 1f); // Digital / cyan
        if (value.Contains("interrogat"))
            return new Color(1f, 0.32f, 0.38f); // Interrogator / red
        return new Color(0.35f, 0.85f, 0.45f); // Field Detective / green
    }

    private static void SetFloatIfPresent(Animator animator, string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private static void SetBoolIfPresent(Animator animator, string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }
}
