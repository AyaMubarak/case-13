using Unity.Netcode;
using UnityEngine;

public class GameMatchManager : NetworkBehaviour
{
    public static GameMatchManager Instance;

    [SerializeField] private float totalMatchTime = 1800f;
    private float remainingTime;

    [SerializeField] private SuspectManager suspectManager;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        remainingTime = totalMatchTime;

        if (suspectManager != null)
        {
            // Hide suspects initially. SeparateInterrogationManager will handle showing them one by one.
            suspectManager.SetSuspectCount(0);
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                EndMatch();
            }
        }
    }

    private void EndMatch()
    {
        Debug.Log("Match ended after 30 minutes.");
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
