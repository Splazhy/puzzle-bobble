using System;

namespace PuzzleBobble.HexGrid;

public struct OffsetCoord
{
    public const int ODD = -1;
    public readonly int col, row;
    public OffsetCoord(int col, int row)
    {
        this.col = col;
        this.row = row;
    }

    public Hex ToHex()
    {
        int q = col - (row + ODD * (row & 1)) / 2;
        int r = row;
        return new Hex(q, r);
    }

    public OffsetCoord Max(OffsetCoord other)
    {
        return new OffsetCoord(Math.Max(col, other.col), Math.Max(row, other.row));
    }

    public OffsetCoord Min(OffsetCoord other)
    {
        return new OffsetCoord(Math.Min(col, other.col), Math.Min(row, other.row));
    }
}