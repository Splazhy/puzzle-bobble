using System;

namespace PuzzleBobble.HexGrid;

public struct OffsetCoord
{
    public const int ODD = -1;
    public readonly int Col, Row;
    public OffsetCoord(int col, int row)
    {
        this.Col = col;
        this.Row = row;
    }

    public readonly Hex ToHex()
    {
        int q = Col - (Row + ODD * (Row & 1)) / 2;
        int r = Row;
        return new Hex(q, r);
    }

    public readonly OffsetCoord Max(OffsetCoord other)
    {
        return new OffsetCoord(Math.Max(Col, other.Col), Math.Max(Row, other.Row));
    }

    public readonly OffsetCoord Min(OffsetCoord other)
    {
        return new OffsetCoord(Math.Min(Col, other.Col), Math.Min(Row, other.Row));
    }

    public static OffsetCoord operator +(OffsetCoord a, OffsetCoord b) => new(a.Col + b.Col, a.Row + b.Row);
    public static OffsetCoord operator -(OffsetCoord a, OffsetCoord b) => new(a.Col - b.Col, a.Row - b.Row);
}