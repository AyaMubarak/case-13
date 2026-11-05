using System;
using UnityEngine;

public enum DetectiveGender
{
    Male,
    Female
}

public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile LocalPlayer { get; private set; }
    public static PlayerProfile LocalInstance => LocalPlayer;
    public DetectiveRole CurrentRole => currentRole;

    /// <summary>Used briefly while creating visual replicas of other Photon players.</summary>
    public static bool SuppressLocalRegistration { get; set; }

    public event Action OnProfileChanged;

    [Header("Agent Identity")]
    public string agentCodename = "Vanguard-1";
    public DetectiveGender gender = DetectiveGender.Female;
    public DetectiveRole currentRole = DetectiveRole.ForensicsExpert;

    [Header("Local Testing (تيست محلي)")]
    [Tooltip("ON: use the Inspector values above and ignore the lobby. OFF: take role/gender from the lobby.")]
    public bool useLocalTestOverride = false;

    private void Awake()
    {
        if (SuppressLocalRegistration)
            return;

        if (LocalPlayer != null && LocalPlayer != this)
        {
            Destroy(gameObject);
            return;
        }
        LocalPlayer = this;
    }

    private void Start()
    {
        if (useLocalTestOverride)
        {
            Debug.Log($"[PlayerProfile] LOCAL TEST override: {currentRole} / {gender}");
            OnProfileChanged?.Invoke();
            return;
        }
        LoadFromLobby();
    }

    /// <summary>Reads role/gender/name from Photon (set in the lobby), falling back to PlayerPrefs.</summary>
    public void LoadFromLobby()
    {
        string roleStr = null, genderStr = null, nameStr = null;

        if (Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.LocalPlayer != null)
        {
            var props = Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties;
            if (props.ContainsKey("Role")) roleStr = props["Role"].ToString();
            if (props.ContainsKey("Gender")) genderStr = props["Gender"].ToString();
            nameStr = Photon.Pun.PhotonNetwork.NickName;
        }

        if (string.IsNullOrEmpty(roleStr)) roleStr = PlayerPrefs.GetString("Player_Role", "Field Detective");
        if (string.IsNullOrEmpty(genderStr)) genderStr = PlayerPrefs.GetString("Player_Gender", "Male");
        if (string.IsNullOrEmpty(nameStr)) nameStr = PlayerPrefs.GetString("Player_Name", agentCodename);

        agentCodename = nameStr;
        gender = genderStr == "Female" ? DetectiveGender.Female : DetectiveGender.Male;
        currentRole = ParseRole(roleStr);

        Debug.Log($"[PlayerProfile] From lobby: {currentRole} / {gender} / {agentCodename}");
        OnProfileChanged?.Invoke();
    }

    private static DetectiveRole ParseRole(string s)
    {
        switch (s)
        {
            case "Forensics Expert": return DetectiveRole.ForensicsExpert;
            case "Digital Analyst": return DetectiveRole.DigitalAnalyst;
            case "Interrogator": return DetectiveRole.Interrogator;
            default: return DetectiveRole.FieldDetective;
        }
    }

    public void SetIdentity(string codename, DetectiveGender newGender, DetectiveRole newRole)
    {
        agentCodename = codename;
        gender = newGender;
        currentRole = newRole;
        OnProfileChanged?.Invoke();
    }

    public void SetGender(DetectiveGender newGender)
    {
        gender = newGender;
        OnProfileChanged?.Invoke();
    }

    public void SetRole(DetectiveRole newRole)
    {
        currentRole = newRole;
        OnProfileChanged?.Invoke();
    }

#if UNITY_EDITOR
    private static System.Collections.Generic.List<GameObject> spawnedEditorTestSquad = new System.Collections.Generic.List<GameObject>();

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    OnProfileChanged?.Invoke();
                }
            };
        }
    }

    [ContextMenu("★ [TEST] Spawn All 8 Characters In Front Of Me (4 Roles x Male/Female)")]
    public void EditorSpawnTestSquad()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Squad Test] Please enter Play Mode first, then click this button!");
            return;
        }

        EditorClearTestSquad();

        // Ensure local player's own mesh is also visible in 3D if testing in Scene view
        var selector = GetComponent<CharacterVisualSelector>();
        if (selector != null && selector.CurrentActiveModel != null)
        {
            foreach (var r in selector.CurrentActiveModel.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        string[] roles = { "Field Detective", "Forensics Expert", "Digital Analyst", "Interrogator" };
        string[] genders = { "Male", "Female" };

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        int index = 0;
        for (int g = 0; g < genders.Length; g++)
        {
            for (int r = 0; r < roles.Length; r++)
            {
                // Arrange in 2 rows in front of the player: row 1 (Male 4 roles), row 2 (Female 4 roles)
                float rowDist = 2.6f + (g * 1.8f);
                float colOffset = (r - 1.5f) * 1.25f;
                Vector3 spawnPos = transform.position + (forward * rowDist) + (right * colOffset);
                Quaternion spawnRot = Quaternion.LookRotation(-forward, Vector3.up);

                SuppressLocalRegistration = true;
                GameObject clone = Instantiate(gameObject);
                SuppressLocalRegistration = false;

                foreach (Camera cam in clone.GetComponentsInChildren<Camera>(true)) DestroyImmediate(cam);
                foreach (AudioListener al in clone.GetComponentsInChildren<AudioListener>(true)) DestroyImmediate(al);
                foreach (CameraFollow cf in clone.GetComponentsInChildren<CameraFollow>(true)) DestroyImmediate(cf);

                PlayerMovement pm = clone.GetComponent<PlayerMovement>();
                if (pm != null) DestroyImmediate(pm);

                PlayerInteraction pi = clone.GetComponent<PlayerInteraction>();
                if (pi != null) DestroyImmediate(pi);

                foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true))
                    body.isKinematic = true;

                foreach (Collider col in clone.GetComponentsInChildren<Collider>(true))
                    col.isTrigger = true;

                RemotePlayerReplica replica = clone.AddComponent<RemotePlayerReplica>();
                string labelName = $"{genders[g]}_{roles[r].Split(' ')[0]}";
                replica.Initialize(spawnPos, spawnRot, labelName, roles[r], genders[g]);

                spawnedEditorTestSquad.Add(clone);
                index++;
            }
        }

        Debug.Log($"<color=green>[Squad Test] Spawned all {index} characters (4 Roles x Male/Female) right in front of you!</color>");
    }

    [ContextMenu("★ [TEST] Toggle Walk Animation On Spawned Characters")]
    public void EditorToggleSquadWalkAnimation()
    {
        if (!Application.isPlaying || spawnedEditorTestSquad.Count == 0) return;

        foreach (GameObject clone in spawnedEditorTestSquad)
        {
            if (clone == null) continue;
            RemotePlayerReplica rep = clone.GetComponent<RemotePlayerReplica>();
            if (rep != null)
            {
                rep.ApplyNetworkState(clone.transform.position, clone.transform.rotation, 1.5f);
            }
        }
        Debug.Log("<color=cyan>[Squad Test] Activated Walk Animation on all spawned characters!</color>");
    }

    [ContextMenu("★ [TEST] Clear Spawned Test Characters")]
    public void EditorClearTestSquad()
    {
        foreach (GameObject clone in spawnedEditorTestSquad)
        {
            if (clone != null) Destroy(clone);
        }
        spawnedEditorTestSquad.Clear();
    }
#endif
}
