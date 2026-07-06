using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RandomizeManager : MonoBehaviour
{
    public static int RandomizeInt(int min, int max)
    {
        return Random.Range(min, max);
    }

    public static float RandomizeFloat(float min, float max)
    {
        return Random.Range(min, max);
    }

    public static int[] RandomizeIntArray(int min, int max, int length)
    {
        int[] randomArray = new int[length];
        for (int i = 0; i < length; i++)
        {
            randomArray[i] = Random.Range(min, max);
        }
        return randomArray;
    }

    public static float[] RandomizeFloatArray(float min, float max, int length)
    {
        float[] randomArray = new float[length];
        for (int i = 0; i < length; i++)
        {
            randomArray[i] = Random.Range(min, max);
        }
        return randomArray;
    }

    public static int[] RandomizeIntArray(int[] array)
    {
        int[] randomArray = new int[array.Length];
        List<int> usedIndices = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, array.Length);
            } while (usedIndices.Contains(randomIndex));
            usedIndices.Add(randomIndex);
            randomArray[i] = array[randomIndex];
        }
        return randomArray;
    }

    public static string[] RandomizeStringArray(string[] array)
    {
        string[] randomArray = new string[array.Length];
        List<int> usedIndices = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, array.Length);
            } while (usedIndices.Contains(randomIndex));
            usedIndices.Add(randomIndex);
            randomArray[i] = array[randomIndex];
        }
        return randomArray;
    }

    public static float[] RandomizeFloatArray(float[] array)
    {
        float[] randomArray = new float[array.Length];
        List<int> usedIndices = new List<int>();
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, array.Length);
            } while (usedIndices.Contains(randomIndex));
            usedIndices.Add(randomIndex);
            randomArray[i] = array[randomIndex];
        }
        return randomArray;
    }

    public static string[] LatinSquare(string[] conditions, int participantID)
    {
        int n = conditions.Length;
        string[] order = new string[n];

        int offset = participantID % n;

        for (int i = 0; i < n; i++)
        {
            order[i] = conditions[(i + offset) % n];
        }

        return order;
    }

    public static string[] GenerateTrialOrder(string[] conditions, int repeats, bool randomize = true)
    {
        List<string> trialOrder = new List<string>();
        for (int i = 0; i < repeats; i++)
        {
            string[] randomizedConditions = randomize ? RandomizeStringArray(conditions) : conditions;
            trialOrder.AddRange(randomizedConditions);
        }
       
        return trialOrder.ToArray();
    }

}
