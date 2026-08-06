using System.Collections;
using UnityEngine;

public class AudioManager : Service<AudioManager>, IInitializable
{
    public const string SfxVolumeKey = "Audio.SfxVolume";
    public const string SfxMuteKey = "Audio.SfxMute";
    public const string MusicVolumeKey = "Audio.MusicVolume";
    public const string MusicMuteKey = "Audio.MusicMute";

    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource timeTickingAudioSource;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip pieceKilledClip;
    [SerializeField] private AudioClip pieceMoveClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip crownKingClip;

    private float sfxVolume = 0.5f;
    private float musicVolume = 0.5f;

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

        PlayerPrefs.SetInt(MusicMuteKey, isMusicMute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        isMusicMute = musicVolume <= 0f;

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetInt(MusicMuteKey, isMusicMute ? 1 : 0);
        PlayerPrefs.Save();
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
        PlaySfx(pieceKilledClip);
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
        if (isSfxMute)
        {
            return;
        }

        timeTickingAudioSource.pitch = 1f;
        timeTickingAudioSource.Stop();
        timeTickingAudioSource.Play();
    }

    public void StopTimeTickingSound()
    {
        timeTickingAudioSource.Stop();
        timeTickingAudioSource.pitch = 1f;
    }

    // Speeds the ticking loop up (and, as a side effect of pitch, raises its tone) as the turn
    // timer approaches zero - mirrors the same urgency curve PlayerCardUI's low-time blink/pulse
    // ramps up on, so the sound and the visual escalate together instead of the tick staying flat
    // while everything else on screen gets more frantic.
    public void SetTimeTickingUrgency(float urgency)
    {
        timeTickingAudioSource.pitch = Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(urgency));
    }

    // PlayOneShot layers overlapping SFX on this single source itself, mixed together, instead of
    // needing a pool of cloned AudioSource components to avoid one cutting another off.
    private void PlaySfx(AudioClip clip)
    {
        if (isSfxMute)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip);
    }

    private void ApplyMute()
    {
        sfxAudioSource.mute = isSfxMute;
        timeTickingAudioSource.mute = isSfxMute;
    }

    private void ApplyVolume()
    {
        sfxAudioSource.volume = sfxVolume;
        timeTickingAudioSource.volume = sfxVolume;
    }
}
