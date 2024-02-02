using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource bgAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource pieceKillAudioSource;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip pieceKilledClip;
    [SerializeField] private AudioClip pieceMoveClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip scrollingMatchmakingClip;

    private float bgVolume = 0.5f;
    private float sfxVolume = 0.5f;

    private bool isBgMute = false;
    private bool isSfxMute = false;

    public float BgVolume { get { return bgVolume; } }
    public float SFXVolume { get { return sfxVolume; } }

    public bool IsBgMute { get { return isBgMute; } }
    public bool IsSFXMute { get { return isSfxMute; } }


    private void OnEnable()
    {
        bgAudioSource.mute = isBgMute;
        sfxAudioSource.mute = isSfxMute;
        pieceKillAudioSource.mute = isSfxMute;

        bgAudioSource.volume = bgVolume;
        sfxAudioSource.volume = sfxVolume;
        pieceKillAudioSource.volume = sfxVolume;
    }

    public void ToggleBgMusicMute()
    {
        isBgMute = !isBgMute;
        bgAudioSource.mute = isBgMute;
    }

    public void ToggleSFXMusicMute()
    {
        isSfxMute = !isSfxMute;
        sfxAudioSource.mute = isSfxMute;
        pieceKillAudioSource.mute = isSfxMute;
    }

    public void UpdateBgVolume(float volume)
    {
        bgVolume = volume;
        bgVolume = Mathf.Clamp(bgVolume, 0, 1);
        bgAudioSource.volume = bgVolume;
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxVolume = Mathf.Clamp(sfxVolume, 0, 1);
        sfxAudioSource.volume = sfxVolume;
        pieceKillAudioSource.volume = sfxVolume;
    }

    public void PlayButtonClickSound()
    {
        if(!isSfxMute)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = buttonClickClip;
            sfxAudioSource.Play();
        }
    }

    public void PlayMatchmakingScrollSound()
    {
        if (!isSfxMute)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.loop = true;
            sfxAudioSource.clip = scrollingMatchmakingClip;
            sfxAudioSource.Play();
        }
    }

    public void StopMatchmakingScrollSound()
    {
        sfxAudioSource.loop = false;
        sfxAudioSource.Stop();
    }

    public void PlayPieceMoveSound()
    {
        if (!isSfxMute)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = pieceMoveClip;
            sfxAudioSource.Play();
        }
    }

    public void PlayPieceKillSound()
    {
        if (!isSfxMute)
        {
            pieceKillAudioSource.Stop();
            pieceKillAudioSource.clip = pieceKilledClip;
            pieceKillAudioSource.Play();
        }
    }
        

    public void PlayCoinSound()
    {
        if (!isSfxMute)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = coinClip;
            sfxAudioSource.Play();
        }
    }

}

public enum SoundType
{
    SFX,
    Bg
}


