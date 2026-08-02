using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPage : Page
{
    [Header("Sound")]
    [SerializeField] private ToggleSwitch soundToggle;
    [SerializeField] private Slider soundVolumeSlider;
    [SerializeField] private TMP_Text soundVolumeValueText;

    [Header("Music")]
    [SerializeField] private TMP_Text musicVolumeValueText;

    private float soundVolume;

    [Header("Gameplay")]
    [SerializeField] private ToggleSwitch moveHintsToggle;
    [SerializeField] private ToggleSwitch vibrationToggle;

    protected override void OnOpened()
    {
        soundVolume = ServiceLocator.Get<AudioManager>().SFXVolume;
        soundToggle.Setup(!ServiceLocator.Get<AudioManager>().IsSFXMute, OnSoundToggleChanged);
        soundVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume;
        UpdateSoundVolumeValueText(soundVolumeSlider.value);

        moveHintsToggle.Setup(ServiceLocator.Get<GameSettingsManager>().ShowMoveHints, OnMoveHintsToggleChanged);
        vibrationToggle.Setup(ServiceLocator.Get<GameSettingsManager>().VibrationEnabled, OnVibrationToggleChanged);
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        ServiceLocator.Get<AudioManager>().ToggleSFXMusicMute();
        soundVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume;
        UpdateSoundVolumeValueText(soundVolumeSlider.value);
    }

    public void OnSoundVolumeChanged(float value)
    {
        soundVolume = value;
        ServiceLocator.Get<AudioManager>().UpdateSFXVolume(soundVolume);
        UpdateSoundVolumeValueText(value);
    }

    private void UpdateSoundVolumeValueText(float value)
    {
        soundVolumeValueText.text = Mathf.RoundToInt(value * 100).ToString();
    }

    public void OnMusicVolumeChanged(float value)
    {
        musicVolumeValueText.text = Mathf.RoundToInt(value * 100).ToString();
    }

    private void OnMoveHintsToggleChanged(bool isOn)
    {
        ServiceLocator.Get<GameSettingsManager>().SetShowMoveHints(isOn);
    }

    private void OnVibrationToggleChanged(bool isOn)
    {
        ServiceLocator.Get<GameSettingsManager>().SetVibrationEnabled(isOn);
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    public void OnDoneButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }
}
