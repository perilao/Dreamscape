using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.SceneManagement;

public class Puzzle2Manager : BasePuzzle
{
    [Header("Picture Sockets")]
    [SerializeField] private List<PictureSocket> pictureSockets = new();

    [Header("Room")]
    [SerializeField] private EnterTriggerCollision EnterRoomCollider;

    [Header("Door")]
    [SerializeField] private InteractableDoor doorInteractable;

    [Header("ExitDoor")]
    [SerializeField] private InteractableDoor exitdoorInteractable;

    [Header("Room Game Objects")]
    [SerializeField] private GameObject laptop;
    [SerializeField] private GameObject laptop_nightmare;
    [SerializeField] private GameObject bed;
    [SerializeField] private GameObject window;
    [SerializeField] private GameObject wardrobe;
    [SerializeField] private Light roomLight;
    [SerializeField] private Light roomLight2;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Light bathLight;
    [SerializeField] private GameObject painting1;
    [SerializeField] private GameObject painting2;
    [SerializeField] private GameObject painting3;
    [SerializeField] private GameObject painting4;
    [SerializeField] private InteractablePicture chess1;
    [SerializeField] private InteractablePicture chess2;
    [SerializeField] private InteractablePicture chess3;
    [SerializeField] private InteractablePicture chess4;

    [Header("Puzzle Room Setup")]
    [SerializeField] private Transform laptopTransform;
    [SerializeField] private Transform bedTransform;
    [SerializeField] private Transform windowTransform;
    [SerializeField] private Transform wardrobeTransform;
    [SerializeField] private GameObject painting1Transform;
    [SerializeField] private GameObject painting2Transform;
    [SerializeField] private GameObject painting3Transform;
    [SerializeField] private GameObject painting4Transform;

    [Header("Áudio")]
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource susurro;
    [SerializeField] private AudioSource susurrofinish;
    [SerializeField] private AudioSource openDoor;

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupPuzzleRoom();

        SetupPictures();

        music.Play();
        openDoor.Play();
        susurro.Play();

        foreach (PictureSocket socket in pictureSockets)
        {
            socket.gameObject.SetActive(true);
        }

        EnterRoomCollider.OnPlayerEnteredRoom.RemoveAllListeners();

        EnterRoomCollider.OnPlayerEnteredRoom.AddListener(OnPlayerEnteredRoom);

        EnterRoomCollider.gameObject.SetActive(true);

        laptop_nightmare.gameObject.SetActive(true);

        chess1.gameObject.SetActive(true);

        chess2.gameObject.SetActive(true);

        chess3.gameObject.SetActive(true);

        chess4.gameObject.SetActive(true);
    }

    public override void ResetPuzzle()
    {
        IsCompleted = false;
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();

        doorInteractable.SetDoorColliders(true);

        exitdoorInteractable.OpenDoor();

        openDoor.Play();

        susurrofinish.Play();

        Debug.Log("Puzzle 2 concluído!");
    }

    //public override void DisablePuzzle()
    //{
    //    foreach (PictureSocket socket in pictureSockets)
    //    {
    //socket.gameObject.SetActive(false);
    //}
    // }

    private void SetupPictures()
    {
        foreach (PictureSocket socket in pictureSockets)
        {
            socket.PicturePlaced -= OnPictureChanged;
            socket.PicturePlaced += OnPictureChanged;
        }

        InteractablePicture[] pictures = new[] { chess1, chess2, chess3, chess4 };

        foreach (InteractablePicture picture in pictures)
        {
            picture.GetComponent<BoxCollider>().enabled = true;
        }
    }

    private void SetupPuzzleRoom()
    {
        // Set the positions, rotations and scales of the room objects based on the predefined transforms

        painting1.SetActive(false);

        painting2.SetActive(false);

        painting3.SetActive(false);

        painting4.SetActive(false);

        painting1Transform.SetActive(true);

        painting2Transform.SetActive(true);

        painting3Transform.SetActive(true);

        painting4Transform.SetActive(true);

        if (laptop != null)
        {
            laptop.transform.SetLocalPositionAndRotation(laptopTransform.position, laptopTransform.rotation);
            laptop.transform.localScale = laptopTransform.localScale;
        }
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

        if (roomLight != null)
        {
            roomLight.color = new(0.7056604f, 0f, 0.04969675f);
            roomLight.enabled = true;
            roomLight.GetComponent<LightFlicker>().enabled = true;

            //roomLight2.color = new(0.7056604f, 0f, 0.04969675f);
            roomLight2.enabled = true;
            roomLight2.GetComponent<LightFlicker>().enabled = true;

            //directionalLight.color = new(0.7056604f, 0f, 0.04969675f);
            directionalLight.enabled = false;

            bathLight.enabled = false;
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
