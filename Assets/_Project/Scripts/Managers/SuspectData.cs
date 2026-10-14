using UnityEngine;

[System.Serializable]
public class SuspectProfile
{
    public string suspectID;
    public string suspectName;
    public string roleOrTitle;
    public Sprite portrait;
    public string bloodType;
    public string alibi;
    public bool isEliminated = false;
    [TextArea(2, 3)]
    public string eliminationReason = "";
}
