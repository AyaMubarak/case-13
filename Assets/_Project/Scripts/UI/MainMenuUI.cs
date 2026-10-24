using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject characterSelectPanel;
    public GameObject lobbyPanel;
    public GameObject quitPanel;

    [Header("Lobby Settings")]
    public Button startGameBtn;

    private void Awake()
    {
        EnsureResponsiveCanvases();
        ResolvePanels();
    }

    private void Start()
    {
        EnsureResponsiveCanvases();

        // إظهار القائمة الرئيسية فقط في البداية
        OpenMainMenu();

        // إخفاء الماوس إذا كان مخفياً وإظهاره
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BindBackButtons();
        BindMainMenuButtons();
    }

    private void EnsureResponsiveCanvases()
    {
        Canvas[] canvases = GetComponentsInParent<Canvas>(true);
        if (canvases == null || canvases.Length == 0)
            canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);

        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.GetComponent<ResponsiveCanvasLayout>() == null)
                canvas.gameObject.AddComponent<ResponsiveCanvasLayout>();
        }
    }

    public void OpenMainMenu()
    {
        mainMenuPanel.SetActive(true);
        if(settingsPanel != null) settingsPanel.SetActive(false);
        if(characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if(lobbyPanel != null) lobbyPanel.SetActive(false);
        if(quitPanel != null) quitPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if(settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenCharacterSelect()
    {
        mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
    }

    private void ResolvePanels()
    {
        if (characterSelectPanel == null)
        {
            CharacterSelectManager selector = FindAnyObjectByType<CharacterSelectManager>(FindObjectsInactive.Include);
            if (selector != null) characterSelectPanel = selector.gameObject;
        }

        if (lobbyPanel == null)
        {
            LobbyUIManager lobby = FindAnyObjectByType<LobbyUIManager>(FindObjectsInactive.Include);
            if (lobby != null) lobbyPanel = lobby.gameObject;
        }
    }

    private void BindBackButtons()
    {
        foreach (Button backButton in GetComponentsInChildren<Button>(true))
        {
            if (backButton.GetComponentInParent<LobbyUIManager>(true) != null)
                continue;
            if (backButton.GetComponentInParent<CharacterSelectManager>(true) != null)
                continue;

            string buttonName = backButton.name.ToLowerInvariant();
            if (!buttonName.Contains("back")) continue;

            if (buttonName.Contains("role"))
            {
                backButton.onClick.AddListener(OpenCharacterSelect);
            }
            else
            {
                backButton.onClick.AddListener(OpenMainMenu);
            }
        }
    }

    private void BindMainMenuButtons()
    {
        if (mainMenuPanel == null) return;

        foreach (Button button in mainMenuPanel.GetComponentsInChildren<Button>(true))
        {
            string key = button.name.ToLowerInvariant();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) key += " " + label.text.ToLowerInvariant();

            if (key.Contains("setting"))
            {
                button.onClick.RemoveListener(OpenSettings);
                button.onClick.AddListener(OpenSettings);
            }
            else if (key.Contains("quit") || key.Contains("exit"))
            {
                button.onClick.RemoveListener(OpenQuitPanel);
                button.onClick.AddListener(OpenQuitPanel);
            }
            else if (key.Contains("play") || key.Contains("start") || key.Contains("case") || key.Contains("begin"))
            {
                button.onClick.RemoveListener(OpenCharacterSelect);
                button.onClick.AddListener(OpenCharacterSelect);
            }
        }
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
