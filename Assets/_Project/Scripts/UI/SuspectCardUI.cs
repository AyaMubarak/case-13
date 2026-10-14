using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SuspectCardUI : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject eliminatedStamp;
    [SerializeField] private Button cardButton;

    private SuspectProfile profile;
    private CaseDossierManager manager;

    public void Setup(SuspectProfile data, CaseDossierManager mgr)
    {
        profile = data;
        manager = mgr;

        if (nameText != null)
        {
            nameText.text = data.suspectName;
            nameText.fontSize = 20f;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = data.isEliminated ? new Color(0.85f, 0.3f, 0.3f, 0.9f) : new Color(0.95f, 0.95f, 0.95f, 1f);
        }

        if (portraitImage != null && data.portrait != null)
        {
            portraitImage.sprite = data.portrait;
            portraitImage.preserveAspect = true;
            portraitImage.color = data.isEliminated ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : Color.white;
        }

        Image cardBg = GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = data.isEliminated 
                ? new Color(0.12f, 0.05f, 0.05f, 0.85f) 
                : new Color(0.08f, 0.11f, 0.16f, 0.92f);
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => manager.OpenSuspectDetails(profile));
        }

        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (eliminatedStamp != null && profile != null)
        {
            eliminatedStamp.SetActive(profile.isEliminated);
        }

        if (nameText != null && profile != null)
        {
            nameText.color = profile.isEliminated ? new Color(0.85f, 0.3f, 0.3f, 0.9f) : new Color(0.95f, 0.95f, 0.95f, 1f);
        }

        if (portraitImage != null && profile != null)
        {
            portraitImage.color = profile.isEliminated ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : Color.white;
        }

        Image cardBg = GetComponent<Image>();
        if (cardBg != null && profile != null)
        {
            cardBg.color = profile.isEliminated 
                ? new Color(0.12f, 0.05f, 0.05f, 0.85f) 
                : new Color(0.08f, 0.11f, 0.16f, 0.92f);
        }
    }
}
