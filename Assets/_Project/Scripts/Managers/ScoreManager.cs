using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; } = 0;

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
        CurrentScore += amount;
        Debug.Log($"<color=yellow>[SCORE]</color> Added {amount} points. Total: {CurrentScore}");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification($"+{amount} POINTS ACQUIRED!");
        }
    }

    public void ResetScore()
    {
        CurrentScore = 0;
    }
}
