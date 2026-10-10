using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerSetupUI : MonoBehaviour
{
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private Dropdown genderDropdown;
    [SerializeField] private Dropdown roleDropdown;
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        confirmButton.onClick.AddListener(ConfirmSelection);
    }

    private void ConfirmSelection()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager not found");
            return;
        }

        if (LobbyManager.Instance == null)
        {
            Debug.LogError("LobbyManager not found");
            return;
        }

        string playerName = playerNameInput.text;
        string gender = genderDropdown.options[genderDropdown.value].text;
        PlayerRole role = (PlayerRole)roleDropdown.value;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.Log("Player name cannot be empty");
            return;
        }

        SendRoleSelectionServerRpc(playerName, gender, (int)role);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendRoleSelectionServerRpc(string playerName, string gender, int roleIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        PlayerRole role = (PlayerRole)roleIndex;

        bool success = LobbyManager.Instance.TryAssignPlayer(clientId, playerName, gender, role);

        if (success)
        {
            PlayerProfileData profile = LobbyManager.Instance.GetProfileByClient(clientId);
            ReplyAcceptedClientRpc(profile.ClientId, profile.PlayerName, profile.Gender, (int)profile.Role);
        }
        else
        {
            ReplyRejectedClientRpc(clientId, "Name or role is already taken.");
        }
    }

    [ClientRpc]
    private void ReplyAcceptedClientRpc(ulong clientId, string playerName, string gender, int roleIndex)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        PlayerProfileData profile = new PlayerProfileData(clientId, playerName, gender, (PlayerRole)roleIndex);

        NetworkPlayerController playerController = GetComponent<NetworkPlayerController>();
        if (playerController != null)
        {
            playerController.SetLocalProfile(profile);
        }
    }

    [ClientRpc]
    private void ReplyRejectedClientRpc(ulong clientId, string message)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        Debug.Log("Rejected: " + message);
    }
}
