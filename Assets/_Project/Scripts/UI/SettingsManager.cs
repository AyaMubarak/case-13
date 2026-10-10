using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterPercentText;
    [Space]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicPercentText;
    [Space]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxPercentText;

    [Header("Gameplay")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Toggle invertYToggle;

    private void Awake()
    {
        LoadSettings();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("Settings_Master", 0.8f);
        float music = PlayerPrefs.GetFloat("Settings_Music", 0.7f);
        float sfx = PlayerPrefs.GetFloat("Settings_SFX", 0.8f);
        int difficulty = PlayerPrefs.GetInt("Settings_Difficulty", 0);

        if (masterSlider != null) { masterSlider.value = master; UpdateText(masterPercentText, master); }
        if (musicSlider != null) { musicSlider.value = music; UpdateText(musicPercentText, music); }
        if (sfxSlider != null) { sfxSlider.value = sfx; UpdateText(sfxPercentText, sfx); }
        if (difficultyDropdown != null) difficultyDropdown.value = difficulty;
        if (invertYToggle != null) invertYToggle.isOn = PlayerPrefs.GetInt("Settings_InvertY", 0) == 1;
    }

    public void OnMasterChanged(float val)
    {
        UpdateText(masterPercentText, val);
        PlayerPrefs.SetFloat("Settings_Master", val);
        AudioListener.volume = val;
    }

    public void OnMusicChanged(float val)
    {
        UpdateText(musicPercentText, val);
        PlayerPrefs.SetFloat("Settings_Music", val);
    }

    public void OnSFXChanged(float val)
    {
        UpdateText(sfxPercentText, val);
        PlayerPrefs.SetFloat("Settings_SFX", val);
    }

    public void OnDifficultyChanged(int index)
    {
        PlayerPrefs.SetInt("Settings_Difficulty", index);
        PlayerPrefs.Save();
    }

    public void OnInvertYChanged(bool isInverse)
    {
        PlayerPrefs.SetInt("Settings_InvertY", isInverse ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void UpdateText(TextMeshProUGUI txt, float val)
    {
        if (txt != null) txt.text = Mathf.RoundToInt(val * 100f) + "%";
    }
}
