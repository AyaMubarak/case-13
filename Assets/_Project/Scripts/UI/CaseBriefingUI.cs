using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CaseBriefingUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject briefingPanel;

    [Header("Text Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI storyText;

    [Header("Buttons")]
    public GameObject continueButton;

    [Header("Story Settings")]
    public string caseTitle = "CASE 13: THE MIDNIGHT HEIST";

    [TextArea(5, 10)]
    public string backStory =
        "A high-tech digital keycard was stolen from the central vault at exactly 02:15 AM. " +
        "We have two suspects in custody, both employees with high security clearance. " +
        "Your job is to interrogate them, find the contradictions in their alibis using evidence, " +
        "and identify the true culprit.";

    public float typingSpeed = 0.05f;

    private void Start()
    {
        // إظهار لوحة الـ Briefing
        if (briefingPanel != null)
        {
            briefingPanel.SetActive(true);
        }

        // إخفاء زر Continue أثناء الكتابة
        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        // وضع عنوان القضية
        if (titleText != null)
        {
            titleText.text = caseTitle;
        }

        // فتح الماوس
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // بدء الكتابة التدريجية
        if (storyText != null)
        {
            StartCoroutine(TypewriterEffect(backStory));
        }
        else
        {
            // إذا لم يوجد Story Text، أظهر الزر مباشرة
            if (continueButton != null)
            {
                continueButton.SetActive(true);
            }
        }
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        storyText.text = "";

        foreach (char c in fullText)
        {
            storyText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        // إظهار زر Continue بعد انتهاء النص
        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    // هذه الدالة يتم ربطها بزر Continue
    public void StartInvestigation()
    {
        // إخفاء لوحة الـ Briefing
        if (briefingPanel != null)
        {
            briefingPanel.SetActive(false);
        }

        // إغلاق الماوس استعداداً للعب
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[CASE] Briefing completed. Investigation started.");
    }
}