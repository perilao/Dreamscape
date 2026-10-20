using System;
using UnityEngine;

public abstract class BasePuzzle : MonoBehaviour
{
    public event Action<BasePuzzle> PuzzleInitialized;
    public event Action<BasePuzzle> PuzzleCompleted;

    [SerializeField] protected GlobalPuzzleManager GlobalPuzzleManager;

    public bool IsCompleted { get; protected set; } = false;

    public int PuzzleID { get; private set; }

    public virtual void InitializePuzzle()
    {
        IsCompleted = false;
        
        PuzzleInitialized?.Invoke(this);
    }

    public abstract void ResetPuzzle();

    public virtual void CompletePuzzle()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        PuzzleCompleted?.Invoke(this);

        DisablePuzzle();
    }

    public virtual void DisablePuzzle(){}
}
