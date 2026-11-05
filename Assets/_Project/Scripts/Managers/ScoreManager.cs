using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public static ScoreManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject scoreObject = new GameObject("ScoreManager");
        return scoreObject.AddComponent<ScoreManager>();
    }

    public int CurrentScore { get; private set; } = 500; // يبدأ المحقق برصيد 500 نقطة كاملة لتقييم الرانك S
    public int TotalMistakesCount { get; private set; } = 0;

    // سجل الأخطاء لعرضها بالتفصيل في تقرير الرانك النهائي
    public List<string> MistakeLogs { get; private set; } = new List<string>();

    private const string CoopMistakesKey = "CaseMistakes";
    private const string CoopScoreKey = "CaseScore";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // الحفاظ على السكور بين المشاهد
    }

    public void AddScore(int amount)
    {
        if (Instance == null) Instance = this;
        CurrentScore += amount;
        Debug.Log($"<color=yellow>[SCORE]</color> Added {amount} points. Total: {CurrentScore}");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"+{amount} BONUS INVESTIGATION PTS!");
        }
        PublishCoopStats();
    }

    public void DeductScoreForMistake(int penaltyAmount, string mistakeContext)
    {
        if (Instance == null) Instance = this;
        CurrentScore = Mathf.Max(0, CurrentScore - penaltyAmount);
        TotalMistakesCount++;
        
        string logEntry = $"• {mistakeContext} (-{penaltyAmount} PTS)";
        MistakeLogs.Add(logEntry);

        Debug.LogWarning($"<color=red>[PENALTY]</color> Deducted {penaltyAmount} pts. New Score: {CurrentScore} | Error: {mistakeContext}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"<color=#FF5252>WRONG INQUIRY (-{penaltyAmount} PTS) // RANK AFFECTED</color>", 2.8f);
        }
        PublishCoopStats();
    }

    public string GetCurrentRankBadge(out string rankTitle, out string rankColor)
    {
        // حساب الرانك المباشر بناءً على النقاط وعدد الأخطاء
        if (CurrentScore >= 450 && TotalMistakesCount == 0)
        {
            rankTitle = "MASTER DETECTIVE (FLAWLESS)";
            rankColor = "#FFD700"; // ذهبي فخم
            return "S";
        }
        else if (CurrentScore >= 350 && TotalMistakesCount <= 2)
        {
            rankTitle = "SENIOR INVESTIGATOR";
            rankColor = "#00E5FF"; // سيان
            return "A";
        }
        else if (CurrentScore >= 200 && TotalMistakesCount <= 4)
        {
            rankTitle = "FIELD AGENT";
            rankColor = "#69F0AE"; // أخضر
            return "B";
        }
        else
        {
            rankTitle = "ROOKIE OFFICER";
            rankColor = "#B0BEC5"; // رمادي
            return "C";
        }
    }

    public void ResetScore()
    {
        CurrentScore = 500;
        TotalMistakesCount = 0;
        MistakeLogs.Clear();
        PublishCoopStats();
    }

    private void PublishCoopStats()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { CoopScoreKey, CurrentScore },
            { CoopMistakesKey, TotalMistakesCount }
        });
    }

    public static void GetCoopTotals(out int score, out int mistakes)
    {
        score = 500;
        mistakes = 0;
        if (!PhotonNetwork.InRoom) return;

        // The team starts with one 500-point case score; each player's
        // incorrect answers contribute to the same shared total.
        score = 500;
        mistakes = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(CoopMistakesKey, out object mistakeValue))
                mistakes += ToInt(mistakeValue);
        }
        score = Mathf.Max(0, 500 - mistakes * 75);
    }

    private static int ToInt(object value)
    {
        if (value is int intValue) return intValue;
        return int.TryParse(value != null ? value.ToString() : "0", out int parsed) ? parsed : 0;
    }
}
