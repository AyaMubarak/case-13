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

        // ضبط قياس زر/عنصر الدليل ليكون بحجم متناسق ومريح
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 62f);
        }

        Image itemBg = GetComponent<Image>();
        if (itemBg != null)
        {
            itemBg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        }

        if (nameText != null)
        {
            nameText.text = $"<b>{evidence.evidenceName}</b>";
            nameText.color = new Color(0.92f, 0.95f, 0.98f);
            nameText.fontSize = 20f;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            if (nameRt != null)
            {
                nameRt.anchorMin = new Vector2(0f, 0f);
                nameRt.anchorMax = new Vector2(1f, 1f);
                nameRt.offsetMin = new Vector2(18f, 0f);
                nameRt.offsetMax = new Vector2(-15f, 0f);
            }
        }

        if (typeText != null)
        {
            string col = evidence.type == EvidenceType.Physical ? "#FF5252" : (evidence.type == EvidenceType.Digital ? "#00E5FF" : "#FFD54F");
            typeText.text = $"<color={col}><b>[{evidence.type.ToString().ToUpper()}]</b></color>";
            typeText.fontSize = 14f;
        }

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