using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RXT
{
    public static class rxt
    {
        private static int uniqueID = -1;
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

        public static void EndProgram()
            => Application.Quit(); 
            // You must Build and Run the application for this to work. It will not work in the editor.
        
        public static void SetUniqueID(int id)
            => uniqueID = id;
        public static int GetUniqueID()
            => uniqueID;
        public static int RandomizeInt(int min, int max)
            => RandomizeManager.RandomizeInt(min, max);
        public static float RandomizeFloat(float min, float max)
            => RandomizeManager.RandomizeFloat(min, max);
        public static int[] RandomizeIntArray(int min, int max, int length)
            => RandomizeManager.RandomizeIntArray(min, max, length);
        public static int[] RandomizeIntArray(int[] array)
            => RandomizeManager.RandomizeIntArray(array);
        public static float[] RandomizeFloatArray(float min, float max, int length)
            => RandomizeManager.RandomizeFloatArray(min, max, length);  
        public static float[] RandomizeFloatArray(float[] array)
            => RandomizeManager.RandomizeFloatArray(array);
        public static string[] RandomizeStringArray(string[] array)
            => RandomizeManager.RandomizeStringArray(array);
        public static string[] LatinSquare(string[] conditions, int participantID)
            => RandomizeManager.LatinSquare(conditions, participantID);
        public static string[] GenerateTrialOrder(string[] conditions, int repeats, bool randomize = true)
            => RandomizeManager.GenerateTrialOrder(conditions, repeats, randomize);

        
        
    }

    
}