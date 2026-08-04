using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class CustomPokeFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    [SerializeField]
    private XRPokeFilter pokeFilter;

    void Start()
    {
        if (pokeFilter == null)
            pokeFilter = GetComponent<XRPokeFilter>();

        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable != null)
        {
            // Add ourselves as filters AFTER the poke filter
            interactable.hoverFilters.Add(this);
            interactable.selectFilters.Add(this);
        }
        
    }

    void OnDestroy()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverFilters.Remove(this);
            interactable.selectFilters.Remove(this);
        }
    }

    public bool canProcess => isActiveAndEnabled;

    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
    {
        return interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor;
    }

    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        return interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor;
    }
}