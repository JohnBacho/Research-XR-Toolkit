using System.Collections;
using UnityEngine;
using sxr_internal;

public class TrialTester : MonoBehaviour
{
    [SerializeField] private float trialInterval = 5f;

    private IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(trialInterval);

            sxr.NextTrial();

            Debug.Log($"Advanced to Trial {sxr.GetTrial()}");
        }
    }
}