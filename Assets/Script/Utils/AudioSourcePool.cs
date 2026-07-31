using System.Collections.Generic;
using UnityEngine;

// A growable pool of AudioSources cloned from a template - each Play() call grabs an idle source
// (or clones a new one if every existing source is currently busy) instead of sharing a single
// AudioSource across unrelated one-shot sounds, so two overlapping sounds never cut each other off.
public class AudioSourcePool
{
    private readonly AudioSource template;
    private readonly List<AudioSource> sources = new();

    private float volume = 1f;
    private bool mute;

    public AudioSourcePool(AudioSource template)
    {
        this.template = template;
        sources.Add(template);
    }

    public void Play(AudioClip clip)
    {
        AudioSource source = GetAvailableSource();
        source.clip = clip;
        source.Play();
    }

    public void SetVolume(float value)
    {
        volume = value;
        foreach (AudioSource source in sources)
        {
            source.volume = volume;
        }
    }

    public void SetMute(bool value)
    {
        mute = value;
        foreach (AudioSource source in sources)
        {
            source.mute = mute;
        }
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in sources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        AudioSource newSource = Object.Instantiate(template, template.transform.parent);
        newSource.volume = volume;
        newSource.mute = mute;
        sources.Add(newSource);
        return newSource;
    }
}
