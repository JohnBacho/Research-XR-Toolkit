using System.Collections;
using UnityEngine;
using sxr_internal;
using RXT;

public class TrialTester : MonoBehaviour
{
    [SerializeField] private float trialInterval = 5f;

    private IEnumerator Start()
    {
        while (true)
        {
            rxt.StartBaseline(2f);
            rxt.StartEventSummaryTimer(2f);
            yield return new WaitForSeconds(trialInterval);
            rxt.PlayAudio("GamePoint", transform.position);
            sxr.SetState("Baseline");

            sxr.NextTrial();

            Debug.Log($"Advanced to Trial {sxr.GetTrial()}");
        }
    }
}