using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject characterSelectPanel; // غيرناها لتنتقل لاختيار الشخصية أولاً
    public GameObject quitPanel;

    [Header("Lobby Settings")]
    public Button startGameBtn;

    private void Start()
    {
        // إظهار القائمة الرئيسية فقط في البداية
        OpenMainMenu();

        // إخفاء الماوس إذا كان مخفياً وإظهاره
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenMainMenu()
    {
        mainMenuPanel.SetActive(true);
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if(quitPanel != null) quitPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if(settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenCharacterSelect()
    {
        mainMenuPanel.SetActive(false);
        if(characterSelectPanel != null) characterSelectPanel.SetActive(true);
    }

    public void StartGame()
    {
        // تحميل مشهد اللعبة (تأكدي أن المشهد الخاص باللعبة رقم 1 في الـ Build Settings)
        SceneManager.LoadScene(1);
    }

    public void OpenQuitPanel()
    {
        if(quitPanel != null) quitPanel.SetActive(true);
    }

    public void ConfirmQuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
