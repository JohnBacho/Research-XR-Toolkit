using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine;
using RXT;

public class PokeButton : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool hasTriggered = false;

    [Header("Button Events")]
    [Tooltip("Triggered once when the button is pressed down")]
    [SerializeField] private UnityEvent onButtonPressed;
    [Tooltip("Triggered when the button is released")]
    [SerializeField] private UnityEvent onButtonReleased;
    private static readonly float volume = 0.2f;
    private static readonly float pitch = 2.2f;
    [SerializeField] private bool IsUIButton =true; 

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnPokeEntered);
        interactable.hoverExited.AddListener(OnPokeExited);
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnPokeEntered);
            interactable.hoverExited.RemoveListener(OnPokeExited);
        }
    }

    public void OnPokeEntered(HoverEnterEventArgs args)
    {
        if (!interactable.enabled) return;
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor && !hasTriggered)
        {
            if (IsUIButton)
            {
                rxt.PlayAudio("uiButton", transform.position, volume, pitch);
            }
            onButtonPressed?.Invoke();
            hasTriggered = true;
        }
    }

    public void OnPokeExited(HoverExitEventArgs args)
    {
        if (!interactable.enabled) return;

        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor)
        {
            onButtonReleased?.Invoke();
            hasTriggered = false;
        }
    }

    public void DisableButton()
    {
        if (interactable != null)
            interactable.enabled = false;
            hasTriggered = true;
    }

    public void EnableButton()
    {
        if (interactable != null)
        {
            interactable.enabled = true;
            hasTriggered = false;            
        }

    }
}