using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SuspectRecord
{
    public string suspectName;
    [TextArea(3, 5)]
    public string statementText;
    public Sprite suspectPortrait;
}

public class InterrogationManager : MonoBehaviour
{
    public static InterrogationManager Instance { get; private set; }

    [Header("All Suspects Master List")]
    public List<SuspectRecord> registeredSuspects = new List<SuspectRecord>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RecordOrUpdateStatement(string name, string text, Sprite portrait)
    {
        SuspectRecord existing = registeredSuspects.Find(s => s.suspectName == name);
        if (existing != null)
        {
            existing.statementText = text;
            if (portrait != null) existing.suspectPortrait = portrait;
        }
        else
        {
            SuspectRecord newRecord = new SuspectRecord
            {
                suspectName = name,
                statementText = text,
                suspectPortrait = portrait
            };
            registeredSuspects.Add(newRecord);
        }

        Debug.Log($"<color=yellow>[INTERROGATION LOG]</color> Updated records for: {name}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"CASE FILE UPDATED: {name.ToUpper()}");
        }
    }

    public List<SuspectRecord> GetAllRecords()
    {
        return registeredSuspects;
    }
}