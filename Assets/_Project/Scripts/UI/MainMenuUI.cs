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

    public static string SelectedCaseScene = "Case01_Main";

    public void SelectCase1()
    {
        SelectedCaseScene = "Case01_Main";
        PlayerPrefs.SetString("Selected_Case", "Case01_Main");
        PlayerPrefs.Save();
        Debug.Log("[CASE SELECTION] Selected Case 01: The Midnight Murder");
    }

    public void SelectCase2()
    {
        SelectedCaseScene = "Case02_Main";
        PlayerPrefs.SetString("Selected_Case", "Case02_Main");
        PlayerPrefs.Save();
        Debug.Log("[CASE SELECTION] Selected Case 02: The Cypher Vault Protocol");
    }

    public void StartGame()
    {
        string sceneToLoad = PlayerPrefs.GetString("Selected_Case", SelectedCaseScene);
        SceneManager.LoadScene(sceneToLoad);
    }

    public void StartCase1()
    {
        SelectCase1();
        SceneManager.LoadScene("Case01_Main");
    }

    public void StartCase2()
    {
        SelectCase2();
        SceneManager.LoadScene("Case02_Main");
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
