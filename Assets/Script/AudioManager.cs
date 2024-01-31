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

    public void PlayButtonClickSound()
    {
        sfxAudioSource.Stop();
        sfxAudioSource.clip = buttonClickClip;
        sfxAudioSource.Play();
    }

    public void PlayMatchmakingScrollSound()
    {
        sfxAudioSource.Stop();
        sfxAudioSource.loop = true;
        sfxAudioSource.clip = scrollingMatchmakingClip;
        sfxAudioSource.Play();
    }

    public void StopMatchmakingScrollSound()
    {
        sfxAudioSource.loop = false;
        sfxAudioSource.Stop();
    }

    public void PlayPieceMoveSound()
    {
        sfxAudioSource.Stop();
        sfxAudioSource.clip = pieceMoveClip;
        sfxAudioSource.Play();
    }

    public void PlayPieceKillSound()
    {
        pieceKillAudioSource.Stop();
        pieceKillAudioSource.clip = pieceKilledClip;
        pieceKillAudioSource.Play();
    }
        

    public void PlayCoinSound()
    {
        sfxAudioSource.Stop();
        sfxAudioSource.clip = coinClip;
        sfxAudioSource.Play();
    }

}


