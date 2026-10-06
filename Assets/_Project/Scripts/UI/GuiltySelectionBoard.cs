using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuiltySelectionBoard : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject selectionBoardPanel;

    [Header("Settings")]
    public string correctGuiltyName = "Nora Blake"; // اسم المذنب الصحيح
    public int correctScoreReward = 100;
    
    // لاستدعاء هذه الدالة عندما تريد فتح لوحة التحقيق الجماعية لاختيار المذنب
    public void OpenBoard()
    {
        if (selectionBoardPanel != null)
        {
            selectionBoardPanel.SetActive(true);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseBoard()
    {
        if (selectionBoardPanel != null)
        {
            selectionBoardPanel.SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // تُربط هذه الدالة في كل زر للمتهمين في اللوحة الجماعية وتمرر اسمه كـ parameter
    public void SelectSuspect(string suspectName)
    {
        if (suspectName.ToLower() == correctGuiltyName.ToLower())
        {
            Debug.Log("<color=green>CORRECT SUSPECT!</color> Case Solved!");
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(correctScoreReward);
            }
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("CASE SOLVED! GUILTY APPREHENDED.");
            }
            
            // هنا يمكنك تشغيل صوت نجاح أو الانتقال للمرحلة التالية
            AudioClip winClip = Resources.Load<AudioClip>("Audio/meldix-success-340660");
            if (winClip != null)
            {
                AudioSource.PlayClipAtPoint(winClip, Camera.main.transform.position);
            }
        }
        else
        {
            Debug.Log("<color=red>WRONG SUSPECT!</color> Investigation Failed!");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowOnScreenNotification("WRONG SUSPECT! INVESTIGATION FAILED.");
            }
            
            // تشغيل صوت خطأ
            AudioClip errorClip = Resources.Load<AudioClip>("Audio/u_xg7ssi08yr-error-tone-10-363618");
            if (errorClip != null)
            {
                AudioSource.PlayClipAtPoint(errorClip, Camera.main.transform.position);
            }
        }
        
        CloseBoard();
    }
}
