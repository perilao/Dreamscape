using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CardGrabInteractable : XRGrabInteractable
{
    [Header("Encaixe da carta no socket")]
    [SerializeField] private Transform socketAttach;

    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        if (interactor is XRSocketInteractor)
            return socketAttach != null ? socketAttach : transform;

        // Para a mão, mantém o Attach Transform configurado normalmente.
        return base.GetAttachTransform(interactor);
    }
}
