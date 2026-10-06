using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    public static event System.Action OnAllEvidenceCollected;

    [Header("Case Settings")]
    public int totalEvidence = 5;

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