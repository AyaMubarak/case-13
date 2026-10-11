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

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            OnProfileChanged?.Invoke();
        }
    }
}