using System;
using System.Collections.Generic;

/// <summary>
/// 10x40 Tetris playfield (20 visible + 20 buffer). Row 0 is bottom.
/// </summary>
public class TetrisBoard
{
    public int Width { get; private set; }
    public int TotalHeight { get; private set; }
    public int VisibleHeight { get; private set; }

    private readonly TetrominoType?[,] _cells;

    public TetrisBoard(int width = TetrisGuideline.VisibleWidth,
                       int visibleHeight = TetrisGuideline.VisibleHeight,
                       int bufferHeight = TetrisGuideline.BufferHeight)
    {
        Width = width;
        VisibleHeight = visibleHeight;
        TotalHeight = visibleHeight + bufferHeight;
        _cells = new TetrominoType?[Width, TotalHeight];
    }

    public TetrominoType? GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= TotalHeight)
            return null;
        return _cells[x, y];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < TotalHeight;
    }

    public bool IsOccupied(int x, int y)
    {
        if (x < 0 || x >= Width) return true; // walls
        if (y < 0) return true; // floor
        if (y >= TotalHeight) return false; // above buffer allowed during movement
        return _cells[x, y].HasValue;
    }

    public bool CanPlace(Tetromino piece)
    {
        foreach (var c in piece.GetCells())
        {
            if (IsOccupied(c.X, c.Y))
                return false;
        }
        return true;
    }

    public bool CanPlace(TetrominoType type, RotationState rotation, GridPosition origin)
    {
        foreach (var c in Tetromino.GetCells(type, rotation, origin))
        {
            if (IsOccupied(c.X, c.Y))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Locks piece into the board. Returns lines cleared count and whether top-out condition applies.
    /// </summary>
    public LockResult LockPiece(Tetromino piece)
    {
        bool lockedCompletelyAboveVisible = true;
        bool spawnedOverlap = false;
        foreach (var c in piece.GetCells())
        {
            if (c.Y >= 0 && c.Y < TotalHeight && c.X >= 0 && c.X < Width)
            {
                if (_cells[c.X, c.Y].HasValue)
                    spawnedOverlap = true; // shouldn't happen if validated, but detect
                _cells[c.X, c.Y] = piece.Type;
            }
            if (c.Y < VisibleHeight)
                lockedCompletelyAboveVisible = false;
        }

        int cleared = ClearLines();
        return new LockResult
        {
            LinesCleared = cleared,
            LockedCompletelyAboveVisible = lockedCompletelyAboveVisible,
            OverlapDetected = spawnedOverlap
        };
    }

    public int ClearLines()
    {
        int cleared = 0;
        for (int y = 0; y < TotalHeight; y++)
        {
            if (IsLineFull(y))
            {
                RemoveLine(y);
                cleared++;
                y--; // recheck same row index after collapse
            }
        }
        return cleared;
    }

    private bool IsLineFull(int y)
    {
        for (int x = 0; x < Width; x++)
        {
            if (!_cells[x, y].HasValue)
                return false;
        }
        return true;
    }

    private void RemoveLine(int y)
    {
        for (int row = y; row < TotalHeight - 1; row++)
        {
            for (int x = 0; x < Width; x++)
                _cells[x, row] = _cells[x, row + 1];
        }
        for (int x = 0; x < Width; x++)
            _cells[x, TotalHeight - 1] = null;
    }

    public int GetGhostY(Tetromino piece)
    {
        int ghostY = piece.Position.Y;
        while (CanPlace(piece.Type, piece.Rotation, new GridPosition(piece.Position.X, ghostY - 1)))
            ghostY--;
        return ghostY;
    }

    /// <summary>
    /// 3-corner T-spin detection helper: counts occupied corners around T center after rotation/lock context.
    /// </summary>
    public int CountOccupiedCorners(GridPosition center)
    {
        // T center in local coords depends on rotation; easier to compute from piece position+rotation.
        // Corners around center cell (center.X, center.Y): (±1,±1)
        int count = 0;
        var corners = new[]
        {
            new GridPosition(center.X - 1, center.Y - 1),
            new GridPosition(center.X + 1, center.Y - 1),
            new GridPosition(center.X - 1, center.Y + 1),
            new GridPosition(center.X + 1, center.Y + 1),
        };
        foreach (var p in corners)
        {
            if (IsOccupied(p.X, p.Y)) count++;
        }
        return count;
    }

    public GridPosition GetTCenter(Tetromino piece)
    {
        // For our T state definitions, the "center" of rotation/pivot in local space is (1,2).
        return piece.Position + new GridPosition(1, 2);
    }
}

public class LockResult
{
    public int LinesCleared { get; set; }
    public bool LockedCompletelyAboveVisible { get; set; }
    public bool OverlapDetected { get; set; }
}
