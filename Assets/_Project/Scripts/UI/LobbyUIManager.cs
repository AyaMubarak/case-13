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

    [Header("Match Start")]
    [SerializeField] private bool autoStartWhenRoomFull = true;

    private const string ROLE_KEY = "Role";
    private const string GENDER_KEY = "Gender";
    private static readonly Color ReadyColor = new Color(0.25f, 0.95f, 0.55f, 1f);
    private static readonly Color WaitingColor = new Color(0.62f, 0.68f, 0.76f, 1f);
    private static readonly Color HostColor = new Color(1f, 0.8f, 0.25f, 1f);
    private bool roomOperationInProgress;
    private bool leavingForDuplicateRole;
    private bool returnToCharacterSelectAfterLeaving;

    private void Awake()
    {
        InitializeRoleSpritesAndImages();
        ConfigureSlotPresentation();
    }

    private void Start()
    {
        InitializeRoleSpritesAndImages();
        ConfigureSlotPresentation();

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
        if (!CanStartRoomOperation()) return;

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
        string enteredCode = roomCodeInput != null ? roomCodeInput.text : "";
        string roomCode = string.IsNullOrWhiteSpace(enteredCode)
            ? Random.Range(1000, 9999).ToString()
            : enteredCode.Trim();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };

        UpdateStatus($"Creating Room {roomCode}...");
        roomOperationInProgress = true;
        SetButtonsInteractable(false);
        PhotonNetwork.CreateRoom(roomCode, options);
    }

    public void OnJoinRoomClicked()
    {
        if (!CanStartRoomOperation()) return;

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
        roomOperationInProgress = true;
        SetButtonsInteractable(false);
        PhotonNetwork.JoinRoom(enteredCode);
    }

    // --- كولباكات الغرفة ومنع تكرار الدور ---

    public override void OnJoinedRoom()
    {
        roomOperationInProgress = false;
        UpdateStatus($"In Room: {PhotonNetwork.CurrentRoom.Name}");
        if (roomCodeInput != null) roomCodeInput.text = PhotonNetwork.CurrentRoom.Name;

        // التحقق من تكرار الدور مع اللاعبين الموجودين
        if (IsRoleDuplicate())
        {
            leavingForDuplicateRole = true;
            UpdateStatus("ROLE ALREADY TAKEN // RETURNING TO LOBBY");
            PhotonNetwork.LeaveRoom();
            return;
        }

        UpdateSlotsUI();
        TryStartMatchWhenFull();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateSlotsUI();
        TryStartMatchWhenFull();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateSlotsUI();
    }

    public override void OnLeftRoom()
    {
        roomOperationInProgress = false;
        SetButtonsInteractable(true);
        UpdateSlotsUI();

        if (leavingForDuplicateRole)
        {
            leavingForDuplicateRole = false;
            UpdateStatus("ROLE ALREADY TAKEN // CHOOSE ANOTHER ROLE OR ROOM");
        }
        else
        {
            UpdateStatus("Left room. Ready to create or join another room.");
        }

        if (returnToCharacterSelectAfterLeaving)
        {
            returnToCharacterSelectAfterLeaving = false;
            gameObject.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateSlotsUI();
        TryStartMatchWhenFull();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdateSlotsUI();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        roomOperationInProgress = false;
        UpdateStatus(returnCode == ErrorCode.GameFull ? "ROOM IS FULL // TRY ANOTHER ROOM" : "ROOM NOT FOUND // CHECK THE CODE");
        SetButtonsInteractable(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        roomOperationInProgress = false;
        UpdateStatus("ROOM CODE EXISTS // TRY ANOTHER CODE");
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
        TextMeshProUGUI[] names = { player1Name, player2Name, player3Name, player4Name };
        TextMeshProUGUI[] statuses = { player1Status, player2Status, player3Status, player4Status };
        Image[] roleImages = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };

        // تصفير جميع الكروت لوضع الانتظار أولاً (Waiting فقط)
        for (int i = 0; i < 4; i++)
        {
            if (names[i] != null) names[i].text = $"OPEN SLOT {i + 1}";
            if (statuses[i] != null)
            {
                statuses[i].text = "WAITING FOR PLAYER";
                statuses[i].color = WaitingColor;
            }

            if (roleImages[i] != null)
            {
                roleImages[i].sprite = null;
                roleImages[i].enabled = false;
            }
        }

        if (!PhotonNetwork.InRoom)
        {
            // إذا لم ننضم لغرفة بعد، نعرض بيانات اللاعب المحلي فقط في الخانة الأولى بحالة Ready
            if (names[0] != null) names[0].text = FormatPlayerName(PlayerPrefs.GetString("Player_Name", "Detective 1"));
            if (statuses[0] != null)
            {
                statuses[0].text = "● READY TO JOIN";
                statuses[0].color = ReadyColor;
            }

            if (startGameButton != null) startGameButton.interactable = false;
            return;
        }

        // توزيع اللاعبين المتصلين في الغرفة على الكروت
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length && i < 4; i++)
        {
            string pName = string.IsNullOrEmpty(players[i].NickName) ? $"Player {i + 1}" : players[i].NickName;
            if (names[i] != null) names[i].text = FormatPlayerName(pName);
            
            // حالة اللاعب المنضم تكون دائماً Ready فقط
            if (statuses[i] != null)
            {
                statuses[i].text = players[i].IsMasterClient ? "★ HOST • READY" : "● READY";
                statuses[i].color = players[i].IsMasterClient ? HostColor : ReadyColor;
            }

        }

        // تفعيل زر Start Game فقط إذا كان اللاعب هو الـ MasterClient (صاحب الغرفة)
        if (startGameButton != null)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }

        RectTransform lobbyRect = transform as RectTransform;
        if (lobbyRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(lobbyRect);
    }

    public void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
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
            roomOperationInProgress = true;
            returnToCharacterSelectAfterLeaving = true;
            SetButtonsInteractable(false);
            UpdateStatus("LEAVING ROOM...");
            PhotonNetwork.LeaveRoom();
            return;
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

    private bool CanStartRoomOperation()
    {
        if (roomOperationInProgress)
        {
            UpdateStatus("PLEASE WAIT FOR THE CURRENT ROOM REQUEST");
            return false;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("CONNECTING TO SERVER // PLEASE WAIT");
            return false;
        }

        if (PhotonNetwork.InRoom)
        {
            UpdateStatus("ALREADY IN A ROOM // LEAVE IT BEFORE JOINING ANOTHER");
            return false;
        }

        return true;
    }

    private void TryStartMatchWhenFull()
    {
        if (!autoStartWhenRoomFull || !PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount != PhotonNetwork.CurrentRoom.MaxPlayers) return;
        if (HasDuplicateRoles()) return;

        UpdateStatus("ROOM FULL // LOADING GAME...");
        OnStartGameClicked();
    }

    private bool HasDuplicateRoles()
    {
        HashSet<string> assignedRoles = new HashSet<string>();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(ROLE_KEY, out object value)) return true;
            if (!assignedRoles.Add(value.ToString())) return true;
        }

        return false;
    }

    private void ConfigureSlotPresentation()
    {
        TextMeshProUGUI[] names = { player1Name, player2Name, player3Name, player4Name };
        TextMeshProUGUI[] statuses = { player1Status, player2Status, player3Status, player4Status };
        Image[] roleImages = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };

        for (int index = 0; index < names.Length; index++)
        {
            TextMeshProUGUI nameText = names[index];
            if (nameText == null) continue;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 14f;
            nameText.fontSizeMax = 25f;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.alignment = TextAlignmentOptions.Center;

            // These labels are separate children of LobbyPanel (not children of
            // the slot cards). Their scene positions already match their cards.
            // Re-anchoring them to LobbyPanel made all four labels overlap.
        }

        for (int index = 0; index < statuses.Length; index++)
        {
            TextMeshProUGUI statusText = statuses[index];
            if (statusText == null) continue;
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 11f;
            statusText.fontSizeMax = 17f;
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            statusText.overflowMode = TextOverflowModes.Ellipsis;
            statusText.alignment = TextAlignmentOptions.Center;

            // Keep the authored scene position; only text sizing/alignment is
            // configured here so the layout remains stable on every aspect ratio.
        }

        for (int index = 0; index < roleImages.Length; index++)
        {
            Image roleImage = roleImages[index];
            if (roleImage == null) continue;
            roleImage.type = Image.Type.Simple;
            roleImage.preserveAspect = true;
            // Role images are also authored in the scene at the centre of each
            // card. Do not apply anchors relative to the whole lobby panel.
        }
    }

    private static void PositionTextInsideSlot(RectTransform element, float minX, float minY, float maxX, float maxY)
    {
        if (element == null || element.parent == null) return;

        element.anchorMin = new Vector2(minX, minY);
        element.anchorMax = new Vector2(maxX, maxY);
        element.offsetMin = Vector2.zero;
        element.offsetMax = Vector2.zero;
    }

    private static string FormatPlayerName(string playerName)
    {
        const int maxLength = 16;
        string trimmedName = string.IsNullOrWhiteSpace(playerName) ? "DETECTIVE" : playerName.Trim();
        return trimmedName.Length <= maxLength ? trimmedName.ToUpperInvariant() : trimmedName.Substring(0, maxLength - 1).ToUpperInvariant() + "…";
    }
}
