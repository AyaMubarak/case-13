using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    public const int MaxPlayers = 4;
    public const int InitialSuspects = 2;

    private readonly Dictionary<ulong, PlayerProfileData> profiles = new();
    private readonly Dictionary<PlayerRole, ulong> roleOwners = new();
    private bool gameStarted;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!profiles.ContainsKey(clientId))
        {
            profiles[clientId] = new PlayerProfileData(clientId, string.Empty, string.Empty, PlayerRole.Detective);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (profiles.TryGetValue(clientId, out var profile))
        {
            if (profile.Role != PlayerRole.Detective)
            {
                roleOwners.Remove(profile.Role);
            }

            profiles.Remove(clientId);
        }

        StartGameIfReady();
    }

    public bool TryAssignPlayer(ulong clientId, string name, string gender, PlayerRole role)
    {
        if (!IsServer)
            return false;

        if (profiles.Count >= MaxPlayers)
            return false;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        string cleanName = name.Trim();

        foreach (var profile in profiles.Values)
        {
            if (profile.ClientId == clientId)
                continue;

            if (profile.PlayerName.Equals(cleanName, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (roleOwners.ContainsKey(role) && roleOwners[role] != clientId)
            return false;

        if (profiles.TryGetValue(clientId, out var oldProfile))
        {
            if (oldProfile.Role != PlayerRole.Detective)
                roleOwners.Remove(oldProfile.Role);
        }

        profiles[clientId] = new PlayerProfileData(clientId, cleanName, gender, role);
        roleOwners[role] = clientId;

        Debug.Log("Assigned: " + cleanName + " -> " + role);

        StartGameIfReady();
        return true;
    }

    public PlayerProfileData GetProfileByClient(ulong clientId)
    {
        if (profiles.TryGetValue(clientId, out var profile))
            return profile;

        return new PlayerProfileData(clientId, string.Empty, string.Empty, PlayerRole.Detective);
    }

    public bool CanStartGame()
    {
        return profiles.Count == MaxPlayers;
    }

    public void StartGameIfReady()
    {
        if (!IsServer)
            return;

        if (gameStarted)
            return;

        if (CanStartGame())
        {
            gameStarted = true;
            StartGameClientRpc();
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Match started. 4 players are ready.");
    }
}
