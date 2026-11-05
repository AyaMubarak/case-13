using Photon.Pun;
using UnityEngine;

/// <summary>Disabled legacy team chat overlay to prevent any unwanted UI box on gameplay screens.</summary>
public class PhotonTeamChat : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        CleanupLegacyChatUI();
        Destroy(this);
    }

    public static void CleanupLegacyChatUI()
    {
        GameObject toggle = GameObject.Find("ChatToggle");
        if (toggle != null) Destroy(toggle);

        GameObject panel = GameObject.Find("TeamChatPanel");
        if (panel != null) Destroy(panel);
    }
}
