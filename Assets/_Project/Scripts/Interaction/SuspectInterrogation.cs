using UnityEngine;

public class SuspectInterrogation : MonoBehaviour, IInteractable
{
    [Header("Suspect Dossier")]
    public string suspectName = "Nora Blake";
    public Sprite suspectPortrait; // خانة لرفع صورة المتهم من الـ Inspector

    [TextArea(2, 4)]
    public string initialAlibi = "I saw someone carrying a small sealed box toward the rear exit. They were wearing a dark jacket!";

    [TextArea(2, 4)]
    public string brokenAlibiResponse = "Alright, alright! My keycard log doesn't lie... I entered the office at 02:15, but I didn't pull the trigger!";

    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Press [E] to Interrogate Suspect";

    public bool isAlibiBroken { get; private set; } = false;

    public string InteractionPrompt
    {
        get
        {
            if (PlayerProfile.LocalPlayer != null &&
                PlayerProfile.LocalPlayer.currentRole != DetectiveRole.Interrogator &&
                PlayerProfile.LocalPlayer.currentRole != DetectiveRole.Any)
            {
                return "[LOCKED] Requires Interrogator";
            }
            return promptMessage;
        }
    }

    public void Interact()
    {
        // فحص صلاحية الدور
        if (PlayerProfile.LocalPlayer != null &&
            PlayerProfile.LocalPlayer.currentRole != DetectiveRole.Interrogator &&
            PlayerProfile.LocalPlayer.currentRole != DetectiveRole.Any)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("ACCESS DENIED // REQUIRES INTERROGATOR");
            }
            return;
        }

        // 1. تسجيل أو تحديث الأقوال مع تمرير الصورة الحقيقية
        if (InterrogationManager.Instance != null)
        {
            InterrogationManager.Instance.RecordOrUpdateStatement(suspectName, isAlibiBroken ? brokenAlibiResponse : initialAlibi, suspectPortrait);
        }

        // 2. إخبار مدير المشهد أن المتهم يجب أن يبدأ حركته نحو الكرسي
        var cutsceneManager = FindAnyObjectByType<SeparateInterrogationManager>();
        if (cutsceneManager != null)
        {
            cutsceneManager.StartSuspectMovement(suspectName);
        }

        // 3. إظهار إشعار حفظ الأقوال
        InterrogationUI.lastInterrogatedSuspect = this;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification("STATEMENT RECORDED // SUSPECT IS MOVING");
        }
        else
        {
            Debug.Log("STATEMENT RECORDED // SUSPECT IS MOVING");
        }
    }

    public void BreakAlibi()
    {
        isAlibiBroken = true;
        Debug.Log($"<color=green>[ALIBI CRACKED]</color> {suspectName}'s statement has been invalidated.");
    }
}