using System;

/// <summary>
/// Core Tetris types and guideline-aligned constants.
/// </summary>
public enum TetrominoType
{
    I, O, T, S, Z, J, L
}

public enum RotationState
{
    /// <summary>Spawn orientation (flat side down for most pieces).</summary>
    North = 0,
    /// <summary>Clockwise rotation from spawn.</summary>
    East = 1,
    /// <summary>180 degrees.</summary>
    South = 2,
    /// <summary>Counter-clockwise rotation from spawn.</summary>
    West = 3
}

public struct GridPosition
{
    public int X;
    public int Y;

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static GridPosition operator +(GridPosition a, GridPosition b)
        => new GridPosition(a.X + b.X, a.Y + b.Y);

    public override string ToString() => $"({X},{Y})";
}

public static class TetrisGuideline
{
    public const int VisibleWidth = 10;
    public const int VisibleHeight = 20;
    public const int BufferHeight = 20;
    public const int TotalHeight = VisibleHeight + BufferHeight; // 40
    public const int SpawnX = 3; // left-aligned center for I/O handled by piece data; guideline: centered, rounded left
    public const int SpawnY = 21; // bottom of spawn rows in 0-based-from-bottom internal coords (visible rows 0-19)
    public const int MaxLockResets = 15;
    public const float LockDelaySeconds = 0.5f;

    /// <summary>
    /// Guideline piece colors (name mapping for rendering layers).
    /// </summary>
    public static string GetColorName(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return "LightBlue";
            case TetrominoType.J: return "DarkBlue";
            case TetrominoType.L: return "Orange";
            case TetrominoType.O: return "Yellow";
            case TetrominoType.S: return "Green";
            case TetrominoType.Z: return "Red";
            case TetrominoType.T: return "Magenta";
            default: return "Gray";
        }
    }
}
