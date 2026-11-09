using System;

/// <summary>
/// Tetromino definitions with SRS rotation states.
/// Cell coordinates are relative to the piece origin in its local bounding box.
/// </summary>
public class Tetromino
{
    public TetrominoType Type { get; private set; }
    public RotationState Rotation { get; private set; }
    public GridPosition Position { get; set; }

    public Tetromino(TetrominoType type)
    {
        Type = type;
        Rotation = RotationState.North;
        Position = GetSpawnPosition(type);
    }

    public static GridPosition GetSpawnPosition(TetrominoType type)
    {
        // Guideline: appear on 21st/22nd rows (1-based from bottom of visible field), centered, rounded left.
        // Internal Y is 0-based from bottom; visible top is 19. Spawn uses rows 20-21 (buffer).
        // I and O have 4-wide bounding behavior; standard spawn column alignment:
        switch (type)
        {
            case TetrominoType.I: return new GridPosition(3, 20);
            case TetrominoType.O: return new GridPosition(4, 20);
            default: return new GridPosition(3, 20);
        }
    }

    public GridPosition[] GetCells()
    {
        return GetCells(Type, Rotation, Position);
    }

    public static GridPosition[] GetCells(TetrominoType type, RotationState rotation, GridPosition origin)
    {
        GridPosition[] local = GetLocalCells(type, rotation);
        var cells = new GridPosition[4];
        for (int i = 0; i < 4; i++)
            cells[i] = origin + local[i];
        return cells;
    }

    /// <summary>
    /// Local cell offsets for each piece/rotation (SRS states).
    /// </summary>
    public static GridPosition[] GetLocalCells(TetrominoType type, RotationState rotation)
    {
        switch (type)
        {
            case TetrominoType.I: return IStates[(int)rotation];
            case TetrominoType.O: return OStates[(int)rotation];
            case TetrominoType.T: return TStates[(int)rotation];
            case TetrominoType.S: return SStates[(int)rotation];
            case TetrominoType.Z: return ZStates[(int)rotation];
            case TetrominoType.J: return JStates[(int)rotation];
            case TetrominoType.L: return LStates[(int)rotation];
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public void RotateClockwise()
    {
        Rotation = (RotationState)(((int)Rotation + 1) % 4);
    }

    public void RotateCounterClockwise()
    {
        Rotation = (RotationState)(((int)Rotation + 3) % 4);
    }

    public Tetromino Clone()
    {
        return new Tetromino(Type) { Rotation = Rotation, Position = Position };
    }

    // SRS local states (4x4 bounding box, origin at bottom-left of box for simplicity)
    private static readonly GridPosition[][] IStates = new[]
    {
        new[] { new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(3,2) }, // N
        new[] { new GridPosition(2,0), new GridPosition(2,1), new GridPosition(2,2), new GridPosition(2,3) }, // E
        new[] { new GridPosition(0,1), new GridPosition(1,1), new GridPosition(2,1), new GridPosition(3,1) }, // S
        new[] { new GridPosition(1,0), new GridPosition(1,1), new GridPosition(1,2), new GridPosition(1,3) }, // W
    };

    private static readonly GridPosition[][] OStates = new[]
    {
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(1,2), new GridPosition(2,2) },
    };

    private static readonly GridPosition[][] TStates = new[]
    {
        new[] { new GridPosition(1,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2) }, // N: .T. / TTT
        new[] { new GridPosition(1,1), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(1,3) }, // E
        new[] { new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(1,3) }, // S
        new[] { new GridPosition(1,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(1,3) }, // W
    };

    private static readonly GridPosition[][] SStates = new[]
    {
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(0,2), new GridPosition(1,2) },
        new[] { new GridPosition(1,1), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(2,3) },
        new[] { new GridPosition(1,2), new GridPosition(2,2), new GridPosition(0,3), new GridPosition(1,3) },
        new[] { new GridPosition(0,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(1,3) },
    };

    private static readonly GridPosition[][] ZStates = new[]
    {
        new[] { new GridPosition(0,1), new GridPosition(1,1), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(2,1), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(1,3) },
        new[] { new GridPosition(0,2), new GridPosition(1,2), new GridPosition(1,3), new GridPosition(2,3) },
        new[] { new GridPosition(1,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(0,3) },
    };

    private static readonly GridPosition[][] JStates = new[]
    {
        new[] { new GridPosition(0,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(1,1), new GridPosition(2,1), new GridPosition(1,2), new GridPosition(1,3) },
        new[] { new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(2,3) },
        new[] { new GridPosition(1,1), new GridPosition(1,2), new GridPosition(0,3), new GridPosition(1,3) },
    };

    private static readonly GridPosition[][] LStates = new[]
    {
        new[] { new GridPosition(2,1), new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2) },
        new[] { new GridPosition(1,1), new GridPosition(1,2), new GridPosition(1,3), new GridPosition(2,3) },
        new[] { new GridPosition(0,2), new GridPosition(1,2), new GridPosition(2,2), new GridPosition(0,3) },
        new[] { new GridPosition(0,1), new GridPosition(1,1), new GridPosition(1,2), new GridPosition(1,3) },
    };
}
