using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class EvidenceManager : MonoBehaviourPunCallbacks
{
    public static EvidenceManager Instance { get; private set; }

    public static event System.Action OnAllEvidenceCollected;

    [Header("Case Settings")]
    public int totalEvidence = 5;

    [Header("Audio Settings")]
    public AudioSource allEvidenceCollectedVoice; // كائن الصوت: "all-evidence-collected"

    [Header("Suspects Management")]
    [Tooltip("اسحبي كائن المتهم الأول هنا ليظهر عند الباب")]
    public GameObject firstSuspect;
    [Tooltip("اسحبي كائن المتهم الثاني هنا لنتأكد من بقائه مطفأ حتى يأتي دوره")]
    public GameObject secondSuspect;

    private List<Evidence> collectedEvidenceList = new List<Evidence>();
    private Dictionary<string, Evidence> allEvidenceMap = new Dictionary<string, Evidence>(System.StringComparer.OrdinalIgnoreCase);
    private bool isCaseComplete = false;

    private const string LAST_COLLECTOR_KEY = "LastCollector";
    private const string LAST_CLUE_KEY = "LastClue";

    private static string RoomEvidenceKey
    {
        get
        {
            bool case02 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Case02");
            return case02 ? "CollectedEvidenceIDs_Case02" : "CollectedEvidenceIDs_Case01";
        }
    }

    public int CollectedCount => collectedEvidenceList.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (allEvidenceCollectedVoice != null)
        {
            allEvidenceCollectedVoice.playOnAwake = false;
            allEvidenceCollectedVoice.loop = false;
            allEvidenceCollectedVoice.spatialBlend = 0f;
            allEvidenceCollectedVoice.volume = 1f;
            allEvidenceCollectedVoice.Stop();
        }

        if (firstSuspect != null) firstSuspect.SetActive(false);
        if (secondSuspect != null) secondSuspect.SetActive(false);

        CacheSceneEvidence();
    }

    private void Start()
    {
        CacheSceneEvidence();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEvidenceCount(collectedEvidenceList.Count, totalEvidence);
        }

        // مزامنة حالة الأدلة في حال كان اللاعب متصلاً بغرفة مسبقاً
        if (PhotonNetwork.InRoom)
        {
            SyncCollectedEvidenceFromRoom();
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        CacheSceneEvidence();
        if (PhotonNetwork.InRoom)
        {
            SyncCollectedEvidenceFromRoom();
        }
    }

    private void CacheSceneEvidence()
    {
        Evidence[] sceneEvidences = FindObjectsByType<Evidence>(FindObjectsInactive.Include);
        foreach (Evidence ev in sceneEvidences)
        {
            if (ev != null && !string.IsNullOrEmpty(ev.evidenceID))
            {
                if (!allEvidenceMap.ContainsKey(ev.evidenceID))
                {
                    allEvidenceMap.Add(ev.evidenceID, ev);
                }
            }
        }
    }

    public void CollectEvidence(Evidence evidence)
    {
        if (evidence == null) return;

        // 1. مزامنة الدليل مع شبكة Photon إذا كنا في غرفة
        if (PhotonNetwork.InRoom)
        {
            List<string> currentIDs = GetRoomCollectedEvidenceIDs();
            if (!currentIDs.Contains(evidence.evidenceID))
            {
                currentIDs.Add(evidence.evidenceID);

                string collectorName = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "A DETECTIVE" : PhotonNetwork.NickName;

                Hashtable props = new Hashtable
                {
                    { RoomEvidenceKey, currentIDs.ToArray() },
                    { LAST_COLLECTOR_KEY, collectorName },
                    { LAST_CLUE_KEY, evidence.evidenceName }
                };

                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }

        // 2. معالجة الدليل محلياً
        ApplyEvidenceLocally(evidence, PhotonNetwork.NickName, true);
    }

    // كولباك استلام تحديثات الغرفة من شبكة Photon لجميع اللاعبين الأربعة
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(RoomEvidenceKey))
        {
            string collector = propertiesThatChanged.ContainsKey(LAST_COLLECTOR_KEY) && propertiesThatChanged[LAST_COLLECTOR_KEY] != null
                ? propertiesThatChanged[LAST_COLLECTOR_KEY].ToString()
                : "A DETECTIVE";

            string clueName = propertiesThatChanged.ContainsKey(LAST_CLUE_KEY) && propertiesThatChanged[LAST_CLUE_KEY] != null
                ? propertiesThatChanged[LAST_CLUE_KEY].ToString()
                : "";

            SyncCollectedEvidenceFromRoom(collector, clueName);
        }
    }

    private List<string> GetRoomCollectedEvidenceIDs()
    {
        List<string> list = new List<string>();
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return list;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomEvidenceKey, out object val) && val is string[] ids)
        {
            list.AddRange(ids);
        }

        return list;
    }

    public void ForceSyncFromRoom(bool showSyncBanner = false)
    {
        CacheSceneEvidence();
        int beforeCount = collectedEvidenceList.Count;
        SyncCollectedEvidenceFromRoom();
        int afterCount = collectedEvidenceList.Count;

        if (showSyncBanner && UIManager.Instance != null && afterCount > beforeCount)
        {
            UIManager.Instance.ShowOnScreenNotification($"SQUAD DATA UPDATED // SYNCED {afterCount}/{totalEvidence} CLUES", 4.5f);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ForceSyncFromRoom(true);
    }

    private void SyncCollectedEvidenceFromRoom(string collectorName = null, string latestClueName = null)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        List<string> roomIDs = GetRoomCollectedEvidenceIDs();
        bool newlyAddedAny = false;

        foreach (string id in roomIDs)
        {
            if (allEvidenceMap.TryGetValue(id, out Evidence ev) && ev != null)
            {
                if (!collectedEvidenceList.Contains(ev))
                {
                    ApplyEvidenceLocally(ev, collectorName, false);
                    newlyAddedAny = true;
                }
                else
                {
                    // التأكد من إخفاء المجسم من الغرفة لجميع اللاعبين
                    if (ev.gameObject.activeSelf)
                    {
                        ev.gameObject.SetActive(false);
                    }
                }
            }
        }

        if (newlyAddedAny && !string.IsNullOrEmpty(latestClueName) && UIManager.Instance != null)
        {
            string displayName = string.IsNullOrEmpty(collectorName) ? "TEAM" : collectorName.ToUpperInvariant();
            UIManager.Instance.ShowOnScreenNotification($"{displayName} SECURED CLUE: {latestClueName.ToUpper()}");
        }
    }

    private void ApplyEvidenceLocally(Evidence evidence, string collectorName, bool showLocalNotification)
    {
        if (collectedEvidenceList.Contains(evidence))
            return;

        collectedEvidenceList.Add(evidence);

        // إخفاء كائن الدليل من المشهد لكي لا يظهر مجدداً
        if (evidence.gameObject.activeSelf)
        {
            evidence.gameObject.SetActive(false);
        }

        int currentCount = collectedEvidenceList.Count;

        // تحديث ملف استبعاد المشتبه بهم (Case Dossier)
        if (CaseDossierManager.Instance != null)
        {
            CaseDossierManager.Instance.ProcessEvidenceCollected(evidence);
        }

        // تحديث واجهة المستخدم (العداد والحقيبة) لجميع اللاعبين
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEvidenceCount(currentCount, totalEvidence);
            UIManager.Instance.UnlockInventoryAccess();

            if (showLocalNotification)
            {
                if (currentCount == 1)
                {
                    UIManager.Instance.ShowOnScreenNotification("FIRST CLUE SECURED! OPEN INVENTORY TO VIEW");
                }
                else
                {
                    UIManager.Instance.ShowOnScreenNotification($"CLUE SECURED: {evidence.evidenceName.ToUpper()}");
                }
            }

            // تحديث محتويات الحقيبة لو كانت مفتوحة حالياً
            UIManager.Instance.RefreshEvidenceList();
        }

        // فحص اكتمال جميع الأدلة
        if (currentCount >= totalEvidence && !isCaseComplete)
        {
            TriggerCaseCompletion();
        }
    }

    private void TriggerCaseCompletion()
    {
        isCaseComplete = true;
        Debug.Log("<color=green>[CASE 13]</color> ALL EVIDENCE COLLECTED!");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowOnScreenNotification("ALL EVIDENCE COLLECTED // PROCEED TO DEDUCTION");
        }

        if (allEvidenceCollectedVoice != null)
        {
            allEvidenceCollectedVoice.gameObject.SetActive(true);
            allEvidenceCollectedVoice.enabled = true;
            if (!allEvidenceCollectedVoice.isPlaying)
            {
                allEvidenceCollectedVoice.Play();
            }
        }

        if (firstSuspect != null)
        {
            firstSuspect.SetActive(true);
            Debug.Log("<color=yellow>[INTERROGATION]</color> First suspect activated at the door!");
        }

        OnAllEvidenceCollected?.Invoke();
    }

    public List<Evidence> GetCollectedEvidence()
    {
        return collectedEvidenceList;
    }

    public bool IsCaseComplete()
    {
        return isCaseComplete;
    }
}
