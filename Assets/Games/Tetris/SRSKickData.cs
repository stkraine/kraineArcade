/// <summary>
/// Super Rotation System (SRS) kick offset tests.
/// Offsets are (x,y) translations tested in order when a basic rotation collides.
/// </summary>
public static class SRSKickData
{
    // JLSTZ kick data (excluding O). Tests: (0,0) + 4 offsets per guideline/SRS.
    // Data format: [fromState][toStateIndex] -> list of (x,y). We index by transition.
    public static GridPosition[] GetKicks(TetrominoType type, RotationState from, RotationState to)
    {
        if (type == TetrominoType.O)
            return new[] { new GridPosition(0, 0) };
        if (type == TetrominoType.I)
            return GetIKicks(from, to);
        return GetJLSTZKicks(from, to);
    }

    private static GridPosition[] GetJLSTZKicks(RotationState from, RotationState to)
    {
        // Standard SRS JLSTZ kicks
        // 0->R: (0,0),(-1,0),(-1,+1),(0,-2),(-1,-2)
        // R->0: (0,0),(+1,0),(+1,-1),(0,+2),(+1,+2)
        // R->2: (0,0),(+1,0),(+1,-1),(0,+2),(+1,+2)
        // 2->R: (0,0),(-1,0),(-1,+1),(0,-2),(-1,-2)
        // 2->L: (0,0),(+1,0),(+1,+1),(0,-2),(+1,-2)
        // L->2: (0,0),(-1,0),(-1,-1),(0,+2),(-1,+2)
        // L->0: (0,0),(-1,0),(-1,-1),(0,+2),(-1,+2)
        // 0->L: (0,0),(+1,0),(+1,+1),(0,-2),(+1,-2)
        if (from == RotationState.North && to == RotationState.East)
            return K(0,0, -1,0, -1,1, 0,-2, -1,-2);
        if (from == RotationState.East && to == RotationState.North)
            return K(0,0, 1,0, 1,-1, 0,2, 1,2);
        if (from == RotationState.East && to == RotationState.South)
            return K(0,0, 1,0, 1,-1, 0,2, 1,2);
        if (from == RotationState.South && to == RotationState.East)
            return K(0,0, -1,0, -1,1, 0,-2, -1,-2);
        if (from == RotationState.South && to == RotationState.West)
            return K(0,0, 1,0, 1,1, 0,-2, 1,-2);
        if (from == RotationState.West && to == RotationState.South)
            return K(0,0, -1,0, -1,-1, 0,2, -1,2);
        if (from == RotationState.West && to == RotationState.North)
            return K(0,0, -1,0, -1,-1, 0,2, -1,2);
        if (from == RotationState.North && to == RotationState.West)
            return K(0,0, 1,0, 1,1, 0,-2, 1,-2);
        return new[] { new GridPosition(0, 0) };
    }

    private static GridPosition[] GetIKicks(RotationState from, RotationState to)
    {
        // Standard SRS I kicks
        if (from == RotationState.North && to == RotationState.East)
            return K(0,0, -2,0, 1,0, -2,-1, 1,2);
        if (from == RotationState.East && to == RotationState.North)
            return K(0,0, 2,0, -1,0, 2,1, -1,-2);
        if (from == RotationState.East && to == RotationState.South)
            return K(0,0, -1,0, 2,0, -1,2, 2,-1);
        if (from == RotationState.South && to == RotationState.East)
            return K(0,0, 1,0, -2,0, 1,-2, -2,1);
        if (from == RotationState.South && to == RotationState.West)
            return K(0,0, 2,0, -1,0, 2,1, -1,-2);
        if (from == RotationState.West && to == RotationState.South)
            return K(0,0, -2,0, 1,0, -2,-1, 1,2);
        if (from == RotationState.West && to == RotationState.North)
            return K(0,0, 1,0, -2,0, 1,-2, -2,1);
        if (from == RotationState.North && to == RotationState.West)
            return K(0,0, -1,0, 2,0, -1,2, 2,-1);
        return new[] { new GridPosition(0, 0) };
    }

    private static GridPosition[] K(int x0,int y0,int x1,int y1,int x2,int y2,int x3,int y3,int x4,int y4)
    {
        return new[]
        {
            new GridPosition(x0,y0), new GridPosition(x1,y1), new GridPosition(x2,y2),
            new GridPosition(x3,y3), new GridPosition(x4,y4)
        };
    }
}
