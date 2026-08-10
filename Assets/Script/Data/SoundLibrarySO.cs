using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Scriptable/SoundLibrary")]
public class SoundLibrarySO : ScriptableObject
{
    [Serializable]
    public class SoundEntry
    {
        public SoundType soundType;
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0, 256)] public int priority = 128;
    }

    public List<SoundEntry> sounds;

    public AudioClip GetClip(SoundType soundType)
    {
        return GetSoundEntry(soundType)?.clip;
    }

    public SoundEntry GetSoundEntry(SoundType soundType)
    {
        for (int i = 0; i < sounds.Count; i++)
        {
            if (sounds[i].soundType == soundType)
            {
                return sounds[i];
            }
        }

        return null;
    }
}
