using System;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    uiButton,
    winAudio,
    lossAudio,
    minigamePointSound,
    increaseButtonSound,
    decreaseButtonSound,
    handleSound,
    successTone,
    suspenseLoop,
}

[System.Serializable]
public class AudioClips
{
    public SoundType sounds;
    public AudioClip clip;
}

namespace SoundManager
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClips[] audioClips;
        [SerializeField] private AudioSource audioPrefab;
        private static SoundManager instance = null;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                SetupAudioSources();
            }
        }

        private void SetupAudioSources()
        {
            for (int i = 0; i < audioClips.Length; i++)
            {
                AudioSource src = Instantiate(audioPrefab, transform);
                src.clip = audioClips[i].clip;
                src.gameObject.name = audioClips[i].sounds.ToString();
            }
        }

        private static AudioSource GetSource(SoundType sound)
        {
            return instance.transform
                .Find(sound.ToString())
                .GetComponent<AudioSource>();
        }

        public static void Play(
            SoundType sound,
            Vector3 position,
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
            SoundType sound,
            Vector3 position,
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

        public static void Stop(SoundType sound)
        {
            AudioSource src = GetSource(sound);
            src.Stop();
        }

        public static void PlayLooped(SoundType sound, Vector3 position, float volume = 1, float pitch = 1)
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