using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundInputController : MonoBehaviour
{
    [SerializeField] private Sprite musicOn;
    [SerializeField] private Sprite musicOff;

    [SerializeField] private Image musicOnOffButtonImg;
    [SerializeField] private Slider volumeSlider;

    [SerializeField] private SoundType soundType;

    private float volume;
    private bool isMute;

    private void OnEnable()
    {
        volume = (soundType == SoundType.Bg) ? AudioManager.Instance.BgVolume : AudioManager.Instance.SFXVolume;
        isMute = (soundType == SoundType.Bg) ? AudioManager.Instance.IsBgMute : AudioManager.Instance.IsSFXMute;
        musicOnOffButtonImg.sprite = isMute ? musicOff : musicOn;
        volumeSlider.value = isMute ? 0 : volume;
    }


    public void OnMusicButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();

        isMute = !isMute;
        musicOnOffButtonImg.sprite = isMute ? musicOff : musicOn;
        volumeSlider.value = isMute ? 0 : volume;

        if (soundType == SoundType.Bg)
        {
            AudioManager.Instance.ToggleBgMusicMute();
        }
        else
        {
            AudioManager.Instance.ToggleSFXMusicMute();
        }
    }

    public void OnMusicUpButtonClick()
    {
        if(!isMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            volume += 0.1f;
            volume = Mathf.Clamp(volume, 0, 1);
            volumeSlider.value = volume;

            if (soundType == SoundType.Bg)
            {
                AudioManager.Instance.UpdateBgVolume(volume);
            }
            else
            {
                AudioManager.Instance.UpdateSFXVolume(volume);
            }
        }
    }

    public void OnMusicDownButtonClick()
    {
        if(!isMute)
        {
            AudioManager.Instance.PlayButtonClickSound();

            volume -= 0.1f;
            volume = Mathf.Clamp(volume, 0, 1);
            volumeSlider.value = volume;

            if (soundType == SoundType.Bg)
            {
                AudioManager.Instance.UpdateBgVolume(volume);
            }
            else
            {
                AudioManager.Instance.UpdateSFXVolume(volume);
            }
        }
    }
}
