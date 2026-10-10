using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("UI Percentage Texts")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    [Header("Game Settings")]
    public TMP_Dropdown difficultyDropdown;
    public Toggle invertYToggle;

    private void Start()
    {
        // تهيئة القيم المبدئية عند فتح الشاشة
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(UpdateMasterVolume);
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            UpdateMasterVolume(masterVolumeSlider.value);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            UpdateMusicVolume(musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(UpdateSFXVolume);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            UpdateSFXVolume(sfxVolumeSlider.value);
        }

        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(UpdateDifficulty);
            difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 0); // 0 = Easy
        }

        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.AddListener(UpdateInvertY);
            invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
        }
    }

    private void UpdateMasterVolume(float value)
    {
        if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("MasterVolume", value);
        // هنا يتم ربط الصوت الحقيقي لاحقاً
    }

    private void UpdateMusicVolume(float value)
    {
        if (musicVolumeText != null) musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void UpdateSFXVolume(float value)
    {
        if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void UpdateDifficulty(int index)
    {
        PlayerPrefs.SetInt("Difficulty", index);
        Debug.Log("Difficulty set to: " + index);
    }

    private void UpdateInvertY(bool isOn)
    {
        PlayerPrefs.SetInt("InvertY", isOn ? 1 : 0);
        Debug.Log("Invert Y Axis: " + isOn);
    }
}
