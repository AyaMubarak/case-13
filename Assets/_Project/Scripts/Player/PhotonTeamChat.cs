using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, collapsible team chat for the current Photon room.</summary>
public class PhotonTeamChat : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte ChatEventCode = 90;
    private GameObject panel;
    private GameObject messagesObject;
    private TMP_InputField input;
    private TextMeshProUGUI messages;
    private bool open;

    private void Awake()
    {
        BuildUI();
        SetOpen(false);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    private void Update()
    {
        if (!open || input == null) return;
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(input.text)) SendMessageToTeam();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ChatEventCode) return;
        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length < 2) return;
        AddMessage(data[0]?.ToString() ?? "DETECTIVE", data[1]?.ToString() ?? "");
    }

    public void ToggleChat() => SetOpen(!open);

    public void SendMessageToTeam()
    {
        if (!PhotonNetwork.InRoom || input == null) return;
        string text = input.text.Trim();
        if (text.Length == 0) return;
        if (text.Length > 160) text = text.Substring(0, 160);

        string sender = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "DETECTIVE" : PhotonNetwork.NickName;
        AddMessage(sender, text);
        PhotonNetwork.RaiseEvent(ChatEventCode, new object[] { sender, text },
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
        input.text = string.Empty;
        input.ActivateInputField();
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (panel != null) panel.SetActive(value);
    }

    private void AddMessage(string sender, string text)
    {
        if (messages == null) return;
        messages.text += (messages.text.Length == 0 ? "" : "\n") + $"<b>{Escape(sender)}:</b> {Escape(text)}";
        messages.text = messages.text.Length > 1800 ? messages.text.Substring(messages.text.Length - 1800) : messages.text;
    }

    private static string Escape(string value) =>
        (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private void BuildUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject toggle = CreateUI("ChatToggle", canvas.transform);
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.02f, 0.78f); toggleRect.anchorMax = new Vector2(0.12f, 0.86f);
        Image toggleImage = toggle.GetComponent<Image>(); toggleImage.color = new Color(0.03f, 0.06f, 0.12f, 0.85f);
        Button toggleButton = toggle.AddComponent<Button>(); toggleButton.onClick.AddListener(ToggleChat);
        TextMeshProUGUI toggleText = CreateText("CHAT", toggle.transform, 18, Color.white);

        panel = CreateUI("TeamChatPanel", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.48f); panelRect.anchorMax = new Vector2(0.34f, 0.78f);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.94f);

        messagesObject = CreateUI("Messages", panel.transform);
        RectTransform messagesRect = messagesObject.GetComponent<RectTransform>();
        messagesRect.anchorMin = new Vector2(0.04f, 0.25f); messagesRect.anchorMax = new Vector2(0.96f, 0.94f);
        messages = messagesObject.AddComponent<TextMeshProUGUI>(); messages.fontSize = 15; messages.color = Color.white;
        messages.textWrappingMode = TextWrappingModes.Normal;

        GameObject inputObject = CreateUI("Input", panel.transform);
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.04f, 0.05f); inputRect.anchorMax = new Vector2(0.74f, 0.19f);
        inputObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        input = inputObject.AddComponent<TMP_InputField>();
        input.textComponent = CreateText("Text", inputObject.transform, 14, Color.white);
        TextMeshProUGUI placeholder = CreateText("Placeholder", inputObject.transform, 14, new Color(1, 1, 1, .5f));
        input.placeholder = placeholder;
        placeholder.text = "Type message...";

        GameObject send = CreateUI("Send", panel.transform);
        RectTransform sendRect = send.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(0.77f, 0.05f); sendRect.anchorMax = new Vector2(0.96f, 0.19f);
        send.GetComponent<Image>().color = new Color(0f, .65f, 1f, .85f);
        send.AddComponent<Button>().onClick.AddListener(SendMessageToTeam);
        CreateText("SEND", send.transform, 13, Color.white);
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false); return obj;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 3); rect.offsetMax = new Vector2(-8, -3);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>(); text.fontSize = size; text.color = color;
        text.alignment = TextAlignmentOptions.Center; return text;
    }
}
