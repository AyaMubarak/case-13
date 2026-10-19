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

    [Header("Role Icons Sprites (صور الأدوار)")]
    [SerializeField] private Sprite fieldDetectiveSprite;
    [SerializeField] private Sprite forensicsExpertSprite;
    [SerializeField] private Sprite digitalAnalystSprite;
    [SerializeField] private Sprite interrogatorSprite;

    [Header("Slot Role Images (الصور المعروضة في فتحات اللاعبين)")]
    [SerializeField] private Image player1RoleImage;
    [SerializeField] private Image player2RoleImage;
    [SerializeField] private Image player3RoleImage;
    [SerializeField] private Image player4RoleImage;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Navigation Panels")]
    [SerializeField] private GameObject characterSelectPanel;

    private const string ROLE_KEY = "Role";
    private const string GENDER_KEY = "Gender";

    private void Awake()
    {
        InitializeRoleSpritesAndImages();
    }

    private void Start()
    {
        InitializeRoleSpritesAndImages();

        // الاتصال بسيرفر Photon تلقائياً عند بدء التشغيل
        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true;
        }
    }

    private void InitializeRoleSpritesAndImages()
    {
        // 1. تحميل صور الأدوار تلقائياً إذا لم تكن مسندة عبر Inspector
        if (fieldDetectiveSprite == null)
            fieldDetectiveSprite = Resources.Load<Sprite>("Role_FieldDetective") ?? Resources.Load<Sprite>("Sprites/Role_FieldDetective");
        if (forensicsExpertSprite == null)
            forensicsExpertSprite = Resources.Load<Sprite>("Role_ForensicsExpert") ?? Resources.Load<Sprite>("Sprites/Role_ForensicsExpert");
        if (digitalAnalystSprite == null)
            digitalAnalystSprite = Resources.Load<Sprite>("Role_DigitalAnalyst") ?? Resources.Load<Sprite>("Sprites/Role_DigitalAnalyst");
        if (interrogatorSprite == null)
            interrogatorSprite = Resources.Load<Sprite>("Role_Interrogator") ?? Resources.Load<Sprite>("Sprites/Role_Interrogator");

        // 2. ربط صور الكروت تلقائياً مع كائنات Slot Covers المتاحة في المشهد
        Image[] images = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };
        string[] coverNames = { "Slot1_Cover", "Slot2_Cover", "Slot3_Cover", "Slot4_Cover" };

        for (int i = 0; i < 4; i++)
        {
            if (images[i] == null)
            {
                // ابحث عن Slot_Cover المناسب في لوحة اللوبي
                foreach (Image img in GetComponentsInChildren<Image>(true))
                {
                    if (img != null && img.name.Replace(" ", "").Equals(coverNames[i], System.StringComparison.OrdinalIgnoreCase))
                    {
                        images[i] = img;
                        break;
                    }
                }
            }

            if (images[i] != null)
            {
                // إعداد الصورة لعرض الأيقونة بنسبتها الطبيعية فوق الكرت
                images[i].type = Image.Type.Simple;
                images[i].preserveAspect = true;
            }
        }

        player1RoleImage = images[0];
        player2RoleImage = images[1];
        player3RoleImage = images[2];
        player4RoleImage = images[3];
    }

    public Sprite GetRoleSprite(string roleName)
    {
        if (string.IsNullOrEmpty(roleName)) return fieldDetectiveSprite;

        string r = roleName.ToLower();
        if (r.Contains("forensic") || r.Contains("doctor"))
            return forensicsExpertSprite != null ? forensicsExpertSprite : fieldDetectiveSprite;
        if (r.Contains("digital") || r.Contains("tech") || r.Contains("analyst"))
            return digitalAnalystSprite != null ? digitalAnalystSprite : fieldDetectiveSprite;
        if (r.Contains("interrogat"))
            return interrogatorSprite != null ? interrogatorSprite : fieldDetectiveSprite;

        return fieldDetectiveSprite;
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
        // مصفوفة لنصوص الكروت وصور الأدوار الأربعة
        TextMeshProUGUI[] names = { player1Name, player2Name, player3Name, player4Name };
        TextMeshProUGUI[] statuses = { player1Status, player2Status, player3Status, player4Status };
        Image[] roleImages = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };

        // تصفير جميع الكروت لوضع الانتظار أولاً (Waiting فقط)
        for (int i = 0; i < 4; i++)
        {
            if (names[i] != null) names[i].text = $"Player {i + 1}";
            if (statuses[i] != null)
            {
                statuses[i].text = "Waiting";
                statuses[i].color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }

            if (roleImages[i] != null)
            {
                // إخفاء أو تعتيم الصورة عندما تكون الفتحة خالية
                roleImages[i].sprite = null;
                roleImages[i].color = new Color(0.12f, 0.12f, 0.15f, 1f); // لون خلفية البطاقة لتغطية أي رسمة قديمة
            }
        }

        if (!PhotonNetwork.InRoom)
        {
            // إذا لم ننضم لغرفة بعد، نعرض بيانات اللاعب المحلي فقط في الخانة الأولى بحالة Ready
            string localRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
            if (names[0] != null) names[0].text = PlayerPrefs.GetString("Player_Name", "Detective 1");
            if (statuses[0] != null)
            {
                statuses[0].text = "Ready";
                statuses[0].color = new Color(0.2f, 0.9f, 0.4f, 1f); // لون أخضر جذاب لحالة Ready
            }

            if (roleImages[0] != null)
            {
                roleImages[0].sprite = GetRoleSprite(localRole);
                roleImages[0].color = Color.white;
            }

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
                : "Field Detective";

            if (names[i] != null) names[i].text = pName;
            
            // حالة اللاعب المنضم تكون دائماً Ready فقط
            if (statuses[i] != null)
            {
                statuses[i].text = "Ready";
                statuses[i].color = new Color(0.2f, 0.9f, 0.4f, 1f);
            }

            // عرض صورة الدور الخاصة باللاعب في البطاقة
            if (roleImages[i] != null)
            {
                roleImages[i].sprite = GetRoleSprite(pRole);
                roleImages[i].color = Color.white;
            }
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
            string sceneToLoad = PlayerPrefs.GetString("Selected_Case", "Case01_Main");
            PhotonNetwork.LoadLevel(sceneToLoad); 
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
