using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mirrors the existing scene player for each teammate. This avoids requiring a separate
/// Resources Photon prefab while making teammates fully visible in third-person with animations.
/// </summary>
public class PhotonGameplayReplicator : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte PoseEventCode = 71;
    private const byte PoseSyncRequestCode = 72;
    private const string CompletedProperty = "CaseComplete";
    private readonly Dictionary<int, RemotePlayerReplica> remotePlayers = new Dictionary<int, RemotePlayerReplica>();

    private PlayerMovement localMovement;
    private Vector3 lastPosition;
    private float nextPoseTime;
    private bool reportedCompletion;
    private bool wasInRoom;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Start()
    {
        ResolveLocalPlayer();
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;
        wasInRoom = true;

        if (localMovement == null)
        {
            ResolveLocalPlayer();
            if (localMovement == null) return;
        }

        if (Time.unscaledTime >= nextPoseTime)
        {
            SendPose();
            nextPoseTime = Time.unscaledTime + 0.05f; // 20 updates per second for smooth movement
        }

        ReportCaseCompletion();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (remotePlayers.TryGetValue(otherPlayer.ActorNumber, out RemotePlayerReplica replica) && replica != null)
        {
            Destroy(replica.gameObject);
        }
        remotePlayers.Remove(otherPlayer.ActorNumber);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // PUN does not automatically re-enter a room after a mobile/network
        // interruption. Remember that this gameplay client was in a room.
        wasInRoom = wasInRoom || PhotonNetwork.InRoom;
        if (wasInRoom)
            PhotonNetwork.ReconnectAndRejoin();
    }

    public override void OnJoinedRoom()
    {
        wasInRoom = true;
        ResolveLocalPlayer();
        // Ask every teammate for a reliable current pose snapshot. This fixes
        // stale/empty teammate positions after reconnecting.
        PhotonNetwork.RaiseEvent(PoseSyncRequestCode, null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!changedProps.ContainsKey(CompletedProperty) || !AllPlayersCompleted()) return;
        UIManager.Instance?.ShowOnScreenNotification("TEAM READY // ALL DETECTIVES COMPLETED THEIR TASKS");
    }

    public void OnEvent(EventData photonEvent)
    {
        if (!PhotonNetwork.InRoom) return;

        if (photonEvent.Code == PoseSyncRequestCode)
        {
            int requester = photonEvent.Sender;
            SendPoseTo(requester, SendOptions.SendReliable);
            return;
        }

        if (photonEvent.Code != PoseEventCode) return;
        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length != 9) return;

        int actorNumber = (int)data[0];
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) return;

        Vector3 position = new Vector3((float)data[1], (float)data[2], (float)data[3]);
        Quaternion rotation = new Quaternion((float)data[4], (float)data[5], (float)data[6], (float)data[7]);
        float speed = (float)data[8];

        RemotePlayerReplica replica = GetOrCreateReplica(actorNumber, position, rotation);
        if (replica != null)
        {
            replica.ApplyNetworkState(position, rotation, speed);
        }
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

    private void SendPoseTo(int actorNumber, SendOptions sendOptions)
    {
        if (localMovement == null) ResolveLocalPlayer();
        if (localMovement == null) return;

        Vector3 position = localMovement.transform.position;
        Quaternion rotation = localMovement.transform.rotation;
        object[] payload =
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            position.x, position.y, position.z,
            rotation.x, rotation.y, rotation.z, rotation.w,
            0f
        };

        PhotonNetwork.RaiseEvent(PoseEventCode, payload,
            new RaiseEventOptions { TargetActors = new[] { actorNumber } }, sendOptions);
    }

    private RemotePlayerReplica GetOrCreateReplica(int actorNumber, Vector3 position, Quaternion rotation)
    {
        if (remotePlayers.TryGetValue(actorNumber, out RemotePlayerReplica existing) && existing != null)
            return existing;

        if (localMovement == null) ResolveLocalPlayer();
        if (localMovement == null) return null;

        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        string playerName = player != null && !string.IsNullOrEmpty(player.NickName) ? player.NickName : $"Detective {actorNumber}";
        string role = "Field Detective";
        string gender = "Male";

        if (player != null && player.CustomProperties != null)
        {
            if (player.CustomProperties.TryGetValue("Role", out object r) && r != null) role = r.ToString();
            if (player.CustomProperties.TryGetValue("Gender", out object g) && g != null) gender = g.ToString();
        }

        PlayerProfile.SuppressLocalRegistration = true;
        GameObject clone = Instantiate(localMovement.gameObject);
        PlayerProfile.SuppressLocalRegistration = false;

        // إزالة الكاميرا ومستمع الصوت من كائن الزميل حتى تبقى كاميرا الـ FPS فقط للاعب المحلي
        foreach (Camera cam in clone.GetComponentsInChildren<Camera>(true)) Destroy(cam);
        foreach (AudioListener al in clone.GetComponentsInChildren<AudioListener>(true)) Destroy(al);
        foreach (CameraFollow cf in clone.GetComponentsInChildren<CameraFollow>(true)) Destroy(cf);

        // تعطيل سكربتات التحكم المحلي والتصادم للاعب المستنسخ
        PlayerMovement pm = clone.GetComponent<PlayerMovement>();
        if (pm != null) Destroy(pm);

        PlayerInteraction pi = clone.GetComponent<PlayerInteraction>();
        if (pi != null) Destroy(pi);

        foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
        }

        foreach (Collider col in clone.GetComponentsInChildren<Collider>(true))
        {
            col.isTrigger = true; // لمنع حبس اللاعبين ببعضهم
        }

        RemotePlayerReplica replica = clone.AddComponent<RemotePlayerReplica>();
        replica.Initialize(position, rotation, playerName, role, gender);
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
