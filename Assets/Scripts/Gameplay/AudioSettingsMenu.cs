
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AudioSettingsMenu : MonoBehaviour
{
    private const string MasterKey = "MasterVolume";
    private const string SFXKey = "SFXVolume";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Value Labels")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text sfxValueText;

    private AudioSource sfxSource;

    private void Awake()
    {
        sfxSource = GetComponent<AudioSource>();

        float master = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MasterKey, 0.8f)
        );

        float sfx = Mathf.Clamp01(
            PlayerPrefs.GetFloat(SFXKey, 1f)
        );

        AudioListener.volume = master;
        sfxSource.volume = sfx;

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(master);
            masterSlider.onValueChanged.AddListener(
                SetMasterVolume
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfx);
            sfxSlider.onValueChanged.AddListener(
                SetSFXVolume
            );
        }

        UpdateLabels();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);

        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterKey, value);

        UpdateLabels();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        sfxSource.volume = value;
        PlayerPrefs.SetFloat(SFXKey, value);

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (masterValueText != null)
        {
            masterValueText.text =
                $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }

        if (sfxValueText != null)
        {
            sfxValueText.text =
                $"{Mathf.RoundToInt(sfxSource.volume * 100f)}%";
        }
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(
                SetMasterVolume
            );

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(
                SetSFXVolume
            );

        PlayerPrefs.Save();
    }
}
