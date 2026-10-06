using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public Button selectButton;

    private Evidence boundEvidence;

    public void Setup(Evidence evidence)
    {
        boundEvidence = evidence;
        if (nameText != null) nameText.text = evidence.evidenceName;
        if (typeText != null) typeText.text = $"[{evidence.type}]";

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        if (UIManager.Instance != null && boundEvidence != null)
        {
            UIManager.Instance.ShowEvidenceDetails(boundEvidence);
        }
    }
}