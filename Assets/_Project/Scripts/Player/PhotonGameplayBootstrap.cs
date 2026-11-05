using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds multiplayer replication automatically after a case scene loads.</summary>
public static class PhotonGameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddReplicatorIfNeeded();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddReplicatorIfNeeded();
    }

    private static void AddReplicatorIfNeeded()
    {
        // إزالة أي عناصر شات قديمة عالقة على الشاشة إن وجدت
        foreach (PhotonTeamChat oldChat in Object.FindObjectsByType<PhotonTeamChat>(FindObjectsInactive.Include))
        {
            if (oldChat != null) Object.Destroy(oldChat);
        }
        GameObject oldToggle = GameObject.Find("ChatToggle");
        if (oldToggle != null) Object.Destroy(oldToggle);
        GameObject oldPanel = GameObject.Find("TeamChatPanel");
        if (oldPanel != null) Object.Destroy(oldPanel);

        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.StartsWith("Case")) return;

        bool isCoopMode = PlayerPrefs.GetInt("Game_Mode", 1) == 1 || PhotonNetwork.InRoom;
        if (!isCoopMode) return;

        if (Object.FindAnyObjectByType<PhotonGameplayReplicator>() != null) return;

        GameObject manager = new GameObject("Photon Gameplay Replicator");
        manager.AddComponent<PhotonGameplayReplicator>();
    }
}
