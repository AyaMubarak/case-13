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

    private void Awake()
    {
        LoadSettings();
    }

    private void OnEnable()
    {
        LoadSettings();
        ApplyResponsiveSettings();
    }

    private void Start()
    {
        ApplyResponsiveSettings();
    }

    public void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("Settings_Master", 0.8f);
        float music = PlayerPrefs.GetFloat("Settings_Music", 0.7f);
        float sfx = PlayerPrefs.GetFloat("Settings_SFX", 0.8f);

        if (masterSlider != null)
        {
            masterSlider.value = master;
            UpdateText(masterPercentText, master);
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.value = music;
            UpdateText(musicPercentText, music);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfx;
            UpdateText(sfxPercentText, sfx);
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        AudioListener.volume = master;
    }

    public void OnMasterChanged(float val)
    {
        UpdateText(masterPercentText, val);
        PlayerPrefs.SetFloat("Settings_Master", val);
        PlayerPrefs.Save();
        AudioListener.volume = val;
    }

    public void OnMusicChanged(float val)
    {
        UpdateText(musicPercentText, val);
        PlayerPrefs.SetFloat("Settings_Music", val);
        PlayerPrefs.Save();
    }

    public void OnSFXChanged(float val)
    {
        UpdateText(sfxPercentText, val);
        PlayerPrefs.SetFloat("Settings_SFX", val);
        PlayerPrefs.Save();
    }

    private void UpdateText(TextMeshProUGUI txt, float val)
    {
        if (txt != null) txt.text = Mathf.RoundToInt(val * 100f) + "%";
    }

    private void ApplyResponsiveSettings()
    {
        SettingsResponsiveLayout layout = GetComponent<SettingsResponsiveLayout>();
        if (layout != null)
        {
            layout.ApplyCleanLayout();
        }
    }
}
