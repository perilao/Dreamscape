using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Puzzle3Manager : BasePuzzle
{
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

    private Rigidbody doorRigidbody;
    private HingeJoint doorHingeJoint;

    private bool originalKinematicState;
    private bool isDoorAnimating;

    private void Awake()
    {
        doorRigidbody = doorInteractable.GetComponent<Rigidbody>();
        doorHingeJoint = doorInteractable.GetComponent<HingeJoint>();

        originalKinematicState = doorRigidbody.isKinematic;
    }

    private void Start()
    {
        if (GlobalPuzzleManager.CurrentPuzzle != this)
        {
            DisablePuzzle();

            SetDoorColliders(false);

            SetDoorRotationImmediate(closedRotation);
        }
    }

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupButtons();

        puzzleButtons.ForEach(puzzleButton =>
        {
            if (puzzleButton.Button != null)
                puzzleButton.Button.gameObject.SetActive(true);
        });

        SetDoorColliders(true);

        EnterRoomCollider.enabled = true;

        OpenDoor();
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();

        SetDoorColliders(true);

        Debug.Log("Puzzle 3 concluído!");
    }

    protected override void DisablePuzzle()
    {
        puzzleButtons.ForEach(puzzleButton =>
        {
            if (puzzleButton.Button != null)
                puzzleButton.Button.gameObject.SetActive(false);
        });
    }

    public override void ResetPuzzle()
    {
        IsCompleted = false;

        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null)
                continue;

            puzzleButton.Button.transform.eulerAngles =
                puzzleButton.DefaultRotation;
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < puzzleButtons.Count; i++)
        {
            Puzzle3ButtonState puzzleButton = puzzleButtons[i];

            if (puzzleButton.Button == null)
                continue;

            puzzleButton.DefaultRotation =
                puzzleButton.Button.transform.eulerAngles;

            puzzleButton.Button.OnMinLimitReached.RemoveListener(OnButtonStateChanged);
            puzzleButton.Button.OnMaxLimitReached.RemoveListener(OnButtonStateChanged);

            puzzleButton.Button.OnMinLimitReached.AddListener(OnButtonStateChanged);
            puzzleButton.Button.OnMaxLimitReached.AddListener(OnButtonStateChanged);

            puzzleButtons[i] = puzzleButton;
        }
    }

    private void OnButtonStateChanged()
    {
        if (CheckPuzzle())
            CompletePuzzle();
        else
            ResetPuzzle();
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

            puzzleButton.Button.OnMinLimitReached.RemoveListener(OnButtonStateChanged);
            puzzleButton.Button.OnMaxLimitReached.RemoveListener(OnButtonStateChanged);
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

        isDoorAnimating = false;

        doorRigidbody.isKinematic = true;

        doorRigidbody.linearVelocity = Vector3.zero;
        doorRigidbody.angularVelocity = Vector3.zero;

        doorInteractable.transform.localRotation = Quaternion.Euler(rotation);

        Physics.SyncTransforms();

        doorRigidbody.isKinematic = originalKinematicState;
    }

    private void OpenDoor()
    {
        AnimateDoor( openedRotation, openDuration, LeanTweenType.easeOutCubic );
    }

    private void CloseDoor()
    {
        AnimateDoor(closedRotation, closeDuration, LeanTweenType.linear);
    }

    private void AnimateDoor(Vector3 targetRotation, float duration, LeanTweenType easeType)
    {
        GameObject door = doorInteractable.gameObject;

        LeanTween.cancel(door);

        isDoorAnimating = true;

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

            isDoorAnimating = false;
        });
    }

    #endregion
}