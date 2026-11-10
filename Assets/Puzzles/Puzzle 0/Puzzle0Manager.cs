using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.SceneManagement;

public class Puzzle0Manager : BasePuzzle
{
    [Header("Picture Sockets")]
    [SerializeField] private List<PictureSocket> pictureSockets = new();

    [Header("Áudio")]
    [SerializeField] private AudioSource music;

    public override void InitializePuzzle()
    {
        base.InitializePuzzle();

        SetupPuzzleRoom();

        SetupPictures();

        music.Play();

        foreach (PictureSocket socket in pictureSockets)
        {
            socket.gameObject.SetActive(true);
        }

    }

    public override void ResetPuzzle()
    {
        IsCompleted = false;
    }

    public override void CompletePuzzle()
    {
        base.CompletePuzzle();
        SceneTransition.LoadScene(2);

        Debug.Log("Puzzle 2 concluído!");
    }


    private void SetupPictures()
    {
        foreach (PictureSocket socket in pictureSockets)
        {
            socket.PicturePlaced -= OnPictureChanged;
            socket.PicturePlaced += OnPictureChanged;
        }

        //InteractablePicture[] pictures = new[] { chess1, chess2, chess3, chess4 };

        //foreach (InteractablePicture picture in pictures)
        //{
         //   picture.GetComponent<BoxCollider>().enabled = true;
        //}
    }

    private void SetupPuzzleRoom()
    {
        
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

    }
}
