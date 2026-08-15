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
    [SerializeField] private ToggleSwitch musicToggle;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;

    private float soundVolume;
    private float musicVolume;

    [Header("Gameplay")]
    [SerializeField] private ToggleSwitch moveHintsToggle;
    [SerializeField] private ToggleSwitch vibrationToggle;

    protected override void OnOpened()
    {
        soundVolume = ServiceLocator.Get<AudioManager>().SFXVolume;
        soundToggle.Setup(!ServiceLocator.Get<AudioManager>().IsSFXMute, OnSoundToggleChanged);
        soundVolumeSlider.SetValueWithoutNotify(ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume);
        UpdateSoundVolumeValueText(soundVolumeSlider.value);

        musicVolume = ServiceLocator.Get<AudioManager>().MusicVolume;
        musicToggle.Setup(!ServiceLocator.Get<AudioManager>().IsMusicMute, OnMusicToggleChanged);
        musicVolumeSlider.SetValueWithoutNotify(ServiceLocator.Get<AudioManager>().IsMusicMute ? 0 : musicVolume);
        UpdateMusicVolumeValueText(musicVolumeSlider.value);

        moveHintsToggle.Setup(ServiceLocator.Get<GameSettingsManager>().ShowMoveHints, OnMoveHintsToggleChanged);
        vibrationToggle.Setup(ServiceLocator.Get<GameSettingsManager>().VibrationEnabled, OnVibrationToggleChanged);
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        ServiceLocator.Get<AudioManager>().ToggleSFXMusicMute();
        soundVolumeSlider.SetValueWithoutNotify(ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume);
        UpdateSoundVolumeValueText(soundVolumeSlider.value);
    }

    public void OnSoundVolumeChanged(float value)
    {
        soundVolume = value;
        ServiceLocator.Get<AudioManager>().UpdateSFXVolume(soundVolume);
        UpdateSoundVolumeValueText(value);
        soundToggle.SetStateWithoutNotify(!ServiceLocator.Get<AudioManager>().IsSFXMute);
    }

    private void UpdateSoundVolumeValueText(float value)
    {
        soundVolumeValueText.text = Mathf.RoundToInt(value * 100).ToString();
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        ServiceLocator.Get<AudioManager>().ToggleMusicMute();
        musicVolumeSlider.SetValueWithoutNotify(ServiceLocator.Get<AudioManager>().IsMusicMute ? 0 : musicVolume);
        UpdateMusicVolumeValueText(musicVolumeSlider.value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        musicVolume = value;
        ServiceLocator.Get<AudioManager>().UpdateMusicVolume(musicVolume);
        UpdateMusicVolumeValueText(value);
        musicToggle.SetStateWithoutNotify(!ServiceLocator.Get<AudioManager>().IsMusicMute);
    }

    private void UpdateMusicVolumeValueText(float value)
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

    public void OnAccountAndLegelClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<WarningNotifier>().Show("Coming Soon!");
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }

    public void OnDoneButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }
}
