using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource bgAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource pieceKillAudioSource;
    [SerializeField] private AudioSource timeTickingAudioSource;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip pieceKilledClip;
    [SerializeField] private AudioClip pieceMoveClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip scrollingMatchmakingClip;
    [SerializeField] private AudioClip crownKingClip;

    private float bgVolume = 0.5f;
    private float sfxVolume = 0.5f;

    private bool isBgMute = false;
    private bool isSfxMute = false;

    public float BgVolume { get { return bgVolume; } }
    public float SFXVolume { get { return sfxVolume; } }

    public bool IsBgMute { get { return isBgMute; } }
    public bool IsSFXMute { get { return isSfxMute; } }

    private void Start()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
        AudioData data = SavingSystem.Instance.Load().audioData;

        isBgMute = data.isMusicMute;
        isSfxMute = data.isSoundMute;
        bgVolume = data.musicVolume;
        sfxVolume = data.soundVolume;
#endif

        bgAudioSource.mute = isBgMute;
        sfxAudioSource.mute = isSfxMute;
        pieceKillAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;

        bgAudioSource.volume = bgVolume;
        sfxAudioSource.volume = sfxVolume;
        pieceKillAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume;
    }

    public void ToggleBgMusicMute()
    {
        isBgMute = !isBgMute;
        bgAudioSource.mute = isBgMute;

        SaveAudioData();
    }

    public void ToggleSFXMusicMute()
    {
        isSfxMute = !isSfxMute;
        sfxAudioSource.mute = isSfxMute;
        pieceKillAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;

        SaveAudioData();
    }

    public void UpdateBgVolume(float volume)
    {
        bgVolume = volume;
        bgVolume = Mathf.Clamp(bgVolume, 0, 1);
        bgAudioSource.volume = bgVolume;

        SaveAudioData();
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        sfxVolume = Mathf.Clamp(sfxVolume, 0, 1);
        sfxAudioSource.volume = sfxVolume;
        pieceKillAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume;

        SaveAudioData();
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

    public void PlayCrownKingSound()
    {
        if (!isSfxMute)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = crownKingClip;
            sfxAudioSource.Play();
        }
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

    private void SaveAudioData()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
        SaveData saveData = SavingSystem.Instance.Load();

        saveData.audioData.isMusicMute = isBgMute;
        saveData.audioData.isSoundMute = isSfxMute;
        saveData.audioData.musicVolume = bgVolume;
        saveData.audioData.soundVolume = sfxVolume;

        SavingSystem.Instance.Save(saveData);
#endif
    }
}

