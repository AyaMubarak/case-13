using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mirrors the existing scene player for each teammate. This avoids requiring a separate
/// Resources Photon prefab while still making teammates visible and animated in gameplay scenes.
/// </summary>
public class PhotonGameplayReplicator : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte PoseEventCode = 71;
    private const string CompletedProperty = "CaseComplete";
    private readonly Dictionary<int, RemotePlayerReplica> remotePlayers = new();

    private PlayerMovement localMovement;
    private Vector3 lastPosition;
    private float nextPoseTime;
    private bool reportedCompletion;

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    private void Start()
    {
        ResolveLocalPlayer();
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom || localMovement == null) return;

        if (Time.unscaledTime >= nextPoseTime)
        {
            SendPose();
            nextPoseTime = Time.unscaledTime + 0.05f;
        }

        ReportCaseCompletion();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (remotePlayers.TryGetValue(otherPlayer.ActorNumber, out RemotePlayerReplica replica) && replica != null)
            Destroy(replica.gameObject);
        remotePlayers.Remove(otherPlayer.ActorNumber);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!changedProps.ContainsKey(CompletedProperty) || !AllPlayersCompleted()) return;
        UIManager.Instance?.ShowOnScreenNotification("TEAM READY // ALL DETECTIVES COMPLETED THEIR TASKS");
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != PoseEventCode || !PhotonNetwork.InRoom) return;
        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length != 9) return;

        int actorNumber = (int)data[0];
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) return;

        Vector3 position = new((float)data[1], (float)data[2], (float)data[3]);
        Quaternion rotation = new((float)data[4], (float)data[5], (float)data[6], (float)data[7]);
        float speed = (float)data[8];

        RemotePlayerReplica replica = GetOrCreateReplica(actorNumber, position, rotation);
        if (replica != null) replica.ApplyNetworkState(position, rotation, speed);
    }

    private void ResolveLocalPlayer()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Exclude);
        foreach (PlayerMovement player in players)
        {
            if (player.GetComponent<RemotePlayerReplica>() == null)
            {
                localMovement = player;
                lastPosition = player.transform.position;
                return;
            }
        }
    }

    private void SendPose()
    {
        Vector3 position = localMovement.transform.position;
        float speed = (position - lastPosition).magnitude / 0.05f;
        lastPosition = position;
        Quaternion rotation = localMovement.transform.rotation;
        object[] payload =
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            position.x, position.y, position.z,
            rotation.x, rotation.y, rotation.z, rotation.w,
            speed
        };

        PhotonNetwork.RaiseEvent(PoseEventCode, payload,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendUnreliable);
    }

    private RemotePlayerReplica GetOrCreateReplica(int actorNumber, Vector3 position, Quaternion rotation)
    {
        if (remotePlayers.TryGetValue(actorNumber, out RemotePlayerReplica existing) && existing != null)
            return existing;
        if (localMovement == null) ResolveLocalPlayer();
        if (localMovement == null) return null;

        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        string playerName = player != null ? player.NickName : $"Detective {actorNumber}";

        PlayerProfile.SuppressLocalRegistration = true;
        GameObject clone = Instantiate(localMovement.gameObject);
        PlayerProfile.SuppressLocalRegistration = false;

        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = true;

        RemotePlayerReplica replica = clone.AddComponent<RemotePlayerReplica>();
        replica.Initialize(position, rotation, playerName);
        remotePlayers[actorNumber] = replica;
        return replica;
    }

    private void ReportCaseCompletion()
    {
        if (reportedCompletion || EvidenceManager.Instance == null || !EvidenceManager.Instance.IsCaseComplete()) return;
        reportedCompletion = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { CompletedProperty, true } });
        UIManager.Instance?.ShowOnScreenNotification("YOUR TASK IS COMPLETE // WAITING FOR TEAMMATES");
        if (AllPlayersCompleted())
            UIManager.Instance?.ShowOnScreenNotification("TEAM READY // ALL DETECTIVES COMPLETED THEIR TASKS");
    }

    private bool AllPlayersCompleted()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount < 2) return false;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(CompletedProperty, out object complete) || !(bool)complete)
                return false;
        }
        return true;
    }
}
