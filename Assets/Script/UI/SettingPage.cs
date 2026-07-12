using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPage : Page
{
    [Header("Board Theme")]
    [SerializeField] private ThemeCard themeCardTemplate;
    [SerializeField] private Transform themeCardContainer;

    private readonly List<ThemeCard> themeCards = new();
    private bool themeCardsCreated;
    private int selectedThemeIndex = -1;

    [Header("Music")]
    [SerializeField] private ToggleSwitch musicToggle;
    [SerializeField] private Slider musicVolumeSlider;

    private float musicVolume;

    [Header("Sound")]
    [SerializeField] private ToggleSwitch soundToggle;
    [SerializeField] private Slider soundVolumeSlider;

    private float soundVolume;

    [Header("Gameplay")]
    [SerializeField] private ToggleSwitch moveHintsToggle;
    [SerializeField] private ToggleSwitch vibrationToggle;

    protected override void OnOpened()
    {
        CreateThemeCards();

        selectedThemeIndex = ServiceLocator.Get<GameSettingsManager>().GetBoardThemeIndex();
        HighlightSelectedTheme(selectedThemeIndex);

        musicVolume = ServiceLocator.Get<AudioManager>().BgVolume;
        musicToggle.Setup(!ServiceLocator.Get<AudioManager>().IsBgMute, OnMusicToggleChanged);
        musicVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsBgMute ? 0 : musicVolume;

        soundVolume = ServiceLocator.Get<AudioManager>().SFXVolume;
        soundToggle.Setup(!ServiceLocator.Get<AudioManager>().IsSFXMute, OnSoundToggleChanged);
        soundVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume;

        moveHintsToggle.Setup(ServiceLocator.Get<GameSettingsManager>().ShowMoveHints, OnMoveHintsToggleChanged);
        vibrationToggle.Setup(ServiceLocator.Get<GameSettingsManager>().VibrationEnabled, OnVibrationToggleChanged);
    }

    private void CreateThemeCards()
    {
        if (themeCardsCreated)
        {
            return;
        }

        int count = ServiceLocator.Get<GameSettingsManager>().BoardThemeCount;
        for (int i = 0; i < count; i++)
        {
            ThemeCard card = Instantiate(themeCardTemplate, themeCardContainer);
            card.gameObject.SetActive(true);
            card.Setup(i, ServiceLocator.Get<GameSettingsManager>().GetBoardTheme(i), OnThemeSelected);
            themeCards.Add(card);
        }

        themeCardsCreated = true;
    }

    private void OnThemeSelected(int index)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        HighlightSelectedTheme(index);
        ServiceLocator.Get<GameSettingsManager>().SetBoardTheme(index);
    }

    private void HighlightSelectedTheme(int index)
    {
        if (selectedThemeIndex >= 0 && selectedThemeIndex < themeCards.Count)
        {
            themeCards[selectedThemeIndex].SetSelected(false);
        }

        selectedThemeIndex = index;

        if (selectedThemeIndex >= 0 && selectedThemeIndex < themeCards.Count)
        {
            themeCards[selectedThemeIndex].SetSelected(true);
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        ServiceLocator.Get<AudioManager>().ToggleBgMusicMute();
        musicVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsBgMute ? 0 : musicVolume;
    }

    public void OnMusicVolumeChanged(float value)
    {
        musicVolume = value;
        ServiceLocator.Get<AudioManager>().UpdateBgVolume(musicVolume);
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        ServiceLocator.Get<AudioManager>().ToggleSFXMusicMute();
        soundVolumeSlider.value = ServiceLocator.Get<AudioManager>().IsSFXMute ? 0 : soundVolume;
    }

    public void OnSoundVolumeChanged(float value)
    {
        soundVolume = value;
        ServiceLocator.Get<AudioManager>().UpdateSFXVolume(soundVolume);
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
