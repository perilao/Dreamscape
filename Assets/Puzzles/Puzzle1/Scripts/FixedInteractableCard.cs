using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FixedInteractableCard : InteractableCard
{
    private FixedJoint fixedJoint;

    void Awake()
    {
        fixedJoint = GetComponent<FixedJoint>();

        grabInteractable.selectEntered.AddListener( OnGrab );
    }

    private void OnGrab(SelectEnterEventArgs arg0)
    {
        grabInteractable.selectEntered.RemoveListener( OnGrab );

        Destroy(fixedJoint);
    }

    void OnValidate()
    {
        ConnectOnValidate();
    }
}
