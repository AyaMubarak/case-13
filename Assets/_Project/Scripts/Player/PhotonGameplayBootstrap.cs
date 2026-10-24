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
        if (!PhotonNetwork.InRoom) return;
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.StartsWith("Case")) return;
        if (Object.FindAnyObjectByType<PhotonGameplayReplicator>() != null) return;

        GameObject manager = new GameObject("Photon Gameplay Replicator");
        manager.AddComponent<PhotonGameplayReplicator>();
    }
}
