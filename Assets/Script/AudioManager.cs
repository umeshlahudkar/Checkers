using System.Collections;
using UnityEngine;

public class AudioManager : Service<AudioManager>, IInitializable
{
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource pieceKillAudioSource;
    [SerializeField] private AudioSource timeTickingAudioSource;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip pieceKilledClip;
    [SerializeField] private AudioClip pieceMoveClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip crownKingClip;

    private AudioSourcePool sfxSourcePool;

    private float sfxVolume = 0.5f;

    private bool isSfxMute = false;

    public float SFXVolume { get { return sfxVolume; } }

    public bool IsSFXMute { get { return isSfxMute; } }

    public IEnumerator Initialize()
    {
        sfxSourcePool = new AudioSourcePool(sfxAudioSource);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        if (SavingSystem.Exists(AudioData.FileName))
        {
            AudioData data = SavingSystem.Load<AudioData>(AudioData.FileName);

            isSfxMute = data.isSoundMute;
            sfxVolume = data.soundVolume;
        }
#endif

        sfxSourcePool.SetMute(isSfxMute);
        pieceKillAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;

        sfxSourcePool.SetVolume(sfxVolume);
        pieceKillAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume;

        yield break;
    }

    public void ToggleSFXMusicMute()
    {
        isSfxMute = !isSfxMute;
        sfxSourcePool.SetMute(isSfxMute);
        pieceKillAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;

        SaveAudioData();
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxVolume = Mathf.Clamp(sfxVolume, 0, 1);
        sfxSourcePool.SetVolume(sfxVolume);
        pieceKillAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume;

        SaveAudioData();
    }

    public void PlayButtonClickSound()
    {
        PlaySfx(buttonClickClip);
    }

    public void PlayPieceMoveSound()
    {
        PlaySfx(pieceMoveClip);
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
        PlaySfx(coinClip);
    }

    public void PlayCrownKingSound()
    {
        PlaySfx(crownKingClip);
    }

    public void PlayTimeTickingSound()
    {
        if (!isSfxMute)
        {
            timeTickingAudioSource.Stop();
            timeTickingAudioSource.Play();
        }
    }

    public void StopTimeTickingSound()
    {
        timeTickingAudioSource.Stop();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (!isSfxMute)
        {
            sfxSourcePool.Play(clip);
        }
    }

    private void SaveAudioData()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        AudioData data = new()
        {
            isSoundMute = isSfxMute,
            soundVolume = sfxVolume
        };

        SavingSystem.Save(AudioData.FileName, data);
#endif
    }
}
