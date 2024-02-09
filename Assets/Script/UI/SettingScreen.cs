using UnityEngine;
using UnityEngine.UI;

public class SettingScreen : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    [SerializeField] private Image musicOnOffButtonImg;
    [SerializeField] private Slider musicVolumeSlider;

    private float musicVolume;
    private bool isMusicMute;

    [Header("SFX")]
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [SerializeField] private Image soundOnOffButtonImg;
    [SerializeField] private Slider soundVolumeSlider;

    private float soundVolume;
    private bool isSoundMute;

    [Header("Fader")]
    [SerializeField] private GameObject faderScreen;

    private void OnEnable()
    {
        InitializeMusicUI();
        InitializeSoundUI();
    }

    private void InitializeMusicUI()
    {
        musicVolume = AudioManager.Instance.BgVolume;
        isMusicMute = AudioManager.Instance.IsBgMute;
        musicOnOffButtonImg.sprite = isMusicMute ? musicOffSprite : musicOnSprite;
        musicVolumeSlider.value = isMusicMute ? 0 : musicVolume;
    }

    private void InitializeSoundUI()
    {
        soundVolume = AudioManager.Instance.SFXVolume;
        isSoundMute = AudioManager.Instance.IsSFXMute;
        soundOnOffButtonImg.sprite = isSoundMute ? soundOffSprite : soundOnSprite;
        soundVolumeSlider.value = isSoundMute ? 0 : soundVolume;
    }


    public void OnMusicToggleButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();

        isMusicMute = !isMusicMute;
        musicOnOffButtonImg.sprite = isMusicMute ? musicOffSprite : musicOnSprite;
        musicVolumeSlider.value = isMusicMute ? 0 : musicVolume;
        AudioManager.Instance.ToggleBgMusicMute();
    }

    public void OnMusicVolumeUpButtonClick()
    {
        if (!isMusicMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            musicVolume += 0.1f;
            musicVolume = Mathf.Clamp(musicVolume, 0, 1);
            musicVolumeSlider.value = musicVolume;
            AudioManager.Instance.UpdateBgVolume(musicVolume);
        }
    }

    public void OnMusicVolumeDownButtonClick()
    {
        if (!isMusicMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            musicVolume -= 0.1f;
            musicVolume = Mathf.Clamp(musicVolume, 0, 1);
            musicVolumeSlider.value = musicVolume;
            AudioManager.Instance.UpdateBgVolume(musicVolume);
        }
    }

    public void OnSoundToggleButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();

        isSoundMute = !isSoundMute;
        soundOnOffButtonImg.sprite = isSoundMute ? soundOffSprite : soundOnSprite;
        soundVolumeSlider.value = isSoundMute ? 0 : soundVolume;
        AudioManager.Instance.ToggleSFXMusicMute();
    }

    public void OnSoundVolumeUpButtonClick()
    {
        if (!isSoundMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            soundVolume += 0.1f;
            soundVolume = Mathf.Clamp(soundVolume, 0, 1);
            soundVolumeSlider.value = soundVolume;
            AudioManager.Instance.UpdateSFXVolume(soundVolume);
        }
    }

    public void OnSoundVolumeDownButtonClick()
    {
        if (!isSoundMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            soundVolume -= 0.1f;
            soundVolume = Mathf.Clamp(soundVolume, 0, 1);
            soundVolumeSlider.value = soundVolume;
            AudioManager.Instance.UpdateSFXVolume(soundVolume);
        }
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.Deactivate();
    }
}
