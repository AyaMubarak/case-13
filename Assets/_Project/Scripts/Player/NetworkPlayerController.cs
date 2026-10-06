using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private RoleUIController roleUIController;

    private PlayerProfileData localProfile;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Local player spawned.");
        }
    }

    public void SetLocalProfile(PlayerProfileData profile)
    {
        localProfile = profile;

        if (IsOwner)
        {
            roleUIController.ShowRoleUI(profile.Role);
            Debug.Log("My role is: " + profile.Role);
        }
    }

    public PlayerProfileData GetLocalProfile()
    {
        return localProfile;
    }
}
