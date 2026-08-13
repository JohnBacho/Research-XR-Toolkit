using System.Collections;
using UnityEngine;
using sxr_internal;
using RXT;

public class RXTIntroDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    [SerializeField] private float baselineDuration = 2f;
    [SerializeField] private float eventSummaryDuration = 2f;
    [SerializeField] private float delayBetweenTrials = 3f;

    [Header("Demo Conditions")]
    [SerializeField]
    private string[] conditions = { "Control", "Condition A", "Condition B" };

    private IEnumerator Start()
    {
        Debug.Log("====================================");
        Debug.Log("        RXT TOOLKIT DEMO");
        Debug.Log("====================================");

        yield return new WaitForSeconds(1f);

        // --------------------------------------------------
        // 1. PARTICIPANT SETUP
        // --------------------------------------------------

        Debug.Log("Setting participant ID...");

        Debug.Log($"Participant ID: {rxt.GetUniqueID()}");

        yield return new WaitForSeconds(1f);

        // --------------------------------------------------
        // 2. GENERATE TRIAL ORDER
        // --------------------------------------------------

        Debug.Log("Generating randomized trial order...");

        string[] trialOrder =
            rxt.GenerateTrialOrder(
                conditions,
                repeats: 2,
                randomize: true
            );

        Debug.Log("Trial order:");

        foreach (string condition in trialOrder)
        {
            Debug.Log($"  - {condition}");
        }

        yield return new WaitForSeconds(2f);

        // --------------------------------------------------
        // 3. START RECORDING
        // --------------------------------------------------

        Debug.Log("Starting data recording...");

        rxt.StartRecording();

        yield return new WaitForSeconds(2f);

        // --------------------------------------------------
        // 4. DEMONSTRATE TRIALS
        // --------------------------------------------------

        for (int i = 0; i < trialOrder.Length; i++)
        {
            yield return StartCoroutine(
                RunTrial(i + 1, trialOrder[i])
            );
        }

        // --------------------------------------------------
        // 5. FINISH
        // --------------------------------------------------

        Debug.Log("====================================");
        Debug.Log("       RXT DEMO COMPLETE");
        Debug.Log("====================================");

        rxt.StopEventSummaryTimer();

        yield return new WaitForSeconds(2f);

        rxt.PauseRecording();

        Debug.Log("Recording paused.");
        Debug.Log("Thank you for trying RXT!");
    }


    private IEnumerator RunTrial(int trialNumber, string condition)
    {
        Debug.Log("------------------------------------");
        Debug.Log($"Starting Trial {trialNumber}");
        Debug.Log($"Condition: {condition}");
        Debug.Log("------------------------------------");

        // --------------------------------------------------
        // BASELINE
        // --------------------------------------------------

        Debug.Log("Starting baseline...");

        rxt.StartBaseline(baselineDuration);

        yield return new WaitForSeconds(baselineDuration);

        // --------------------------------------------------
        // EVENT SUMMARY TIMER
        // --------------------------------------------------

        Debug.Log("Starting event summary timer...");

        rxt.StartEventSummaryTimer(eventSummaryDuration);

        // --------------------------------------------------
        // AUDIO
        // --------------------------------------------------

        Debug.Log("Playing trial audio...");

        rxt.PlayAudioOnce(
            "GamePoint",
            transform.position,
            volume: 1f,
            pitch: 1f
        );

        // --------------------------------------------------
        // HAPTICS
        // --------------------------------------------------

        Debug.Log("Triggering haptic feedback...");

        rxt.TriggerHaptic();

        // --------------------------------------------------
        // SXR STATE
        // --------------------------------------------------

        Debug.Log($"Setting sXR state to: {condition}");

        sxr.SetState(condition);

        yield return new WaitForSeconds(delayBetweenTrials);

        // --------------------------------------------------
        // NEXT TRIAL
        // --------------------------------------------------

        sxr.NextTrial();

        Debug.Log($"Advanced to sXR Trial {sxr.GetTrial()}");

        // Make sure the event timer isn't left running.

        yield return new WaitForSeconds(1f);
    }
}