using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RXT
{
        public static class rxt
    {
        public static void StartRecording()
            => DataCollection.Instance?.StartRecording();

        public static void PauseRecording()
            => DataCollection.Instance?.PauseRecording();

        public static void StartEventSummaryTimer(float time)
            => DataCollection.Instance?.StartEventSummaryTimer(time);

        public static void StopEventSummaryTimer()
            => DataCollection.Instance?.StopEventSummaryTimer();

        public static void StartBaseline(float time = 1f)
            => DataCollection.Instance?.StartBaseline(time);
        
        public static void PlayAudio(
            string sound,
            Vector3 position = default(Vector3),
            float volume = 1,
            float pitch = 1
        ) => SoundManager.SoundManager.Play(sound, position, volume, pitch);

        public static void PlayAudioOnce(
            string sound,
            Vector3 position  = default(Vector3),
            float volume = 1,
            float pitch = 1
        ) => SoundManager.SoundManager.PlayOnce(sound, position, volume, pitch);

        public static void PlayAudioLooped(string sound, Vector3 position = default(Vector3), float volume = 1, float pitch = 1)
            => SoundManager.SoundManager.PlayLooped(sound, position, volume, pitch);
        public static void StopAudio(string sound)
            => SoundManager.SoundManager.Stop(sound);
    }
}