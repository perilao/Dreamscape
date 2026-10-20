using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Puzzle2Manager : BasePuzzle
{
    [Header("Picture Sockets")]
    [SerializeField] private List<PictureSocket> pictureSockets = new();

    [Header("Room")]
    [SerializeField] private EnterTriggerCollision EnterRoomCollider;

    [Header("Door")]
    [SerializeField] private InteractableDoor doorInteractable;

    [Header("Room Game Objects")]
    [SerializeField] private GameObject bed;
    [SerializeField] private GameObject window;
    [SerializeField] private GameObject wardrobe;
    [SerializeField] private Light roomLight;
    [SerializeField] private GameObject painting1;
    [SerializeField] private GameObject painting2;
    [SerializeField] private GameObject painting3;
    [SerializeField] private GameObject painting4;

    [Header("Puzzle Room Setup")]
    [SerializeField] private Transform bedTransform;
    [SerializeField] private Transform windowTransform;
    [SerializeField] private Transform wardrobeTransform;
    [SerializeField] private Transform painting1Transform;
    [SerializeField] private Transform painting2Transform;
    [SerializeField] private Transform painting3Transform;
    [SerializeField] private Transform painting4Transform;
    
    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupPuzzleRoom();

        SetupPictureSockets();

        EnterRoomCollider.OnPlayerEnteredRoom.RemoveAllListeners();

        EnterRoomCollider.OnPlayerEnteredRoom.AddListener(OnPlayerEnteredRoom);

        EnterRoomCollider.gameObject.SetActive(true);
    }

    public override void ResetPuzzle()
    {
        IsCompleted = false;
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();

        Debug.Log("Puzzle 2 concluído!");

        
    }

    public override void DisablePuzzle()
    {
        foreach (PictureSocket socket in pictureSockets)
        {
            socket.gameObject.SetActive(false);
        }
    }

    private void SetupPictureSockets()
    {
        foreach (PictureSocket socket in pictureSockets)
        {
            socket.PicturePlaced -= OnPictureChanged;
            socket.PicturePlaced += OnPictureChanged;
        }
    }

    private void SetupPuzzleRoom()
    {
        // Set the positions, rotations and scales of the room objects based on the predefined transforms
        if (bed != null)
        {
            bed.transform.SetLocalPositionAndRotation(bedTransform.position, bedTransform.rotation);
            bed.transform.localScale = bedTransform.localScale;
            bed.GetComponent<BoxCollider>().enabled = false; 
        }
        if (window != null)
        {
            window.transform.SetLocalPositionAndRotation(windowTransform.position, windowTransform.rotation);
            window.transform.localScale = windowTransform.localScale;
        }
        if (wardrobe != null)
        {
            wardrobe.transform.SetLocalPositionAndRotation(wardrobeTransform.position, wardrobeTransform.rotation);
            wardrobe.transform.localScale = wardrobeTransform.localScale;
        }
        if (painting1 != null)
        {
            painting1.transform.SetLocalPositionAndRotation(painting1Transform.position, painting1Transform.rotation);
            painting1.transform.localScale = painting1Transform.localScale;
        }
        if (painting2 != null)
        {
            painting2.transform.SetLocalPositionAndRotation(painting2Transform.position, painting2Transform.rotation);
            painting2.transform.localScale = painting2Transform.localScale;
        }
        if (painting3 != null)
        {
            painting3.transform.SetLocalPositionAndRotation(painting3Transform.position, painting3Transform.rotation);
            painting3.transform.localScale = painting3Transform.localScale;
        }
        if (painting4 != null)
        {
            painting4.transform.SetLocalPositionAndRotation(painting4Transform.position, painting4Transform.rotation);
            painting4.transform.localScale = painting4Transform.localScale;
        }
        if (roomLight != null)
        {
            roomLight.color = new (0.7056604f, 0f, 0.04969675f);
            roomLight.enabled = true;
        }
    }

    private void OnPictureChanged(PictureSocket initialSocket)
    {
        CheckPuzzleCompletion();
    }

    private void CheckPuzzleCompletion()
    {
        if (IsCompleted)
            return;

        if (CheckPuzzle())
            CompletePuzzle();
    }

    private bool CheckPuzzle()
    {
        if (pictureSockets.Count == 0)
            return false;

        foreach (PictureSocket socket in pictureSockets)
        {
            if (!socket.IsCorrect())
                return false;
        }

        return true;
    }

    public void OnPlayerEnteredRoom()
    {
        doorInteractable.SetDoorColliders(false);

        if (EnterRoomCollider != null)
            Destroy(EnterRoomCollider.gameObject);

        doorInteractable.CloseDoor();
    }
}
