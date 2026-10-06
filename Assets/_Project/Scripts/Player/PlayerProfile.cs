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

    private void Awake()
    {
        if (LocalPlayer != null && LocalPlayer != this)
        {
            Destroy(gameObject);
            return;
        }
        LocalPlayer = this;
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