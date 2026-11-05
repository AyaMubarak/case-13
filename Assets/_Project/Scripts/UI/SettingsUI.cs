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

    private void Start()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(UpdateMasterVolume);
            masterVolumeSlider.onValueChanged.AddListener(UpdateMasterVolume);
            masterVolumeSlider.value = PlayerPrefs.GetFloat("Settings_Master", 0.8f);
            UpdateMasterVolume(masterVolumeSlider.value);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(UpdateMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
            musicVolumeSlider.value = PlayerPrefs.GetFloat("Settings_Music", 0.7f);
            UpdateMusicVolume(musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(UpdateSFXVolume);
            sfxVolumeSlider.onValueChanged.AddListener(UpdateSFXVolume);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("Settings_SFX", 0.8f);
            UpdateSFXVolume(sfxVolumeSlider.value);
        }
    }

    private void UpdateMasterVolume(float value)
    {
        if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("Settings_Master", value);
        PlayerPrefs.Save();
        AudioListener.volume = value;
    }

    private void UpdateMusicVolume(float value)
    {
        if (musicVolumeText != null) musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("Settings_Music", value);
        PlayerPrefs.Save();
    }

    private void UpdateSFXVolume(float value)
    {
        if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("Settings_SFX", value);
        PlayerPrefs.Save();
    }
}
