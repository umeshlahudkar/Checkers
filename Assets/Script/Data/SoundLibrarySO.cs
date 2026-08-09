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
    }

    public List<SoundEntry> sounds;

    public AudioClip GetClip(SoundType soundType)
    {
        for (int i = 0; i < sounds.Count; i++)
        {
            if (sounds[i].soundType == soundType)
            {
                return sounds[i].clip;
            }
        }

        return null;
    }
}
