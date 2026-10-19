using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XROffsetGrabInteractable : XRGrabInteractable
{
    void Start()
    {
        if (!attachTransform)
        {
            GameObject attachPoint = new("Offset Grab Pivot");

            attachPoint.transform.SetParent(transform, false);

            attachTransform = attachPoint.transform;
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        attachTransform.SetPositionAndRotation
        (
            args.interactorObject.transform.position, 
            args.interactorObject.transform.rotation
        );
        
        base.OnSelectEntered(args);
    }
}
