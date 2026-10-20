using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class PuzzleToggleButton : PuzzleObject
{
    public UnityEvent StateChanged;

    private bool originalKinematicState;

    public enum ButtonState
    {
        Up,
        Down
    }

    [SerializeField] private ButtonState initialState = ButtonState.Up;

    [SerializeField] private bool ChangeStateAtAnimationEnd = false;

    [SerializeField] private Transform upLocalTransform;
    [SerializeField] private Transform downLocalTransform;

    public ButtonState CurrentState { get; private set; }

    public Rigidbody rigidbody { get; private set; }

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        rigidbody = GetComponent<Rigidbody>();

        originalKinematicState = rigidbody.isKinematic;

        CurrentState = initialState;

        if (!rigidbody.isKinematic)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        rigidbody.isKinematic = true;

        Transform initialTransform = initialState == ButtonState.Up ? upLocalTransform : downLocalTransform;

        transform.SetLocalPositionAndRotation(initialTransform.localPosition, initialTransform.localRotation);

        Physics.SyncTransforms();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Toggle();
    }

    public void Toggle()
    {
        ButtonState newState = CurrentState == ButtonState.Up ? ButtonState.Down : ButtonState.Up;

        SetState(newState);
    }

    private void SetState(ButtonState newState, bool forceUpdate = false)
    {
        if (CurrentState == newState && !forceUpdate)
            return;

        CurrentState = newState;

        AnimateButton
        (
            newState == ButtonState.Up ? upLocalTransform : downLocalTransform,
            0.5f,
            LeanTweenType.easeOutCubic
        );

        if (!ChangeStateAtAnimationEnd)
            StateChanged?.Invoke();
    }

    private void AnimateButton(Transform targetTransform, float duration, LeanTweenType easeType)
    {
        ForceRelease();

        LeanTween.cancel(gameObject);

        rigidbody.isKinematic = true;

        targetTransform.GetLocalPositionAndRotation(out Vector3 targetPosition, out Quaternion targetRotation);

        LeanTween.moveLocal(gameObject, targetPosition, duration).setEase(easeType);

        LeanTween.rotateLocal(gameObject, targetRotation.eulerAngles, duration).setEase(easeType)
        .setOnComplete(() =>
        {
            transform.SetLocalPositionAndRotation(targetPosition, targetRotation);

            Physics.SyncTransforms();

            rigidbody.isKinematic = originalKinematicState;

            if (ChangeStateAtAnimationEnd)
                StateChanged?.Invoke();
        });
    }

    public void ForceRelease()
    {
        if (!interactable.isSelected)
            return;

        XRInteractionManager interactionManager = interactable.interactionManager;

        foreach (IXRSelectInteractor interactor in interactable.interactorsSelecting.ToArray())
        {
            interactionManager.SelectExit(interactor, interactable);
        }
    }
}