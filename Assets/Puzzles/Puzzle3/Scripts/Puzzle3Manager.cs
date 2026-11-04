using System;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle3Manager : BasePuzzle
{
    [Serializable]
    struct Puzzle3ButtonState
    {
        [SerializeField] private PuzzleToggleButton button;
        [SerializeField] private PuzzleToggleButton.ButtonState correctState;

        [HideInInspector] public Vector3 DefaultRotation;

        public readonly PuzzleToggleButton Button => button;
        public readonly PuzzleToggleButton.ButtonState CorrectState => correctState;
    }

    [Header("Puzzle Buttons")]
    [SerializeField] private List<Puzzle3ButtonState> puzzleButtons = new();

    [SerializeField] private PuzzleToggleButton hotValveButton;

    [Header("Door")]
    [SerializeField] private InteractableDoor doorInteractable;

    [Header("Room")]
    [SerializeField] private EnterTriggerCollision EnterRoomCollider;

    [SerializeField] private List<Light> roomLight = new();

    [SerializeField] private Light directionalLight;

    [SerializeField] private GameObject mirrorReferences;

    private List<Puzzle3ButtonState> ButtonsSequencerTemp = new();

    [Header("Áudio")]
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource susurro;
    [SerializeField] private AudioSource openDoor;

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupButtons();

        SetupLight(false);

        puzzleButtons.ForEach(puzzleButton => { puzzleButton.Button?.gameObject.SetActive(true); });

        doorInteractable.SetDoorColliders(true);

        EnterRoomCollider.OnPlayerEnteredRoom.RemoveAllListeners();

        EnterRoomCollider.OnPlayerEnteredRoom.AddListener(OnPlayerEnteredRoom);

        EnterRoomCollider.gameObject.SetActive(true);

        hotValveButton.GetComponent<BoxCollider>().enabled = true;

        hotValveButton.StateChanged.RemoveAllListeners();
        hotValveButton.StateChanged.AddListener(OnHotValveButtonStateChanged);

        mirrorReferences.SetActive(false);

        doorInteractable.OpenDoor();

        music.Play();
        openDoor.Play();
        susurro.Play();
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();

        SetupLight(true);

        doorInteractable.OpenDoor();

        music.Stop();

        Debug.Log("Puzzle 3 concluído!");
    }

    public override void DisablePuzzle()
    {
        mirrorReferences.SetActive(false);

        puzzleButtons.ForEach(puzzleButton => { puzzleButton.Button?.gameObject.SetActive(false); });
    }

    public override void ResetPuzzle()
    {
        Debug.Log("Puzzle 3 Reset");

        IsCompleted = false;

        ButtonsSequencerTemp.Clear();

        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null)
                continue;

            puzzleButton.Button.ResetButton();
        }
    }

    private void SetupButtons()
    {
        ButtonsSequencerTemp.Clear();

        for (int i = 0; i < puzzleButtons.Count; i++)
        {
            Puzzle3ButtonState puzzleButton = puzzleButtons[i];

            if (puzzleButton.Button == null)
                continue;

            puzzleButton.DefaultRotation = puzzleButton.Button.transform.eulerAngles;

            puzzleButton.Button.StateChanged.RemoveAllListeners();

            puzzleButton.Button.StateChanged.AddListener( () => OnButtonStateChanged(puzzleButton) );

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

    private void OnHotValveButtonStateChanged()
    {
        if (hotValveButton.CurrentState == PuzzleToggleButton.ButtonState.Down)
        {
            hotValveButton.StateChanged.RemoveAllListeners();

            hotValveButton.GetComponent<BoxCollider>().enabled = false;

            hotValveButton.ForceRelease();

            mirrorReferences.SetActive(true);
        }
    }

    private bool CheckButtonsSequence(Puzzle3ButtonState buttonState)
    {
        int index = puzzleButtons.FindIndex(button => button.Button == buttonState.Button);

        if (index < 0)
            return false;

        // O botão precisa estar no estado correto.
        if (buttonState.Button.CurrentState != buttonState.CorrectState)
            return false;

        // Não permite acionar novamente um botão já registrado.
        if (ButtonsSequencerTemp.Exists(button => button.Button == buttonState.Button))
            return false;

        int count = ButtonsSequencerTemp.Count;

        bool isValid = count switch
        {
            0 or 3 => index is 0 or 3,
            1 or 2 => index is 1 or 2,
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
            if (puzzleButton.Button == null || puzzleButton.Button.CurrentState != puzzleButton.CorrectState)
                return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        foreach (Puzzle3ButtonState puzzleButton in puzzleButtons)
        {
            if (puzzleButton.Button == null)
                continue;

            puzzleButton.Button.StateChanged.RemoveAllListeners();
            puzzleButton.Button.StateChanged.RemoveAllListeners();
        }
    }

    public void OnPlayerEnteredRoom()
    {
        doorInteractable.SetDoorColliders(false);

        if (EnterRoomCollider != null)
            Destroy(EnterRoomCollider.gameObject);

        doorInteractable.CloseDoor();
    }

    private void SetupLight(bool IsEnabled)
    {
        foreach (Light lampada in roomLight)
        {
            lampada.enabled = IsEnabled;
        }

        directionalLight.enabled = false;
    }
}