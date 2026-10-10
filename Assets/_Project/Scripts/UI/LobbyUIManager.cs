using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("Room Input & Status")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Player Slots UI (الفتحات الأربعة)")]
    [SerializeField] private TextMeshProUGUI player1Name;
    [SerializeField] private TextMeshProUGUI player1Status;
    [Space]
    [SerializeField] private TextMeshProUGUI player2Name;
    [SerializeField] private TextMeshProUGUI player2Status;
    [Space]
    [SerializeField] private TextMeshProUGUI player3Name;
    [SerializeField] private TextMeshProUGUI player3Status;
    [Space]
    [SerializeField] private TextMeshProUGUI player4Name;
    [SerializeField] private TextMeshProUGUI player4Status;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Navigation Panels")]
    [SerializeField] private GameObject characterSelectPanel;

    private const string ROLE_KEY = "Role";
    private const string GENDER_KEY = "Gender";

    private void Start()
    {
        // الاتصال بسيرفر Photon تلقائياً عند بدء التشغيل
        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        UpdateSlotsUI();
    }

    // --- كولباكات الاتصال بالسيرفر ---

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected to Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("Ready to Create or Join Room");
        SetButtonsInteractable(true);
    }

    // --- أحداث أزرار الواجهة ---

    public void OnCreateRoomClicked()
    {
        string localRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        string localName = PlayerPrefs.GetString("Player_Name", "Detective 1");
        string localGender = PlayerPrefs.GetString("Player_Gender", "Male");

        // إعداد اسم اللاعب وخصائص الدور
        PhotonNetwork.NickName = localName;
        Hashtable playerProps = new Hashtable
        {
            { ROLE_KEY, localRole },
            { GENDER_KEY, localGender }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        // إنشاء رمز الغرفة
        string roomCode = string.IsNullOrWhiteSpace(roomCodeInput.text)
            ? Random.Range(1000, 9999).ToString()
            : roomCodeInput.text.Trim();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };

        UpdateStatus($"Creating Room {roomCode}...");
        SetButtonsInteractable(false);
        PhotonNetwork.CreateRoom(roomCode, options);
    }

    public void OnJoinRoomClicked()
    {
        string enteredCode = roomCodeInput != null ? roomCodeInput.text.Trim() : "";

        if (string.IsNullOrEmpty(enteredCode))
        {
            UpdateStatus("Please enter room code!");
            return;
        }

        string localRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        string localName = PlayerPrefs.GetString("Player_Name", "Detective 1");
        string localGender = PlayerPrefs.GetString("Player_Gender", "Male");

        PhotonNetwork.NickName = localName;
        Hashtable playerProps = new Hashtable
        {
            { ROLE_KEY, localRole },
            { GENDER_KEY, localGender }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        UpdateStatus($"Joining Room {enteredCode}...");
        SetButtonsInteractable(false);
        PhotonNetwork.JoinRoom(enteredCode);
    }

    // --- كولباكات الغرفة ومنع تكرار الدور ---

    public override void OnJoinedRoom()
    {
        UpdateStatus($"In Room: {PhotonNetwork.CurrentRoom.Name}");
        if (roomCodeInput != null) roomCodeInput.text = PhotonNetwork.CurrentRoom.Name;

        // التحقق من تكرار الدور مع اللاعبين الموجودين
        if (IsRoleDuplicate())
        {
            UpdateStatus("Role already taken! Please choose another role.");
            PhotonNetwork.LeaveRoom();
            return;
        }

        UpdateSlotsUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateSlotsUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateSlotsUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdateSlotsUI();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateStatus("Room not found or full!");
        SetButtonsInteractable(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateStatus("Room code exists! Try another.");
        SetButtonsInteractable(true);
    }

    // --- فحص منع تكرار الدور ---

    private bool IsRoleDuplicate()
    {
        string myRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        int count = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey(ROLE_KEY))
            {
                if (p.CustomProperties[ROLE_KEY].ToString() == myRole)
                {
                    count++;
                }
            }
        }

        return count > 1; // إذا وُجد أكثر من لاعب بنفس الدور
    }

    // --- تحديث كروت اللاعبين الأربعة في الـ UI ---

    private void UpdateSlotsUI()
    {
        // مصفوفة لنصوص الكروت الأربعة
        TextMeshProUGUI[] names = { player1Name, player2Name, player3Name, player4Name };
        TextMeshProUGUI[] statuses = { player1Status, player2Status, player3Status, player4Status };

        // تصفير جميع الكروت لوضع الانتظار أولاً
        for (int i = 0; i < 4; i++)
        {
            if (names[i] != null) names[i].text = $"Player {i + 1}";
            if (statuses[i] != null) statuses[i].text = "Waiting...";
        }

        if (!PhotonNetwork.InRoom)
        {
            // إذا لم ننضم لغرفة بعد، نعرض بيانات اللاعب المحلي فقط في الخانة الأولى
            if (names[0] != null) names[0].text = PlayerPrefs.GetString("Player_Name", "Detective 1");
            if (statuses[0] != null) statuses[0].text = PlayerPrefs.GetString("Player_Role", "Field Detective");
            if (startGameButton != null) startGameButton.interactable = false;
            return;
        }

        // توزيع اللاعبين المتصلين في الغرفة على الكروت
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length && i < 4; i++)
        {
            string pName = string.IsNullOrEmpty(players[i].NickName) ? $"Player {i + 1}" : players[i].NickName;
            string pRole = players[i].CustomProperties.ContainsKey(ROLE_KEY) 
                ? players[i].CustomProperties[ROLE_KEY].ToString() 
                : "Assigned";

            if (names[i] != null) names[i].text = pName;
            if (statuses[i] != null) statuses[i].text = pRole;
        }

        // تفعيل زر Start Game فقط إذا كان اللاعب هو الـ MasterClient (صاحب الغرفة)
        if (startGameButton != null)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    public void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateStatus("Loading Game Scene...");
            // تغيير الانتقال ليعتمد على رقم المشهد (1) كما في إعداداتك السابقة
            PhotonNetwork.LoadLevel(1); 
        }
    }

    public void OnBackClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        gameObject.SetActive(false);
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
        }
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[Lobby PUN] {msg}");
    }

    private void SetButtonsInteractable(bool state)
    {
        if (createRoomButton != null) createRoomButton.interactable = state;
        if (joinRoomButton != null) joinRoomButton.interactable = state;
    }
}
