using System.Collections.Generic;
using UnityEngine;

public class GlobalPuzzleManager : MonoBehaviour
{
    [SerializeField] private List<BasePuzzle> PuzzleManagers = new();

    private int CurrentPuzzleIndex = 0;

    public BasePuzzle CurrentPuzzle => PuzzleManagers[CurrentPuzzleIndex];
    
    void Awake()
    {
        foreach (BasePuzzle puzzle in PuzzleManagers)
        {
            if (puzzle == null)
                PuzzleManagers.Remove(puzzle);
        }

        SwitchPuzzle(0);
    }

    void Start()
    {
        print("Puzzle Start: " + CurrentPuzzle);
    }

    private void SwitchPuzzle(int index)
    {
        if (index < 0 || index >= PuzzleManagers.Count)
            return;

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
}
