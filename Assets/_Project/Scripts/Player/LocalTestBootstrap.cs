using UnityEngine;
using UnityEngine.SceneManagement;

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
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Lobby") return;

        PlayerMovement move = FindLocalPlayerMovement();
        Debug.Log("[LocalTest] Scene: " + SceneManager.GetActiveScene().name +
                  " | PlayerMovement found: " + (move != null));

        if (move == null)
        {
            Debug.LogError("[LocalTest] No PlayerMovement in this scene! Add PlayerMovement to the player object (or the prefab is not spawned).");
            return;
        }

        GameObject player = move.gameObject;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[LocalTest] Ready. Player=" + player.name + " pos=" + player.transform.position);
    }

    private static PlayerMovement FindLocalPlayerMovement()
    {
        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerMovement player in players)
        {
            if (player != null && player.GetComponent<RemotePlayerReplica>() == null)
                return player;
        }

        return null;
    }
}
