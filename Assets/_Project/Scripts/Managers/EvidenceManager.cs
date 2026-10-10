using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
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
    private bool isCaseComplete = false;

    // الخاصية المطلوبة لعد الأدلة
    public int CollectedCount => collectedEvidenceList.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // إيقاف التشغيل التلقائي وضبط إعدادات الصوت احتياطياً
        if (allEvidenceCollectedVoice != null)
        {
            allEvidenceCollectedVoice.playOnAwake = false;
            allEvidenceCollectedVoice.loop = false;
            allEvidenceCollectedVoice.spatialBlend = 0f; // ليكون الصوت 2D ومسموعاً في كل الغرفة
            allEvidenceCollectedVoice.volume = 1f;
            allEvidenceCollectedVoice.Stop();
        }

        // التأكد من أن المتهمين معطلان تماماً في بداية اللعبة
        if (firstSuspect != null)
        {
            firstSuspect.SetActive(false);
        }

        if (secondSuspect != null)
        {
            secondSuspect.SetActive(false);
        }
    }

    private void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEvidenceCount(0, totalEvidence);
        }
    }

    public void CollectEvidence(Evidence evidence)
    {
        if (collectedEvidenceList.Contains(evidence))
            return;

        collectedEvidenceList.Add(evidence);

        int currentCount = collectedEvidenceList.Count;

        // تحديث العداد على الشاشة
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEvidenceCount(currentCount, totalEvidence);

            // تفعيل الحقيبة وتنبيه اللاعب عند جمع أول دليل
            if (currentCount == 1)
            {
                UIManager.Instance.UnlockInventoryAccess();
                UIManager.Instance.ShowOnScreenNotification("FIRST CLUE SECURED! PRESS [TAB] OR OPEN INVENTORY TO VIEW");
            }
            else
            {
                UIManager.Instance.ShowOnScreenNotification($"CLUE SECURED: {evidence.evidenceName.ToUpper()}");
            }
        }

        // فحص شرط اكتمال جميع الأدلة
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

        // تشغيل صوت اكتمال الأدلة
        if (allEvidenceCollectedVoice != null)
        {
            allEvidenceCollectedVoice.gameObject.SetActive(true);
            allEvidenceCollectedVoice.enabled = true;
            if (!allEvidenceCollectedVoice.isPlaying)
            {
                allEvidenceCollectedVoice.Play();
            }
        }

        // تفعيل المتهم الأول فوراً ليظهر عند الباب ويبدأ التحقيق معه
        if (firstSuspect != null)
        {
            firstSuspect.SetActive(true);
            Debug.Log("<color=yellow>[INTERROGATION]</color> First suspect activated at the door!");
        }

        // إطلاق الحدث العام لأي سكربتات أخرى تستمع إليه
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