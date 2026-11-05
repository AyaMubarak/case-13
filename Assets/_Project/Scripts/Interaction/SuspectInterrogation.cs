using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

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

    public bool CanInterrogate()
    {
        // في الطور الفردي (Solo Mode) أو إذا لم يكن في غرفة أونلاين نشطة: فتح الاستجواب بالكامل
        if (PlayerPrefs.GetInt("Game_Mode", 1) == 0 || !PhotonNetwork.InRoom) return true;

        if (PlayerProfile.LocalPlayer == null) return true;
        if (PlayerProfile.LocalPlayer.currentRole == DetectiveRole.Interrogator ||
            PlayerProfile.LocalPlayer.currentRole == DetectiveRole.Any)
        {
            return true;
        }

        // إذا لم يكن هناك لاعب بدور Interrogator متصل ونشط حالياً في الغرفة (مثلاً خرج أو فصل جهازه أو الفريق أقل من 4)،
        // نسمح لأي محقق متواجد باستجواب المتهم حتى لا يعلق الفريق أبداً!
        if (!IsActiveInterrogatorInRoom())
        {
            return true;
        }

        return false;
    }

    private static bool IsActiveInterrogatorInRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList == null) return false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p == null || p.IsInactive) continue;
            if (p.CustomProperties != null && p.CustomProperties.TryGetValue("Role", out object roleObj) && roleObj != null)
            {
                string r = roleObj.ToString().ToLowerInvariant();
                if (r.Contains("interrogat"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public string InteractionPrompt
    {
        get
        {
            if (!CanInterrogate())
            {
                return "[LOCKED] Requires Interrogator";
            }
            return promptMessage;
        }
    }

    public void Interact()
    {
        if (!CanInterrogate())
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowOnScreenNotification("ACCESS DENIED // REQUIRES INTERROGATOR");
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
        
        if (UIManager.Instance != null && UIManager.Instance.IsTouchDevice())
        {
            UIManager.Instance.ShowOnScreenNotification("STATEMENT RECORDED // TAP [FILES] TO VIEW DOSSIER");
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification("STATEMENT RECORDED // PRESS [B] TO VIEW DOSSIER");
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
