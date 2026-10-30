using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Self-installing local-test helper. No scene setup needed: it runs automatically after every scene load.
/// It activates the player, hides the role panel, fixes the camera target and logs what it finds.
/// Turn it off by setting ENABLED to false.
/// </summary>
public static class LocalTestBootstrap
{
    private static readonly bool ENABLED = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!ENABLED) return;

        SceneManager.sceneLoaded += (s, m) => Run();
        Run();
    }

    private static void Run()
    {
        PlayerMovement move = FindLocalPlayerMovement();
        Debug.Log("[LocalTest] Scene: " + SceneManager.GetActiveScene().name +
                  " | PlayerMovement found: " + (move != null));

        if (move == null)
        {
            Debug.LogError("[LocalTest] No PlayerMovement in this scene! Add PlayerMovement to the player object (or the prefab is not spawned).");
            return;
        }

        GameObject player = move.gameObject;

        // Case 2 uses a first-person camera: do not render the player's own
        // body/arms when the camera rotates inside the character model.
        foreach (Renderer playerRenderer in player.GetComponentsInChildren<Renderer>(true))
            playerRenderer.enabled = false;

        EnsureEvidenceGameplay(player);

        if (!player.activeInHierarchy)
        {
            Debug.LogWarning("[LocalTest] Player was inactive -> activating.");
            player.SetActive(true);
        }

        if (!move.enabled)
        {
            Debug.LogWarning("[LocalTest] PlayerMovement was disabled -> enabling.");
            move.enabled = true;
        }

        move.forceSimpleMove = true;

        PlayerProfile profile = player.GetComponent<PlayerProfile>();
        if (profile != null && PlayerProfile.LocalPlayer == null)
            profile.LoadFromLobby();

        CameraFollow camFollow = Object.FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
        if (camFollow != null)
        {
            camFollow.target = player.transform;
            Debug.Log("[LocalTest] CameraFollow target set to " + player.name);
        }
        else
        {
            Debug.LogWarning("[LocalTest] No CameraFollow in scene.");
        }

        RoleSelectionUI roleUI = Object.FindAnyObjectByType<RoleSelectionUI>(FindObjectsInactive.Include);
        if (roleUI != null && roleUI.selectionPanel != null)
            roleUI.selectionPanel.SetActive(false);

        // Keep the controls visible while testing Case 2 in the simulator.
        GameObject mobileControls = null;
        foreach (RectTransform rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
        {
            if (rect.name == "MobileControls")
            {
                mobileControls = rect.gameObject;
                break;
            }
        }
        if (mobileControls != null)
        {
            mobileControls.SetActive(true);
            RectTransform controlsRect = mobileControls.GetComponent<RectTransform>();
            if (controlsRect != null)
            {
                controlsRect.localScale = Vector3.one;
                controlsRect.anchorMin = Vector2.zero;
                controlsRect.anchorMax = Vector2.one;
                controlsRect.offsetMin = Vector2.zero;
                controlsRect.offsetMax = Vector2.zero;
            }

            MobileJoystick joystick = mobileControls.GetComponentInChildren<MobileJoystick>(true);
            if (joystick != null)
            {
                joystick.gameObject.SetActive(true);
                if (joystick.joystickBackground == null)
                    joystick.joystickBackground = joystick.transform.parent as RectTransform;
                if (joystick.joystickHandle == null)
                {
                    Transform handle = joystick.transform.Find("Handle");
                    if (handle == null) handle = joystick.transform.Find("JoystickHandle");
                    if (handle == null && joystick.transform.childCount > 0) handle = joystick.transform.GetChild(0);
                    joystick.joystickHandle = handle as RectTransform;
                }
                if (joystick.joystickBackground != null && joystick.joystickHandle != null)
                {
                    Image backgroundImage = joystick.joystickBackground.GetComponent<Image>();
                    if (backgroundImage != null) backgroundImage.raycastTarget = true;
                    Image handleImage = joystick.joystickHandle.GetComponent<Image>();
                    if (handleImage != null) handleImage.raycastTarget = true;
                    move.mobileJoystick = joystick;
                }

                Canvas canvas = mobileControls.GetComponentInParent<Canvas>(true);
                if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                if (Object.FindAnyObjectByType<EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<EventSystem>();
                    eventSystem.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        GameObject skipButtonObject = null;
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button.name.IndexOf("Skip", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                skipButtonObject = button.gameObject;
                break;
            }
        }
        if (skipButtonObject != null)
        {
            RectTransform skipRect = skipButtonObject.GetComponent<RectTransform>();
            if (skipRect != null)
            {
                skipRect.anchorMin = new Vector2(1f, 1f);
                skipRect.anchorMax = new Vector2(1f, 1f);
                skipRect.pivot = new Vector2(1f, 1f);
                skipRect.anchoredPosition = new Vector2(-30f, -30f);
                skipRect.sizeDelta = new Vector2(180f, 60f);
                skipRect.localScale = Vector3.one;
            }
            Text label = skipButtonObject.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                GameObject labelObject = new GameObject("SkipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelObject.transform.SetParent(skipButtonObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                label = labelObject.GetComponent<Text>();
            }
            label.text = "SKIP";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_Text tmpLabel = skipButtonObject.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null)
            {
                tmpLabel.text = "SKIP >>";
                tmpLabel.alignment = TextAlignmentOptions.Center;
                tmpLabel.fontSize = 24f;
                tmpLabel.fontStyle = FontStyles.Bold;
                tmpLabel.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                tmpLabel.raycastTarget = false;
            }

            Image skipImage = skipButtonObject.GetComponent<Image>();
            if (skipImage != null)
                skipImage.color = Color.white;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[LocalTest] Ready. Player=" + player.name + " pos=" + player.transform.position);
    }

    private static PlayerMovement FindLocalPlayerMovement()
    {
        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (PlayerMovement player in players)
        {
            if (player != null && player.GetComponent<RemotePlayerReplica>() == null)
                return player;
        }

        // Case 2 can be opened as a scene that contains only the camera/light.
        // Keep local testing usable by creating a minimal controller when the
        // authored Player prefab was not included in the scene.
        if (SceneManager.GetActiveScene().name.Contains("Case02"))
        {
            GameObject player = new GameObject("Player (Local Test Fallback)");
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<Rigidbody>();
            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 1f, 0f);
            return player.AddComponent<PlayerMovement>();
        }

        return null;
    }

    private static void EnsureEvidenceGameplay(GameObject player)
    {
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction == null)
            interaction = player.AddComponent<PlayerInteraction>();

        if (EvidenceManager.Instance == null)
        {
            GameObject manager = new GameObject("EvidenceManager");
            manager.AddComponent<EvidenceManager>();
        }

        UIManager ui = UIManager.Instance;
        if (ui == null)
        {
            GameObject canvasObject = new GameObject("Case2_RuntimeHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

            GameObject promptObject = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            promptObject.transform.SetParent(canvasObject.transform, false);
            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.12f);
            promptRect.anchorMax = new Vector2(0.5f, 0.12f);
            promptRect.sizeDelta = new Vector2(900f, 100f);
            TextMeshProUGUI prompt = promptObject.GetComponent<TextMeshProUGUI>();
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.fontSize = 32f;
            prompt.color = Color.white;

            ui = canvasObject.AddComponent<UIManager>();
            ui.interactionPromptText = prompt;

            ui.notificationBannerText = CreateRuntimeLabel(canvasObject.transform, "NotificationBanner", new Vector2(0.5f, 0.82f), 26f);
            ui.evidenceCounterText = CreateRuntimeLabel(canvasObject.transform, "EvidenceCounter", new Vector2(0.08f, 0.92f), 22f);
        }

        // Guarantee a working mobile hand button. Existing Case 1-style buttons
        // are reused; otherwise create a small fallback button in the lower-right.
        if (interaction.mobileInteractBtn == null)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                string n = button.name.ToLowerInvariant();
                if (n.Contains("interact") || n.Contains("hand") || n.Contains("collect") || n.Contains("investigate"))
                {
                    interaction.mobileInteractBtn = button.gameObject;
                    break;
                }
            }
        }
        if (interaction.mobileInteractBtn == null && ui != null)
        {
            Canvas canvas = ui.GetComponent<Canvas>();
            if (canvas != null)
            {
                GameObject hand = new GameObject("MobileInteractHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                hand.transform.SetParent(canvas.transform, false);
                RectTransform handRect = hand.GetComponent<RectTransform>();
                handRect.anchorMin = new Vector2(0.86f, 0.12f);
                handRect.anchorMax = new Vector2(0.96f, 0.25f);
                handRect.offsetMin = Vector2.zero;
                handRect.offsetMax = Vector2.zero;
                Image handImage = hand.GetComponent<Image>();
                handImage.color = new Color(0.04f, 0.12f, 0.18f, 0.9f);
                GameObject labelObject = new GameObject("HandLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(hand.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.text = "✋";
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 42f;
                label.color = Color.white;
                interaction.mobileInteractBtn = hand;
            }
        }
        if (interaction.mobileInteractBtn != null)
        {
            Button handButton = interaction.mobileInteractBtn.GetComponent<Button>();
            if (handButton != null)
            {
                handButton.onClick.RemoveAllListeners();
                handButton.onClick.AddListener(interaction.TriggerMobileInteraction);
            }
        }

        foreach (Evidence evidence in Object.FindObjectsByType<Evidence>(FindObjectsInactive.Include))
        {
            // Preserve each evidence item's authored role restriction. The
            // bootstrap must never turn role-locked evidence into Any.
            SphereCollider sphere = evidence.GetComponent<SphereCollider>();
            if (sphere == null) sphere = evidence.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(sphere.radius, 0.75f);

            if (evidence.name == "EV_01")
            {
                // EV_01 is manually calibrated in Case02_Main. Do not
                // overwrite its authored position/rotation/scale at runtime.
                evidence.gameObject.SetActive(true);
                foreach (MeshCollider meshCollider in evidence.GetComponentsInChildren<MeshCollider>(true))
                {
                    // Keep the visual mesh collider for physical contact, but
                    // do not force Unity to build an invalid convex hull.
                    meshCollider.convex = false;
                    meshCollider.isTrigger = false;
                }
                SphereCollider mugInteraction = evidence.GetComponent<SphereCollider>();
                if (mugInteraction == null) mugInteraction = evidence.gameObject.AddComponent<SphereCollider>();
                mugInteraction.center = new Vector3(0f, 0.35f, 0f);
                mugInteraction.radius = 0.75f;
                mugInteraction.isTrigger = true;
                foreach (Renderer renderer in evidence.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;
            }
            else if (evidence.name == "EV_03")
            {
                // Place the syringe visibly above the left analysis bench.
                evidence.transform.position = new Vector3(-1.6f, 0.86f, 39.2f);
                evidence.transform.localScale = Vector3.one * 0.22f;
            }
        }
    }

    private static TextMeshProUGUI CreateRuntimeLabel(Transform parent, string name, Vector2 anchor, float size)
    {
        Transform existing = parent.Find(name);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        if (rect == null) rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor; rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(1000f, 70f);

        // A GameObject cannot contain both Image and TextMeshProUGUI because
        // both inherit Graphic. Reuse an existing child text or create one.
        TextMeshProUGUI label = labelObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject textObject = new GameObject(name + "Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(labelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            label = textObject.GetComponent<TextMeshProUGUI>();
        }
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = size;
        label.color = Color.white;
        label.text = "";
        Image background = labelObject.GetComponent<Image>();
        if (background == null) background = labelObject.AddComponent<Image>();
        background.color = new Color(0.02f, 0.05f, 0.08f, 0.82f);
        background.raycastTarget = false;
        return label;
    }
}
