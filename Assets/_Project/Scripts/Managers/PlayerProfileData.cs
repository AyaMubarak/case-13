using System;
using Unity.Netcode;

[Serializable]
public class PlayerProfileData : INetworkSerializable
{
    public ulong ClientId;
    public string PlayerName;
    public string Gender;
    public PlayerRole Role;
    public bool IsReady;
    public bool IsAlive;

    public PlayerProfileData() { }

    public PlayerProfileData(ulong clientId, string playerName, string gender, PlayerRole role)
    {
        ClientId = clientId;
        PlayerName = playerName;
        Gender = gender;
        Role = role;
        IsReady = false;
        IsAlive = true;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Gender);
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref IsAlive);
    }
}
