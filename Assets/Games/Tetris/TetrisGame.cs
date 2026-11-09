using System;
using System.Collections.Generic;

/// <summary>
/// Guideline-aligned Tetris game state machine (bringup): 7-bag, hold, SRS, lock delay,
/// ghost, line clears, scoring, levels, and basic T-spin/back-to-back/combo tracking.
/// </summary>
public class TetrisGame
{
    public TetrisBoard Board { get; private set; }
    public SevenBagRandomizer Randomizer { get; private set; }
    public Tetromino ActivePiece { get; private set; }
    public TetrominoType? HoldPiece { get; private set; }
    public bool HoldUsed { get; private set; }
    public bool IsGameOver { get; private set; }

    public int Score { get; private set; }
    public int Level { get; private set; }
    public int LinesClearedTotal { get; private set; }
    public int Combo { get; private set; }
    public bool BackToBackEligible { get; private set; }

    public float LockDelayTimer { get; private set; }
    public int LockResetCount { get; private set; }
    public bool LastActionWasRotation { get; private set; }
    public int LastKickIndex { get; private set; }

    public const int PreviewCount = 6;

    public TetrisGame(int? seed = null, int startLevel = 1)
    {
        Board = new TetrisBoard();
        Randomizer = new SevenBagRandomizer(seed);
        Level = Math.Max(1, startLevel);
        SpawnNext();
    }

    public IEnumerable<TetrominoType> GetPreview()
    {
        for (int i = 0; i < PreviewCount; i++)
            yield return Randomizer.Peek(i);
    }

    public bool SpawnNext()
    {
        HoldUsed = false;
        var type = Randomizer.Next();
        ActivePiece = new Tetromino(type);
        LockDelayTimer = 0f;
        LockResetCount = 0;
        LastActionWasRotation = false;
        LastKickIndex = 0;
        if (!Board.CanPlace(ActivePiece))
        {
            IsGameOver = true;
            return false;
        }
        return true;
    }

    public bool Move(int dx, int dy)
    {
        if (IsGameOver || ActivePiece == null) return false;
        var moved = ActivePiece.Clone();
        moved.Position = new GridPosition(moved.Position.X + dx, moved.Position.Y + dy);
        if (!Board.CanPlace(moved)) return false;
        ActivePiece = moved;
        LastActionWasRotation = false;
        OnSuccessfulMoveOrRotate();
        return true;
    }

    public bool SoftDrop()
    {
        if (Move(0, -1))
        {
            Score += 1; // guideline: soft drop scoring varies; common to award 1 per cell
            return true;
        }
        return false;
    }

    public int HardDrop()
    {
        if (IsGameOver || ActivePiece == null) return 0;
        int cells = 0;
        while (Move(0, -1)) cells++;
        Score += cells * 2;
        LockActivePiece();
        return cells;
    }

    public bool Rotate(bool clockwise)
    {
        if (IsGameOver || ActivePiece == null) return false;
        if (ActivePiece.Type == TetrominoType.O)
        {
            LastActionWasRotation = true;
            LastKickIndex = 0;
            return true; // O rotates in place
        }

        var from = ActivePiece.Rotation;
        var to = (RotationState)(((int)from + (clockwise ? 1 : 3)) % 4);
        var kicks = SRSKickData.GetKicks(ActivePiece.Type, from, to);
        for (int i = 0; i < kicks.Length; i++)
        {
            var origin = ActivePiece.Position + kicks[i];
            if (Board.CanPlace(ActivePiece.Type, to, origin))
            {
                ActivePiece.Rotation = to;
                ActivePiece.Position = origin;
                LastActionWasRotation = true;
                LastKickIndex = i;
                OnSuccessfulMoveOrRotate();
                return true;
            }
        }
        return false;
    }

    public bool Hold()
    {
        if (IsGameOver || ActivePiece == null || HoldUsed) return false;
        var current = ActivePiece.Type;
        if (HoldPiece.HasValue)
        {
            var swap = HoldPiece.Value;
            HoldPiece = current;
            ActivePiece = new Tetromino(swap);
        }
        else
        {
            HoldPiece = current;
            ActivePiece = new Tetromino(Randomizer.Next());
        }
        HoldUsed = true;
        LockDelayTimer = 0f;
        LockResetCount = 0;
        LastActionWasRotation = false;
        LastKickIndex = 0;
        if (!Board.CanPlace(ActivePiece))
        {
            IsGameOver = true;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Advance lock delay timer (seconds). When grounded and timer exceeds delay with move-reset rules, locks.
    /// </summary>
    public void Tick(float deltaSeconds)
    {
        if (IsGameOver || ActivePiece == null) return;
        if (IsGrounded())
        {
            LockDelayTimer += deltaSeconds;
            if (LockDelayTimer >= TetrisGuideline.LockDelaySeconds)
                LockActivePiece();
        }
        else
        {
            LockDelayTimer = 0f;
        }
    }

    public bool IsGrounded()
    {
        if (ActivePiece == null) return false;
        return !Board.CanPlace(ActivePiece.Type, ActivePiece.Rotation,
            new GridPosition(ActivePiece.Position.X, ActivePiece.Position.Y - 1));
    }

    private void OnSuccessfulMoveOrRotate()
    {
        // Move reset lock delay (15 moves) per guideline modern behavior
        if (IsGrounded() && LockResetCount < TetrisGuideline.MaxLockResets)
        {
            LockDelayTimer = 0f;
            LockResetCount++;
        }
    }

    private void LockActivePiece()
    {
        if (IsGameOver || ActivePiece == null) return;
        bool tspin = DetectTSpin();
        bool mini = tspin && DetectTSpinMini();
        var result = Board.LockPiece(ActivePiece);
        if (result.LockedCompletelyAboveVisible || result.OverlapDetected)
        {
            IsGameOver = true;
            ActivePiece = null;
            return;
        }
        ApplyScoring(result.LinesCleared, tspin, mini);
        SpawnNext();
    }

    private bool DetectTSpin()
    {
        if (ActivePiece == null || ActivePiece.Type != TetrominoType.T) return false;
        if (!LastActionWasRotation) return false;
        var center = Board.GetTCenter(ActivePiece);
        return Board.CountOccupiedCorners(center) >= 3;
    }

    private bool DetectTSpinMini()
    {
        // Simplified mini detection: if last kick was not the "TST" kick (index 4 in SRS for some transitions)
        // and front corners check differs. For bringup, treat non-5th-kick T-spins with 3 corners as potential mini;
        // refine later. Guideline uses pointing-side cells; this is a conservative bringup approximation.
        return LastKickIndex != 4 && ActivePiece != null;
    }

    private void ApplyScoring(int lines, bool tspin, bool mini)
    {
        if (lines > 0)
        {
            Combo++;
        }
        else
        {
            Combo = 0;
            if (tspin) Score += (mini ? 100 : 400) * Level; // T-spin no line
            return;
        }

        bool difficult = lines == 4 || tspin;
        int baseScore = 0;
        if (tspin)
        {
            if (mini)
                baseScore = lines == 1 ? 200 : 400; // mini single/double (approx)
            else
                baseScore = lines == 1 ? 800 : (lines == 2 ? 1200 : 1600);
        }
        else
        {
            baseScore = lines == 1 ? 100 : (lines == 2 ? 300 : (lines == 3 ? 500 : 800));
        }

        int add = baseScore * Level;
        if (BackToBackEligible && difficult)
            add = (int)(add * 1.5f);
        if (Combo > 1)
            add += 50 * (Combo - 1) * Level;

        Score += add;
        LinesClearedTotal += lines;
        BackToBackEligible = difficult;
        Level = 1 + (LinesClearedTotal / 10);
    }

    public int GetFallIntervalMs()
    {
        // Approximate guideline/Worlds-like curve for bringup (ms per row)
        int lvl = Math.Min(Level, 15);
        int[] ms = { 1000, 793, 618, 473, 355, 262, 190, 135, 94, 66, 45, 35, 28, 22, 17, 0 };
        return ms[lvl];
    }
}
