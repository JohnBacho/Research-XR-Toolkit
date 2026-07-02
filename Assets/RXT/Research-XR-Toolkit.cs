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

        public static bool RecordingGaze()
            => DataCollection.Instance != null && DataCollection.Instance.RecordingGaze();

        public static void StartBaseline()
            => DataCollection.Instance?.StartBaseline();

        public static void StartEventSummaryTimer(float time)
            => DataCollection.Instance?.StartEventSummaryTimer(time);

        public static void StopEventSummaryTimer()
            => DataCollection.Instance?.StopEventSummaryTimer();
        
        public static void PlayAudio(
            SoundType sound,
            Vector3 position,
            float volume = 1,
            float pitch = 1
        ) => SoundManager.SoundManager.Play(sound, position, volume, pitch);

        public static void PlayAudioOnce(
            SoundType sound,
            Vector3 position,
            float volume = 1,
            float pitch = 1
        ) => SoundManager.SoundManager.PlayOnce(sound, position, volume, pitch);

        public static void PlayAudioLooped(SoundType sound, Vector3 position, float volume = 1, float pitch = 1)
            => SoundManager.SoundManager.PlayLooped(sound, position, volume, pitch);
        public static void StopAudio(SoundType sound)
            => SoundManager.SoundManager.Stop(sound);
    }
}