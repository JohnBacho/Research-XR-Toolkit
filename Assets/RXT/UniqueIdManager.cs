using System;
using System.IO;
using UnityEngine;
using RXT;

public class UniqueIdManager : MonoBehaviour
{

    private const string FileName = "RunCounter.txt";

    private void Awake()
    {
        string experimentRoot = Path.Combine(
            Application.dataPath,
            "Data", "RunCounter"
    );

        string filePath = Path.Combine(experimentRoot, FileName);

        int counter = 1;

        try
        {
            Directory.CreateDirectory(experimentRoot);

            if (File.Exists(filePath))
            {
                string text = File.ReadAllText(filePath).Trim();

                if (int.TryParse(text, out int value))
                {
                    counter = value + 1;
                }
                else
                {
                    Debug.LogWarning(
                        $"RunCounter.txt corrupted ('{text}'), resetting to 1."
                    );
                }
            }

            File.WriteAllText(filePath, counter.ToString());

            rxt.SetUniqueID(counter);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unique ID generation failed:\n{ex}");

            counter = Mathf.Abs(Environment.TickCount);
            rxt.SetUniqueID(counter);
        }
    }
}