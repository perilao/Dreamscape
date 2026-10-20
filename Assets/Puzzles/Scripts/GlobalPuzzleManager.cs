using System.Collections.Generic;
using UnityEngine;

public class GlobalPuzzleManager : MonoBehaviour
{
    public List<BasePuzzle> PuzzleManagers = new();

    private int CurrentPuzzleIndex = 0;

    public BasePuzzle CurrentPuzzle => PuzzleManagers[CurrentPuzzleIndex];

    void Awake()
    {
        PuzzleManagers.RemoveAll(puzzle => puzzle == null);
    }

    void Start()
    {
        if (PuzzleManagers.Count > 1)
        {
            for (int i = 1; i < PuzzleManagers.Count; i++)
            {
                PuzzleManagers[i].DisablePuzzle();
            }
        }

        SwitchPuzzle(0);
    }

    private void SwitchPuzzle(int index)
    {
        if (index < 0 || index >= PuzzleManagers.Count)
            QuitGame();

        CurrentPuzzle.PuzzleCompleted -= PuzzleCompleted;

        CurrentPuzzleIndex = index;

        CurrentPuzzle.InitializePuzzle();

        CurrentPuzzle.PuzzleCompleted += PuzzleCompleted;
    }

    private void PuzzleCompleted(BasePuzzle puzzle)
    {
        if (puzzle != CurrentPuzzle)
        {
            Debug.LogError("Puzzle completed is not the current puzzle.");
            return;
        }

        SwitchPuzzle(CurrentPuzzleIndex + 1);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
