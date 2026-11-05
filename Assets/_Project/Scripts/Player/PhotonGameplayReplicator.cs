using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mirrors the existing scene player for each teammate. This avoids requiring a separate
/// Resources Photon prefab while making teammates fully visible in third-person with animations.
/// Also handles automatic reconnection, last-checkpoint restoration, and full squad state
/// synchronization when a device sleeps, backgrounds, or briefly loses connection.
/// </summary>
public class PhotonGameplayReplicator : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte PoseEventCode = 71;
    private const byte PoseSyncRequestCode = 72;
    private const string CompletedProperty = "CaseComplete";
    private const string ActiveCaseSceneProperty = "ActiveCaseScene";
    private readonly Dictionary<int, RemotePlayerReplica> remotePlayers = new Dictionary<int, RemotePlayerReplica>();

    private PlayerMovement localMovement;
    private Vector3 lastPosition;
    private float nextPoseTime;
    private float nextCheckpointSaveTime;
    private bool reportedCompletion;
    private bool wasInRoom;
    private bool isReconnecting;
    private string lastRoomName;
    private Coroutine reconnectCoroutine;

    // حفظ آخر نقطة (Position & Rotation) وصل إليها اللاعب في كل قضية بشكل ثابت
    private static string savedCheckpointScene = "";
    private static bool hasSavedCheckpoint = false;
    private static Vector3 savedCheckpointPosition;
    private static Quaternion savedCheckpointRotation;
    private static string loadingSceneTarget = "";
    private static float loadingSceneStartTime = -999f;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        PhotonNetwork.KeepAliveInBackground = 120000f;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    private void Start()
    {
        PhotonNetwork.KeepAliveInBackground = 120000f;
        PhotonNetwork.AutomaticallySyncScene = true;
        ResolveLocalPlayer();

        string currentScene = SceneManager.GetActiveScene().name;
        if (string.Equals(loadingSceneTarget, currentScene, System.StringComparison.OrdinalIgnoreCase))
        {
            loadingSceneTarget = "";
        }
        if (savedCheckpointScene != currentScene)
        {
            savedCheckpointScene = currentScene;
            hasSavedCheckpoint = false;
        }

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            wasInRoom = true;
            lastRoomName = PhotonNetwork.CurrentRoom.Name;
            SyncFullSquadState(false);
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom)
        {
            if (wasInRoom && !isReconnecting && PlayerPrefs.GetInt("Game_Mode", 1) == 1)
            {
                BeginReconnectFlow();
            }
            return;
        }

        wasInRoom = true;
        if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
        {
            lastRoomName = PhotonNetwork.CurrentRoom.Name;
        }

        if (localMovement == null)
        {
            ResolveLocalPlayer();
            if (localMovement == null) return;
        }

        // حفظ موقع اللاعب الحالي كآخر نقطة (Last Checkpoint) كل نصف ثانية
        if (Time.unscaledTime >= nextCheckpointSaveTime)
        {
            SaveLocalCheckpoint();
            nextCheckpointSaveTime = Time.unscaledTime + 0.5f;
        }

        if (Time.unscaledTime >= nextPoseTime)
        {
            SendPose();
            nextPoseTime = Time.unscaledTime + 0.05f; // 20 updates per second for smooth movement
        }

        ReportCaseCompletion();
    }

    private void SaveLocalCheckpoint()
    {
        if (localMovement == null) return;
        string currentScene = SceneManager.GetActiveScene().name;
        savedCheckpointScene = currentScene;
        savedCheckpointPosition = localMovement.transform.position;
        savedCheckpointRotation = localMovement.transform.rotation;
        hasSavedCheckpoint = true;
    }

    private void RestoreLocalCheckpoint()
    {
        if (!hasSavedCheckpoint) return;
        string currentScene = SceneManager.GetActiveScene().name;
        if (savedCheckpointScene != currentScene) return;

        if (localMovement == null) ResolveLocalPlayer();
        if (localMovement == null) return;

        Rigidbody rb = localMovement.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        localMovement.transform.position = savedCheckpointPosition;
        localMovement.transform.rotation = savedCheckpointRotation;
        lastPosition = savedCheckpointPosition;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveLocalCheckpoint();
            return;
        }

        // Device screen woke up / returned from background
        if (PlayerPrefs.GetInt("Game_Mode", 1) == 1)
        {
            if (!PhotonNetwork.InRoom && wasInRoom)
            {
                BeginReconnectFlow();
            }
            else if (PhotonNetwork.InRoom)
            {
                SyncFullSquadState(true);
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveLocalCheckpoint();
            return;
        }

        if (PlayerPrefs.GetInt("Game_Mode", 1) == 1)
        {
            if (!PhotonNetwork.InRoom && wasInRoom)
            {
                BeginReconnectFlow();
            }
            else if (PhotonNetwork.InRoom)
            {
                SyncFullSquadState(false);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer != null && otherPlayer.IsInactive)
        {
            // Keep replica while player is temporarily inactive (screen off)
            return;
        }

        if (otherPlayer != null && remotePlayers.TryGetValue(otherPlayer.ActorNumber, out RemotePlayerReplica replica) && replica != null)
        {
            Destroy(replica.gameObject);
        }
        if (otherPlayer != null)
        {
            remotePlayers.Remove(otherPlayer.ActorNumber);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (cause == DisconnectCause.DisconnectByClientLogic || cause == DisconnectCause.ApplicationQuit)
            return;

        SaveLocalCheckpoint();
        wasInRoom = wasInRoom || !string.IsNullOrEmpty(lastRoomName);
        if (wasInRoom && PlayerPrefs.GetInt("Game_Mode", 1) == 1)
        {
            BeginReconnectFlow();
        }
    }

    private void BeginReconnectFlow()
    {
        if (isReconnecting) return;
        isReconnecting = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification("RECONNECTING TO SQUAD... // SYNCING LIVE PROGRESS", 15f);
        }

        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
        }
        reconnectCoroutine = StartCoroutine(ReconnectRoutine());
    }

    private IEnumerator ReconnectRoutine()
    {
        int attempts = 0;
        while (!PhotonNetwork.InRoom && attempts < 15)
        {
            attempts++;

            if (!PhotonNetwork.IsConnected)
            {
                if (!PhotonNetwork.ReconnectAndRejoin())
                {
                    if (!PhotonNetwork.Reconnect())
                    {
                        PhotonNetwork.ConnectUsingSettings();
                    }
                }
            }
            else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !string.IsNullOrEmpty(lastRoomName))
            {
                if (!PhotonNetwork.RejoinRoom(lastRoomName))
                {
                    PhotonNetwork.JoinRoom(lastRoomName);
                }
            }

            yield return new WaitForSecondsRealtime(2.0f);
        }

        isReconnecting = false;
        reconnectCoroutine = null;
    }

    public override void OnConnectedToMaster()
    {
        if (wasInRoom && !PhotonNetwork.InRoom && !string.IsNullOrEmpty(lastRoomName))
        {
            if (!PhotonNetwork.RejoinRoom(lastRoomName))
            {
                PhotonNetwork.JoinRoom(lastRoomName);
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (wasInRoom && !PhotonNetwork.InRoom && !string.IsNullOrEmpty(lastRoomName))
        {
            PhotonNetwork.JoinRoom(lastRoomName);
        }
    }

    public override void OnJoinedRoom()
    {
        wasInRoom = true;
        isReconnecting = false;
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }

        if (PhotonNetwork.CurrentRoom != null)
        {
            lastRoomName = PhotonNetwork.CurrentRoom.Name;
        }

        RestoreLocalPlayerCustomProperties();
        ResolveLocalPlayer();
        RestoreLocalCheckpoint();
        SyncFullSquadState(true);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        // لا نقوم بتحميل المشهد يدوياً هنا لأن PhotonNetwork.AutomaticallySyncScene يتكفل بنقل كل المتصلين مع الهوست مرة واحدة بدون تكرار تحميل المشهد!
    }

    private void RestoreLocalPlayerCustomProperties()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;

        string savedRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        string savedGender = PlayerPrefs.GetString("Player_Gender", "Male");
        string savedName = PlayerPrefs.GetString("Player_Name", "Detective 1");

        if (!string.IsNullOrEmpty(savedName))
            PhotonNetwork.NickName = savedName;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "Role", savedRole },
            { "Gender", savedGender }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (PlayerProfile.LocalPlayer != null && !PlayerProfile.LocalPlayer.useLocalTestOverride)
        {
            PlayerProfile.LocalPlayer.LoadFromLobby();
        }
    }

    public void SyncFullSquadState(bool notifyPlayer)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        // 1. إذا انتقل الهوست والفريق إلى القضية الثانية (Case02_Main) أثناء انقطاع اللاعب، ينتقل معهم فوراً بنفس شخصيته!
        if (CheckAndSyncActiveCaseScene())
        {
            return;
        }

        // 2. التأكد من تخطي أي فيديو مقدمة عالق وإعادة اللاعب لآخر نقطة كان عندها
        if (CaseIntroCutscene.IsPlaying)
        {
            CaseIntroCutscene intro = FindAnyObjectByType<CaseIntroCutscene>(FindObjectsInactive.Include);
            if (intro != null && EvidenceManager.Instance != null && EvidenceManager.Instance.CollectedCount > 0)
            {
                intro.EndCutscene();
            }
        }
        RestoreLocalCheckpoint();

        // 3. مزامنة كافة الأدلة التي جمعها الفريق أثناء انقطاع اللاعب
        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.ForceSyncFromRoom(notifyPlayer);
        }

        // 4. مزامنة مرحلة التحقيق والاستجواب أو انتهاء القضية
        if (SeparateInterrogationManager.Instance != null)
        {
            SeparateInterrogationManager.Instance.SyncInterrogationStateFromRoom();
        }

        // 5. طلب مواقع وحركات الزملاء الحالية في الغرفة وإرسال موقع اللاعب الحالي لهم
        PhotonNetwork.RaiseEvent(PoseSyncRequestCode, null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
        if (localMovement != null)
        {
            SendPose();
        }

        if (notifyPlayer && UIManager.Instance != null)
        {
            int count = EvidenceManager.Instance != null ? EvidenceManager.Instance.CollectedCount : 0;
            int total = EvidenceManager.Instance != null ? EvidenceManager.Instance.totalEvidence : 5;
            UIManager.Instance.ShowOnScreenNotification($"SQUAD SYNC COMPLETE // EVIDENCE {count}/{total} UPDATED", 4.5f);
        }
    }

    private bool CheckAndSyncActiveCaseScene()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ActiveCaseSceneProperty, out object sceneObj) &&
            sceneObj is string targetScene &&
            !string.IsNullOrEmpty(targetScene))
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(currentScene, targetScene, System.StringComparison.OrdinalIgnoreCase) &&
                targetScene.StartsWith("Case"))
            {
                // منع تحميل المشهد مرتين إذا كان الانتقال جارياً بالفعل
                if (string.Equals(loadingSceneTarget, targetScene, System.StringComparison.OrdinalIgnoreCase) &&
                    Time.unscaledTime - loadingSceneStartTime < 8f)
                {
                    return true;
                }

                loadingSceneTarget = targetScene;
                loadingSceneStartTime = Time.unscaledTime;

                Debug.Log($"<color=cyan>[SQUAD SYNC]</color> Teammates moved to '{targetScene}'. Following squad now...");
                PlayerPrefs.SetString("Selected_Case", targetScene);
                PlayerPrefs.Save();

                hasSavedCheckpoint = false;
                savedCheckpointScene = targetScene;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOnScreenNotification("SQUAD MOVED TO CASE 02 // SYNCHRONIZING...", 4f);
                }

                SceneManager.LoadScene(targetScene);
                return true;
            }
        }

        return false;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer != null && targetPlayer != PhotonNetwork.LocalPlayer &&
            (changedProps.ContainsKey("Role") || changedProps.ContainsKey("Gender")))
        {
            if (remotePlayers.TryGetValue(targetPlayer.ActorNumber, out RemotePlayerReplica replica) && replica != null)
            {
                string pName = !string.IsNullOrEmpty(targetPlayer.NickName) ? targetPlayer.NickName : $"Detective {targetPlayer.ActorNumber}";
                string role = targetPlayer.CustomProperties.TryGetValue("Role", out object r) && r != null ? r.ToString() : "Field Detective";
                string gender = targetPlayer.CustomProperties.TryGetValue("Gender", out object g) && g != null ? g.ToString() : "Male";
                replica.Initialize(replica.transform.position, replica.transform.rotation, pName, role, gender);
            }
        }

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
            if (player == null || player.IsInactive) continue;
            if (!player.CustomProperties.TryGetValue(CompletedProperty, out object complete) || !(bool)complete)
                return false;
        }
        return true;
    }
}
