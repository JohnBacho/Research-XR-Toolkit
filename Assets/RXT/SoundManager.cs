using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AudioClips
{
    public string AudioName;
    public AudioClip clip;
}

namespace SoundManager
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClips[] audioClips;
        [SerializeField] private AudioSource audioPrefab;
        private Dictionary<string, AudioClip> AudioFiles = new();

        private static SoundManager instance = null;

        private void Awake()
        {
            if (instance == null)
                instance = this;

            foreach (AudioClips audio in audioClips)
            {
                if (audio.clip == null) continue;
                if (audio.AudioName == null) audio.AudioName = audio.clip != null ? audio.clip.name : "";
                AudioFiles[audio.AudioName.ToLower()] = audio.clip;
            }

            SetupAudioSources();
        }

        private void SetupAudioSources()
        {
            foreach(KeyValuePair<string, AudioClip> kvp in AudioFiles)
            {
                AudioSource src = Instantiate(audioPrefab, transform);
                src.clip = kvp.Value;
                src.gameObject.name = kvp.Key;
            }
        }

        private static AudioSource GetSource(string sound)
        {
            return instance.transform.Find(sound.ToLower())?.GetComponent<AudioSource>();
        }

        public static void Play(
            string sound,
            Vector3 position = default(Vector3),
            float volume = 1,
            float pitch = 1
        )
        {
            AudioSource src = GetSource(sound);
            src.transform.position = position;
            src.volume = volume;
            src.pitch = pitch;
            src.PlayOneShot(src.clip);
        }

        public static void PlayOnce(
            string sound,
            Vector3 position = default(Vector3),
            float volume = 1,
            float pitch = 1
        )
        {
            AudioSource src = GetSource(sound);
            src.transform.position = position;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();
        }

        public static void Stop(string sound)
        {
            AudioSource src = GetSource(sound);
            src.Stop();
        }

        public static void PlayLooped(string sound, Vector3 position = default(Vector3), float volume = 1, float pitch = 1)
        {
            AudioSource src = GetSource(sound);
            src.transform.position = position;
            src.volume = volume;
            src.pitch = pitch;
            src.loop = true;
            src.Play();
        }
    }
}