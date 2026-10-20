using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Puzzle3Manager : BasePuzzle
{
    private static readonly WaitForSeconds _waitForSeconds0_01 = new(0.01f);

    [Serializable]
    struct Puzzle3ButtonState
    {
        [SerializeField] private HingeJointListener button;
        [SerializeField] private HingeJointListener.HingeJointState correctState;

        [HideInInspector] public Vector3 DefaultRotation;

        public readonly HingeJointListener Button => button;
        public readonly HingeJointListener.HingeJointState CorrectState => correctState;
    }

    [Header("Puzzle Buttons")]
    [SerializeField] private List<Puzzle3ButtonState> puzzleButtons = new();

    [Header("Door")]
    [SerializeField] private XRGrabInteractable doorInteractable;

    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openedRotation = new(0f, 0f, 25f);

    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDuration = 0.3f;

    [Header("Room")]
    [SerializeField] private BoxCollider EnterRoomCollider;

    [SerializeField] private Light roomLight;

    private Rigidbody doorRigidbody;

    private bool originalKinematicState;

    private List<Puzzle3ButtonState> ButtonsSequencerTemp = new();

    private void Awake()
    {
        doorRigidbody = doorInteractable.GetComponent<Rigidbody>();

        originalKinematicState = doorRigidbody.isKinematic;

        doorRigidbody.isKinematic = true;

        doorRigidbody.linearVelocity = Vector3.zero;
        doorRigidbody.angularVelocity = Vector3.zero;

        doorInteractable.transform.localRotation = Quaternion.Euler(closedRotation);

        Physics.SyncTransforms();
    }

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupButtons();

        puzzleButtons.ForEach(puzzleButton => { puzzleButton.Button?.gameObject.SetActive(true); });

        SetDoorColliders(true);

        EnterRoomCollider.enabled = true;

        roomLight.enabled = false;

        OpenDoor();
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();

        OpenDoor();

        SetDoorColliders(true);

        Debug.Log("Puzzle 3 concluído!");
    }

    public override void DisablePuzzle()
    {
        print("Puzzle 3 Disabled");

        puzzleButtons.ForEach(puzzleButton => { puzzleButton.Button?.gameObject.SetActive(false); });

        SetDoorColliders(false);

        SetDoorRotationImmediate(closedRotation);
    }

    public override void ResetPuzzle()
    {
        print("Puzzle 3 Reset");

        IsCompleted = false;

        ButtonsSequencerTemp.Clear();

        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null)
                continue;

            puzzleButton.Button.transform.eulerAngles = puzzleButton.DefaultRotation;
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < puzzleButtons.Count; i++)
        {
            Puzzle3ButtonState puzzleButton = puzzleButtons[i];

            if (puzzleButton.Button == null)
                continue;

            puzzleButton.DefaultRotation = puzzleButton.Button.transform.eulerAngles;

            puzzleButton.Button.OnMinLimitReached.RemoveAllListeners();
            puzzleButton.Button.OnMaxLimitReached.RemoveAllListeners();

            puzzleButton.Button.OnMinLimitReached.AddListener(() => OnButtonStateChanged(puzzleButton));

            puzzleButton.Button.OnMaxLimitReached.AddListener(() => OnButtonStateChanged(puzzleButton));

            puzzleButtons[i] = puzzleButton;
        }
    }

    private void OnButtonStateChanged(Puzzle3ButtonState buttonState)
    {
        if (IsCompleted)
            return;

        if (!CheckButtonsSequence(buttonState))
        {
            ResetPuzzle();
            return;
        }

        if (ButtonsSequencerTemp.Count == puzzleButtons.Count && CheckPuzzle())
            CompletePuzzle();
    }

    private bool CheckButtonsSequence(Puzzle3ButtonState buttonState)
    {
        int index = puzzleButtons.FindIndex(button => button.Button == buttonState.Button);

        if (index < 0)
            return false;

        // O botão precisa estar no estado correto.
        if (buttonState.Button.hingeJointState != buttonState.CorrectState)
            return false;

        // Não permite acionar novamente um botão já registrado.
        if (ButtonsSequencerTemp.Exists(button => button.Button == buttonState.Button))
            return false;

        int count = ButtonsSequencerTemp.Count;

        bool isValid = count switch
        {
            0 => index == 0,
            1 or 2 => index is 1 or 2,
            3 => index == 3,
            _ => false
        };

        if (!isValid)
            return false;

        ButtonsSequencerTemp.Add(buttonState);

        return true;
    }

    private bool CheckPuzzle()
    {
        if (puzzleButtons.Count == 0)
            return false;

        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null || puzzleButton.Button.hingeJointState != puzzleButton.CorrectState)
                return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (doorInteractable != null)
            LeanTween.cancel(doorInteractable.gameObject);

        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null)
                continue;

            puzzleButton.Button.OnMinLimitReached.RemoveAllListeners();
            puzzleButton.Button.OnMaxLimitReached.RemoveAllListeners();
        }
    }

    public void OnPlayerEnteredRoom()
    {
        SetDoorColliders(false);

        if (EnterRoomCollider != null)
            Destroy(EnterRoomCollider);

        CloseDoor();
    }

    #region Door

    private void SetDoorColliders(bool enabled)
    {
        foreach (Collider collider in doorInteractable.colliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    private void SetDoorRotationImmediate(Vector3 rotation)
    {
        LeanTween.cancel(doorInteractable.gameObject);

        doorRigidbody.isKinematic = true;

        doorRigidbody.linearVelocity = Vector3.zero;
        doorRigidbody.angularVelocity = Vector3.zero;

        doorInteractable.transform.localRotation = Quaternion.Euler(rotation);

        Physics.SyncTransforms();

        doorRigidbody.isKinematic = originalKinematicState;
    }

    private void OpenDoor()
    {
        AnimateDoor(openedRotation, openDuration, LeanTweenType.easeOutCubic);
    }

    private void CloseDoor()
    {
        AnimateDoor(closedRotation, closeDuration, LeanTweenType.linear);
    }

    private void AnimateDoor(Vector3 targetRotation, float duration, LeanTweenType easeType)
    {
        GameObject door = doorInteractable.gameObject;

        LeanTween.cancel(door);

        doorRigidbody.isKinematic = true;

        doorRigidbody.linearVelocity = Vector3.zero;
        doorRigidbody.angularVelocity = Vector3.zero;

        LeanTween.rotateLocal(door, targetRotation, duration).setEase(easeType)
        .setOnComplete(() =>
        {
            door.transform.localRotation = Quaternion.Euler(targetRotation);

            Physics.SyncTransforms();

            doorRigidbody.linearVelocity = Vector3.zero;
            doorRigidbody.angularVelocity = Vector3.zero;

            doorRigidbody.isKinematic = originalKinematicState; 
        });
    }

    #endregion
}