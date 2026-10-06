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
    public GameObject continueButton; // زر المتابعة
    
    [Header("Story Settings")]
    public string caseTitle = "CASE 13: THE MIDNIGHT HEIST";
    [TextArea(5, 10)]
    public string backStory = "A high-tech digital keycard was stolen from the central vault at exactly 02:15 AM. We have two suspects in custody, both employees with high security clearance. Your job is to interrogate them, find the contradictions in their alibis using evidence, and identify the true culprit.";
    
    public float typingSpeed = 0.05f;

    private void Start()
    {
        // عرض اللوحة عند بداية المشهد
        if (briefingPanel != null)
        {
            briefingPanel.SetActive(true);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false); // إخفاء الزر حتى تنتهي الكتابة
        }

        if (titleText != null)
        {
            titleText.text = caseTitle;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(TypewriterEffect(backStory));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        storyText.text = "";
        
        // تشغيل صوت كتابة الآلة الكاتبة إذا أردت (اختياري)
        // AudioClip typeClip = Resources.Load<AudioClip>("Audio/typewriter_sound");

        foreach (char c in fullText)
        {
            storyText.text += c;
            
            // if (typeClip != null) AudioSource.PlayClipAtPoint(typeClip, Camera.main.transform.position);
            
            yield return new WaitForSeconds(typingSpeed);
        }

        // إظهار زر المتابعة بعد انتهاء النص
        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    // تُربط هذه الدالة بزر المتابعة (Continue Button)
    public void StartInvestigation()
    {
        if (briefingPanel != null)
        {
            briefingPanel.SetActive(false);
        }
        
        // هنا يمكنك تفعيل شاشة اختيار الأدوار أو بدء اللعب
        if (RoleSelectionUI.OnPlayerSetupComplete != null)
        {
            // إذا كانت لوحة اختيار الأدوار منفصلة، قم بتفعيلها هنا
        }
    }
}
