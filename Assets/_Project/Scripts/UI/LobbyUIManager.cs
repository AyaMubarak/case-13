using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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

    // The authored slot covers already contain the male/female silhouettes.
    // Reuse them so the lobby reflects the gender selected by each player.
    private Sprite maleGenderSprite;
    private Sprite femaleGenderSprite;

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
    private static readonly Color HostColor = new Color(0.62f, 0.68f, 0.76f, 1f);

    private static readonly string[] ALL_ROLES = new string[]
    {
        "Field Detective",
        "Forensics Expert",
        "Digital Analyst",
        "Interrogator"
    };

    private bool roomOperationInProgress;
    private bool returnToCharacterSelectAfterLeaving;
    private Coroutine operationTimeoutCoroutine;
    private Coroutine fullRoomStartCoroutine;
    private bool matchStarting;

    // عناصر نافذة تنبيه تكرار الدور
    private GameObject roleConflictAlertPanel;
    private TextMeshProUGUI alertMessageText;
    private Button alertSwitchButton;
    private TextMeshProUGUI alertSwitchButtonText;
    private Button alertLeaveButton;
    private string pendingFreeRole = "";

    private void Awake()
    {
        InitializeRoleSpritesAndImages();
        ConfigureSlotPresentation();
        CreateRoleConflictAlertUI();
    }

    private void Start()
    {
        InitializeRoleSpritesAndImages();
        ConfigureSlotPresentation();
        EnsureConnected();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        StopOperationTimeout();
        roomOperationInProgress = false;
        HideRoleConflictAlert();
        UpdateSlotsUI();
        EnsureConnected();
    }

    public static void EnsureConnectedGlobal()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void EnsureConnected()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("Connecting to server...");
            SetButtonsInteractable(false);
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        else if (PhotonNetwork.InLobby)
        {
            UpdateStatus("Ready to Create or Join Room");
            SetButtonsInteractable(true);
        }
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void InitializeRoleSpritesAndImages()
    {
        if (fieldDetectiveSprite == null)
            fieldDetectiveSprite = Resources.Load<Sprite>("Role_FieldDetective") ?? Resources.Load<Sprite>("Sprites/Role_FieldDetective");
        if (forensicsExpertSprite == null)
            forensicsExpertSprite = Resources.Load<Sprite>("Role_ForensicsExpert") ?? Resources.Load<Sprite>("Sprites/Role_ForensicsExpert");
        if (digitalAnalystSprite == null)
            digitalAnalystSprite = Resources.Load<Sprite>("Role_DigitalAnalyst") ?? Resources.Load<Sprite>("Sprites/Role_DigitalAnalyst");
        if (interrogatorSprite == null)
            interrogatorSprite = Resources.Load<Sprite>("Role_Interrogator") ?? Resources.Load<Sprite>("Sprites/Role_Interrogator");

        Image[] images = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };
        string[] coverNames = { "Slot1_Cover", "Slot2_Cover", "Slot3_Cover", "Slot4_Cover" };

        for (int i = 0; i < 4; i++)
        {
            if (images[i] == null)
            {
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
                images[i].type = Image.Type.Simple;
                images[i].preserveAspect = true;
            }
        }

        player1RoleImage = images[0];
        player2RoleImage = images[1];
        player3RoleImage = images[2];
        player4RoleImage = images[3];

        if (maleGenderSprite == null && images.Length > 0 && images[0] != null)
            maleGenderSprite = images[0].sprite;
        if (femaleGenderSprite == null && images.Length > 1 && images[1] != null)
            femaleGenderSprite = images[1].sprite;
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

    private Sprite GetGenderSprite(object genderValue, int slotIndex)
    {
        string gender = genderValue != null ? genderValue.ToString() : string.Empty;
        bool female = gender.Equals("Female", System.StringComparison.OrdinalIgnoreCase);

        Sprite selected = female ? femaleGenderSprite : maleGenderSprite;
        if (selected != null) return selected;

        // Preserve the authored placeholder silhouette for an unassigned slot
        // if an imported sprite is unavailable.
        return (slotIndex % 2 == 1) ? femaleGenderSprite : maleGenderSprite;
    }

    // --- كولباكات الاتصال بالسيرفر ---

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected to Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        roomOperationInProgress = false;
        StopOperationTimeout();
        UpdateStatus("Ready to Create or Join Room");
        SetButtonsInteractable(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        roomOperationInProgress = false;
        StopOperationTimeout();
        HideRoleConflictAlert();
        SetButtonsInteractable(false);
        UpdateStatus($"Disconnected: {cause}. Reconnecting...");
        Debug.LogWarning($"[Lobby PUN] Disconnected: {cause}. Attempting reconnect...");
        
        PhotonNetwork.ConnectUsingSettings();
    }

    // --- أحداث أزرار الواجهة ---

    public void OnCreateRoomClicked()
    {
        if (!CanStartRoomOperation()) return;

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

        // توليد رمز غرفة عشوائي جديد دائماً عند إنشاء الغرفة لتجنب تعارض الكود المكتوب بالخطأ
        string roomCode = Random.Range(1000, 9999).ToString();
        if (roomCodeInput != null)
        {
            roomCodeInput.text = roomCode;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true,
            PlayerTtl = 120000,
            EmptyRoomTtl = 60000
        };

        UpdateStatus($"Creating Room {roomCode}...");
        roomOperationInProgress = true;
        SetButtonsInteractable(false);
        StartOperationTimeout("Creating room timed out. Please try again.");

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
        StartOperationTimeout("Joining room timed out. Please check the code and try again.");

        PhotonNetwork.JoinRoom(enteredCode);
    }

    // --- كولباكات الغرفة ومنع تكرار الدور مع نافذة التنبيه الذكية ---

    public override void OnJoinedRoom()
    {
        roomOperationInProgress = false;
        StopOperationTimeout();
        UpdateStatus($"In Room: {PhotonNetwork.CurrentRoom.Name}");
        if (roomCodeInput != null) roomCodeInput.text = PhotonNetwork.CurrentRoom.Name;

        if (IsLocalNameDuplicate())
        {
            UpdateStatus("NAME ALREADY IN USE // CHOOSE A DIFFERENT NAME");
            PhotonNetwork.LeaveRoom();
            return;
        }

        // التحقق الحصري من تكرار الدور للداخل الجديد فقط
        if (IsLocalPlayerDuplicate())
        {
            HandleRoleDuplicate();
            return;
        }

        HideRoleConflictAlert();
        UpdateSlotsUI();
        TryStartMatchWhenFull();
    }

    private bool IsLocalNameDuplicate()
    {
        string localName = (PhotonNetwork.NickName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(localName) || !PhotonNetwork.InRoom) return false;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer) continue;
            if (string.Equals((player.NickName ?? string.Empty).Trim(), localName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateSlotsUI();
        
        // إذا انضم لاعب جديد، هو فقط من سيفحص نفسه ويظهر له التنبيه، اللاعبون القدامى لا يظهر لهم شيء

        TryStartMatchWhenFull();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateSlotsUI();
    }

    public override void OnLeftRoom()
    {
        matchStarting = false;
        if (fullRoomStartCoroutine != null)
        {
            StopCoroutine(fullRoomStartCoroutine);
            fullRoomStartCoroutine = null;
        }
        roomOperationInProgress = false;
        StopOperationTimeout();
        HideRoleConflictAlert();
        SetButtonsInteractable(true);
        UpdateSlotsUI();

        UpdateStatus("Left room. Ready to create or join another room.");

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

        // فحص حصري للاعب المحلي فقط
        if (PhotonNetwork.InRoom && IsLocalPlayerDuplicate())
        {
            HandleRoleDuplicate();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        roomOperationInProgress = false;
        StopOperationTimeout();
        UpdateStatus(returnCode == ErrorCode.GameFull ? "ROOM IS FULL // TRY ANOTHER ROOM" : "ROOM NOT FOUND // CHECK THE CODE");
        SetButtonsInteractable(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        roomOperationInProgress = false;
        StopOperationTimeout();
        UpdateStatus("ROOM CREATION FAILED // TRY AGAIN");
        SetButtonsInteractable(true);
    }

    // --- فحص الأدوار وإيجاد الدور الفارغ ---

    /// <summary>
    /// يفحص إذا كان اللاعب المحلي هو المنضم حديثاً والذي يحمل دوراً مكرراً مع لاعب أقدم منه في الغرفة
    /// </summary>
    private bool IsLocalPlayerDuplicate()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return false;

        string myRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p == PhotonNetwork.LocalPlayer) continue;

            if (p.CustomProperties.TryGetValue(ROLE_KEY, out object roleVal) && roleVal != null)
            {
                if (roleVal.ToString().Equals(myRole, System.StringComparison.OrdinalIgnoreCase))
                {
                    // الحصرية: إذا كان اللاعب الآخر يملك ActorNumber أصغر، فهو الأقدم ويحتفظ بدوره، واللاعب المحلي هو المكرر
                    if (p.ActorNumber < myActorNumber)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private List<string> GetAvailableRoles()
    {
        HashSet<string> takenRoles = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            // نأخذ أدوار بقية اللاعبين فقط
            if (p == PhotonNetwork.LocalPlayer) continue;

            if (p.CustomProperties.TryGetValue(ROLE_KEY, out object roleVal) && roleVal != null)
            {
                takenRoles.Add(roleVal.ToString());
            }
        }

        List<string> available = new List<string>();
        foreach (string role in ALL_ROLES)
        {
            if (!takenRoles.Contains(role))
            {
                available.Add(role);
            }
        }

        return available;
    }

    private void HandleRoleDuplicate()
    {
        string currentRole = PlayerPrefs.GetString("Player_Role", "Field Detective");
        List<string> freeRoles = GetAvailableRoles();

        if (freeRoles.Count > 0)
        {
            pendingFreeRole = freeRoles[0];

            // إعادة التعيين التلقائي الفوري (Auto-Assignment) لدور شاغر لتفادي التكرار وضمان تنوع الفريق
            Debug.Log($"<color=orange>[Lobby PUN] Role duplicate detected for local player! Auto-assigning from {currentRole} to {pendingFreeRole}</color>");
            PlayerPrefs.SetString("Player_Role", pendingFreeRole);
            PlayerPrefs.Save();

            Hashtable playerProps = new Hashtable
            {
                { ROLE_KEY, pendingFreeRole }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            // عرض نافذة التنبيه المطابقة للصورة تماماً
            ShowRoleConflictAlert(
                "Someone is already playing as this role.\nYour role has been auto-assigned to an\navailable role to avoid duplication.\n\nPlease check the Lobby Screen to see\neveryone's selected roles.",
                "OK",
                true
            );
        }
        else
        {
            pendingFreeRole = "";
            ShowRoleConflictAlert(
                "All detective roles are occupied in this room.\nPlease join another session or check with your team.",
                "OK",
                false
            );
        }
    }

    // --- واجهة تنبيه تكرار الدور (Alert Popup) ---

    private void CreateRoleConflictAlertUI()
    {
        if (roleConflictAlertPanel != null) return;

        // 1. حاوية التنبيه كامل الشاشة مع خلفية تعتيم سينمائية
        roleConflictAlertPanel = new GameObject("RoleConflictAlertPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        roleConflictAlertPanel.transform.SetParent(transform, false);

        RectTransform alertRect = roleConflictAlertPanel.GetComponent<RectTransform>();
        alertRect.anchorMin = Vector2.zero;
        alertRect.anchorMax = Vector2.one;
        alertRect.offsetMin = Vector2.zero;
        alertRect.offsetMax = Vector2.zero;

        Image bgImage = roleConflictAlertPanel.GetComponent<Image>();
        bgImage.color = new Color(0.02f, 0.03f, 0.05f, 0.75f); // خلفية ضبابية تعتيم مطابقة للصورة

        Sprite suppliedPopup = Resources.Load<Sprite>("RoleConflictPopup");
        if (suppliedPopup == null)
        {
            Texture2D popupTexture = Resources.Load<Texture2D>("RoleConflictPopup");
            if (popupTexture != null)
            {
                suppliedPopup = Sprite.Create(
                    popupTexture,
                    new Rect(0f, 0f, popupTexture.width, popupTexture.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
        GameObject artworkObject = new GameObject("RoleConflictArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        artworkObject.transform.SetParent(roleConflictAlertPanel.transform, false);
        RectTransform artworkRect = artworkObject.GetComponent<RectTransform>();
        artworkRect.anchorMin = Vector2.zero;
        artworkRect.anchorMax = Vector2.one;
        artworkRect.offsetMin = Vector2.zero;
        artworkRect.offsetMax = Vector2.zero;
        Image artworkImage = artworkObject.GetComponent<Image>();
        artworkImage.sprite = suppliedPopup;
        artworkImage.preserveAspect = false;
        artworkImage.raycastTarget = false;

        // 2. إطار التنبيه الخارجي البرتقالي الذهبي (Orange Glow Outline)
        GameObject outerBox = new GameObject("AlertOuterBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outerBox.transform.SetParent(roleConflictAlertPanel.transform, false);

        RectTransform outerRect = outerBox.GetComponent<RectTransform>();
        outerRect.anchorMin = new Vector2(0.24f, 0.16f);
        outerRect.anchorMax = new Vector2(0.76f, 0.84f);
        outerRect.offsetMin = Vector2.zero;
        outerRect.offsetMax = Vector2.zero;

        Image outerImage = outerBox.GetComponent<Image>();
        outerImage.color = new Color(1f, 0.62f, 0.08f, 0.95f); // خط برتقالي ذهبي ساطع يطابق الصورة
        outerBox.SetActive(false);

        // الصندوق الداخلي شبه الشفاف
        GameObject innerBox = new GameObject("AlertInnerBox", typeof(RectTransform), typeof(Image));
        innerBox.transform.SetParent(outerBox.transform, false);

        RectTransform innerRect = innerBox.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        Image innerImage = innerBox.GetComponent<Image>();
        innerImage.color = new Color(0.07f, 0.07f, 0.10f, 0.85f); // كحلي داكن شفاف قليلاً لإظهار اللوحة وراءه

        // 3. العنوان الرئيسي: ROLE CONFLICT!
        GameObject titleObj = new GameObject("AlertTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(innerBox.transform, false);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.78f);
        titleRect.anchorMax = new Vector2(0.95f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "<i><b><color=#FFA51F>ROLE CONFLICT!</color></b></i>";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32f;

        // 4. نص التنبيه المكتوب بنفس صياغة الصورة تماماً
        GameObject textObj = new GameObject("AlertText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(innerBox.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.28f);
        textRect.anchorMax = new Vector2(0.92f, 0.76f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        alertMessageText = textObj.GetComponent<TextMeshProUGUI>();
        alertMessageText.alignment = TextAlignmentOptions.Center;
        alertMessageText.fontSize = 17f;
        alertMessageText.lineSpacing = 10f;
        alertMessageText.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
        alertMessageText.text = "Someone is already playing as this role.\nYour role has been auto-assigned to an\navailable role to avoid duplication.\n\nPlease check the Lobby Screen to see\neveryone's selected roles.";

        // 5. زر التأكيد البرتقالي المتدرج: OK
        GameObject okBtnObj = new GameObject("Btn_OK", typeof(RectTransform), typeof(Image), typeof(Button));
        okBtnObj.transform.SetParent(innerBox.transform, false);

        RectTransform okRect = okBtnObj.GetComponent<RectTransform>();
        okRect.anchorMin = new Vector2(0.33f, 0.08f);
        okRect.anchorMax = new Vector2(0.67f, 0.23f);
        okRect.offsetMin = Vector2.zero;
        okRect.offsetMax = Vector2.zero;

        Image okBtnImg = okBtnObj.GetComponent<Image>();
        okBtnImg.color = new Color(1f, 1f, 1f, 0f);

        alertSwitchButton = okBtnObj.GetComponent<Button>();
        alertSwitchButton.onClick.AddListener(OnConfirmSwitchRole);
        // Keep the transparent hit target active even though the old generated
        // dialog frame is hidden.
        okBtnObj.transform.SetParent(roleConflictAlertPanel.transform, false);
        okRect.anchorMin = new Vector2(0.38f, 0.13f);
        okRect.anchorMax = new Vector2(0.62f, 0.28f);
        okRect.offsetMin = Vector2.zero;
        okRect.offsetMax = Vector2.zero;

        GameObject okTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        okTxtObj.transform.SetParent(okBtnObj.transform, false);
        RectTransform okTxtRect = okTxtObj.GetComponent<RectTransform>();
        okTxtRect.anchorMin = Vector2.zero;
        okTxtRect.anchorMax = Vector2.one;
        okTxtRect.offsetMin = Vector2.zero;
        okTxtRect.offsetMax = Vector2.zero;

        alertSwitchButtonText = okTxtObj.GetComponent<TextMeshProUGUI>();
        alertSwitchButtonText.alignment = TextAlignmentOptions.Center;
        alertSwitchButtonText.text = "<b>OK</b>";
        alertSwitchButtonText.fontSize = 22f;
        alertSwitchButtonText.color = new Color(0f, 0f, 0f, 0f);

        // 6. زر Back في الزاوية السفلية اليسرى للرجوع إلى شاشة التخصيص
        GameObject backBtnObj = new GameObject("Btn_BackAlert", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(roleConflictAlertPanel.transform, false);

        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.04f, 0.035f);
        backRect.anchorMax = new Vector2(0.15f, 0.11f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        Image backBtnImg = backBtnObj.GetComponent<Image>();
        backBtnImg.color = new Color(1f, 1f, 1f, 0f);

        alertLeaveButton = backBtnObj.GetComponent<Button>();
        alertLeaveButton.onClick.AddListener(OnLeaveFromAlert);

        GameObject backTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        backTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backTxtRect = backTxtObj.GetComponent<RectTransform>();
        backTxtRect.anchorMin = Vector2.zero;
        backTxtRect.anchorMax = Vector2.one;
        backTxtRect.offsetMin = Vector2.zero;
        backTxtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI backTxt = backTxtObj.GetComponent<TextMeshProUGUI>();
        backTxt.alignment = TextAlignmentOptions.Center;
        backTxt.text = "<b><size=18>↩</size>   Back</b>";
        backTxt.fontSize = 15f;
        backTxt.color = new Color(1f, 1f, 1f, 0f);

        roleConflictAlertPanel.SetActive(false);
    }

    private void ShowRoleConflictAlert(string message, string switchButtonTitle, bool canSwitch)
    {
        CreateRoleConflictAlertUI();
        if (roleConflictAlertPanel == null) return;

        if (alertMessageText != null) alertMessageText.text = message;

        if (alertSwitchButton != null)
        {
            alertSwitchButton.gameObject.SetActive(canSwitch);
            if (alertSwitchButtonText != null) alertSwitchButtonText.text = switchButtonTitle;
        }

        roleConflictAlertPanel.transform.SetAsLastSibling();
        roleConflictAlertPanel.SetActive(true);
    }

    private void HideRoleConflictAlert()
    {
        if (roleConflictAlertPanel != null)
        {
            roleConflictAlertPanel.SetActive(false);
        }
    }

    private void OnConfirmSwitchRole()
    {
        Debug.Log($"[Lobby PUN] Acknowledged role conflict. Operating with assigned role: {PlayerPrefs.GetString("Player_Role")}");
        HideRoleConflictAlert();
        UpdateSlotsUI();
        TryStartMatchWhenFull();
    }

    private void OnLeaveFromAlert()
    {
        HideRoleConflictAlert();
        OnBackClicked();
    }

    // --- تحديث كروت اللاعبين الأربعة في الـ UI ---

    private void UpdateSlotsUI()
    {
        TextMeshProUGUI[] names = { player1Name, player2Name, player3Name, player4Name };
        TextMeshProUGUI[] statuses = { player1Status, player2Status, player3Status, player4Status };
        Image[] roleImages = { player1RoleImage, player2RoleImage, player3RoleImage, player4RoleImage };

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
                // Keep the authored grey silhouette/cover in the card. Do not
                // assign a sprite here: doing so creates the white rectangles
                // seen over the slot artwork.
                roleImages[i].sprite = null;
                roleImages[i].enabled = true;
            }
        }

        if (!PhotonNetwork.InRoom)
        {
            if (names[0] != null) names[0].text = FormatPlayerName(PlayerPrefs.GetString("Player_Name", "Detective 1"));
            if (statuses[0] != null)
            {
                statuses[0].text = "● READY TO JOIN";
                statuses[0].color = ReadyColor;
            }

            if (startGameButton != null) startGameButton.interactable = false;
            return;
        }

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length && i < 4; i++)
        {
            string pName = string.IsNullOrEmpty(players[i].NickName) ? $"Player {i + 1}" : players[i].NickName;
            if (names[i] != null) names[i].text = FormatPlayerName(pName);
            
            if (statuses[i] != null)
            {
                statuses[i].text = players[i].IsMasterClient ? "★ HOST • READY" : "● READY";
                statuses[i].color = players[i].IsMasterClient ? HostColor : ReadyColor;
            }

        }

        if (startGameButton != null)
        {
            // لا يمكن للهوست بدء اللعبة مطلقاً إلا إذا كان لا يوجد أي تكرار بالأدوار
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }

        RectTransform lobbyRect = transform as RectTransform;
        if (lobbyRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(lobbyRect);
    }

    public void OnStartGameClicked()
    {
        CaseIntroCutscene.ResetCompletedIntrosForNewMatch();
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            if (matchStarting) return;
            matchStarting = true;
            fullRoomStartCoroutine = StartCoroutine(StartWhenRoomStateIsReady());
        }
    }

    private IEnumerator StartWhenRoomStateIsReady()
    {
        // Player custom properties can arrive a frame after the fourth player
        // enters. Give Photon time to deliver them, then re-check a few times.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            {
                matchStarting = false;
                yield break;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers && !HasDuplicateRoles())
            {
                string targetScene = PlayerPrefs.GetString("Selected_Case", "Case01_Main");
                PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
                {
                    { "ActiveCaseScene", targetScene }
                });
                UpdateStatus("Loading Game Scene...");
                PhotonNetwork.LoadLevel(targetScene);
                yield break;
            }

            yield return new WaitForSeconds(0.15f);
        }

        matchStarting = false;
        fullRoomStartCoroutine = null;
        UpdateStatus("ROOM FULL // WAITING FOR PLAYER DATA...");
    }

    public void OnBackClicked()
    {
        StopOperationTimeout();
        HideRoleConflictAlert();

        if (PhotonNetwork.InRoom)
        {
            roomOperationInProgress = true;
            returnToCharacterSelectAfterLeaving = true;
            SetButtonsInteractable(false);
            UpdateStatus("LEAVING ROOM...");
            StartOperationTimeout("Leaving room timed out.");
            PhotonNetwork.LeaveRoom();
            return;
        }

        roomOperationInProgress = false;
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
            UpdateStatus("PLEASE WAIT FOR CURRENT OPERATION");
            return false;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("CONNECTING TO SERVER // PLEASE WAIT");
            EnsureConnected();
            return false;
        }

        if (PhotonNetwork.InRoom)
        {
            UpdateStatus("ALREADY IN A ROOM // LEAVE IT FIRST");
            return false;
        }

        return true;
    }

    // --- مؤقت منع التجمد والتعليق (Timeout) ---

    private void StartOperationTimeout(string timeoutMessage)
    {
        StopOperationTimeout();
        operationTimeoutCoroutine = StartCoroutine(OperationTimeoutRoutine(timeoutMessage));
    }

    private void StopOperationTimeout()
    {
        if (operationTimeoutCoroutine != null)
        {
            StopCoroutine(operationTimeoutCoroutine);
            operationTimeoutCoroutine = null;
        }
    }

    private IEnumerator OperationTimeoutRoutine(string timeoutMessage)
    {
        yield return new WaitForSeconds(8f);

        roomOperationInProgress = false;
        operationTimeoutCoroutine = null;
        UpdateStatus(timeoutMessage);
        SetButtonsInteractable(true);
        Debug.LogWarning("[Lobby PUN] Operation timed out. UI controls restored.");
    }

    private void TryStartMatchWhenFull()
    {
        if (!autoStartWhenRoomFull || !PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount != PhotonNetwork.CurrentRoom.MaxPlayers) return;
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
        }

        for (int index = 0; index < statuses.Length; index++)
        {
            TextMeshProUGUI stText = statuses[index];
            if (stText == null) continue;
            stText.enableAutoSizing = true;
            stText.fontSizeMin = 11f;
            stText.fontSizeMax = 17f;
            stText.textWrappingMode = TextWrappingModes.NoWrap;
            stText.overflowMode = TextOverflowModes.Ellipsis;
            stText.alignment = TextAlignmentOptions.Center;
        }

        for (int index = 0; index < roleImages.Length; index++)
        {
            Image roleImage = roleImages[index];
            if (roleImage == null) continue;
            roleImage.type = Image.Type.Simple;
            roleImage.preserveAspect = true;
        }
    }

    private static string FormatPlayerName(string playerName)
    {
        const int maxLength = 16;
        string trimmedName = string.IsNullOrWhiteSpace(playerName) ? "DETECTIVE" : playerName.Trim();
        return trimmedName.Length <= maxLength ? trimmedName.ToUpperInvariant() : trimmedName.Substring(0, maxLength - 1).ToUpperInvariant() + "…";
    }
}
