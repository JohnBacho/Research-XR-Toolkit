using System.Collections;
using UnityEngine;
using sxr_internal;
using VIVE.OpenXR.Samples.FacialTracking;

public class TrialTester : MonoBehaviour
{
    [SerializeField] private float trialInterval = 5f;

    private IEnumerator Start()
    {
        while (true)
        {
            DataCollection.Instance.StartEventSummaryTimer(2f);
            yield return new WaitForSeconds(trialInterval);

            sxr.NextTrial();

            Debug.Log($"Advanced to Trial {sxr.GetTrial()}");
        }
    }
}