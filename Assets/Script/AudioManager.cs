using System.Collections;
using UnityEngine;

public class AudioManager : Service<AudioManager>, IInitializable
{
    public const string SfxVolumeKey = "Audio.SfxVolume";
    public const string SfxMuteKey = "Audio.SfxMute";
    public const string MusicVolumeKey = "Audio.MusicVolume";
    public const string MusicMuteKey = "Audio.MusicMute";

    [SerializeField] private SoundLibrarySO soundLibrary;

    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource timeTickingAudioSource;

    private float sfxVolume = 0.5f;
    private float musicVolume = 0.5f;

    private SoundLibrarySO.SoundEntry musicEntry;
    private SoundLibrarySO.SoundEntry timerTickEntry;
    private float timerTickBasePitch = 1f;

    private bool isSfxMute = false;
    private bool isMusicMute = false;

    public float SFXVolume { get { return sfxVolume; } }
    public float MusicVolume { get { return musicVolume; } }

    public bool IsSFXMute { get { return isSfxMute; } }
    public bool IsMusicMute { get { return isMusicMute; } }

    public IEnumerator Initialize()
    {
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
        isSfxMute = PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        isMusicMute = PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;

        musicEntry = soundLibrary.GetSoundEntry(SoundType.BackgroundMusic);
        musicAudioSource.clip = musicEntry?.clip;
        musicAudioSource.loop = true;
        musicAudioSource.pitch = musicEntry?.pitch ?? 1f;
        musicAudioSource.priority = musicEntry?.priority ?? 128;

        timerTickEntry = soundLibrary.GetSoundEntry(SoundType.TimerTick);
        timeTickingAudioSource.clip = timerTickEntry?.clip;
        timerTickBasePitch = timerTickEntry?.pitch ?? 1f;
        timeTickingAudioSource.pitch = timerTickBasePitch;
        timeTickingAudioSource.priority = timerTickEntry?.priority ?? 128;

        ApplyMute();
        ApplyVolume();

        yield break;
    }

    public void ToggleSFXMusicMute()
    {
        isSfxMute = !isSfxMute;
        ApplyMute();

        PlayerPrefs.SetInt(SfxMuteKey, isSfxMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        isSfxMute = sfxVolume <= 0f;
        ApplyMute();
        ApplyVolume();

        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(SfxMuteKey, isSfxMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleMusicMute()
    {
        isMusicMute = !isMusicMute;
        ApplyMute();

        PlayerPrefs.SetInt(MusicMuteKey, isMusicMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        isMusicMute = musicVolume <= 0f;
        ApplyMute();
        ApplyVolume();

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetInt(MusicMuteKey, isMusicMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayButtonClickSound()
    {
        PlaySfx(SoundType.ButtonClick);
    }

    public void PlayPieceMoveSound()
    {
        PlaySfx(SoundType.PieceMove);
    }

    public void PlayPieceKillSound()
    {
        PlaySfx(SoundType.PieceCapture);
    }

    public void PlayCoinSound()
    {
        PlaySfx(SoundType.Coin);
    }

    public void PlayCrownKingSound()
    {
        PlaySfx(SoundType.CrownKing);
    }

    public void PlayPieceShrinkSound()
    {
        PlaySfx(SoundType.PieceShrink);
    }

    public void PlayGameOverSound()
    {
        PlaySfx(SoundType.GameOver);
    }

    public void PlayGameWinSound()
    {
        PlaySfx(SoundType.GameWin);
    }

    public void PlayGameLoseSound()
    {
        PlaySfx(SoundType.GameLose);
    }

    public void PlayGameDrawSound()
    {
        PlaySfx(SoundType.GameDraw);
    }

    public void PlayBackgroundMusic()
    {
        if (musicAudioSource.clip == null)
        {
            return;
        }

        musicAudioSource.Play();
    }

    public void StopBackgroundMusic()
    {
        musicAudioSource.Stop();
    }

    public void PlayTimeTickingSound()
    {
        if (isSfxMute || timeTickingAudioSource.clip == null)
        {
            return;
        }

        timeTickingAudioSource.pitch = timerTickBasePitch;
        timeTickingAudioSource.Stop();
        timeTickingAudioSource.Play();
    }

    public void StopTimeTickingSound()
    {
        timeTickingAudioSource.Stop();
        timeTickingAudioSource.pitch = timerTickBasePitch;
    }

    // Speeds the ticking loop up (and, as a side effect of pitch, raises its tone) as the turn
    // timer approaches zero - mirrors the same urgency curve PlayerCardUI's low-time blink/pulse
    // ramps up on, so the sound and the visual escalate together instead of the tick staying flat
    // while everything else on screen gets more frantic.
    public void SetTimeTickingUrgency(float urgency)
    {
        timeTickingAudioSource.pitch = Mathf.Lerp(timerTickBasePitch, timerTickBasePitch * 1.5f, Mathf.Clamp01(urgency));
    }

    // PlayOneShot layers overlapping SFX on this single source itself, mixed together, instead of
    // needing a pool of cloned AudioSource components to avoid one cutting another off.
    private void PlaySfx(SoundType soundType)
    {
        if (isSfxMute)
        {
            return;
        }

        SoundLibrarySO.SoundEntry entry = soundLibrary.GetSoundEntry(soundType);
        if (entry == null || entry.clip == null)
        {
            return;
        }

        sfxAudioSource.pitch = entry.pitch;
        sfxAudioSource.priority = entry.priority;
        sfxAudioSource.PlayOneShot(entry.clip, entry.baseVolume);
    }

    private void ApplyMute()
    {
        sfxAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;
        musicAudioSource.mute = isMusicMute;
    }

    private void ApplyVolume()
    {
        sfxAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume * (timerTickEntry?.baseVolume ?? 1f);
        musicAudioSource.volume = musicVolume * (musicEntry?.baseVolume ?? 1f);
    }
}
