using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PuzzleBobble.HexGrid;

public readonly struct Hex
{
    public readonly int Q;
    public readonly int R;

    public Hex(int q_, int r_)
    {
        Q = q_;
        R = r_;
    }

    public Hex(int q_, int r_, int s_)
    {
        Debug.Assert(q_ + r_ + s_ == 0);
        Q = q_;
        R = r_;
    }

    public int S => -Q - R;

    public override bool Equals(object? obj) => obj is Hex other && Equals(other);

    public bool Equals(Hex other) => Q == other.Q && R == other.R;
    public override int GetHashCode() => (Q, R).GetHashCode();
    public static bool operator ==(Hex lhs, Hex rhs) => lhs.Equals(rhs);
    public static bool operator !=(Hex lhs, Hex rhs) => !(lhs == rhs);

    public static Hex operator +(Hex a, Hex b) => new(a.Q + b.Q, a.R + b.R, a.S + b.S);
    public static Hex operator -(Hex a, Hex b) => new(a.Q - b.Q, a.R - b.R, a.S - b.S);
    public static Hex operator *(Hex a, int k) => new(a.Q * k, a.R * k, a.S * k);

    public int Length() => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;
    public int Distance(Hex other)
    {
        return (this - other).Length();
    }

    private static readonly Hex[] directions = [
        new(1,0,-1),  new(1, -1, 0), new(0, -1, 1),
        new(-1, 0, 1), new(-1, 1, 0), new(0, 1, -1)
    ];

    public static Hex Direction(int direction)
    {
        Debug.Assert(0 <= direction && direction < 6);
        return directions[direction];
    }

    public OffsetCoord ToOffsetCoord()
    {
        int col = Q + (R + OffsetCoord.ODD * (R & 1)) / 2;
        int row = R;
        return new OffsetCoord(col, row);
    }

    public override string ToString() => $"Hex({Q}, {R}, {S})";

    public IEnumerable<Hex> Neighbors()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return this + Direction(i);
        }
    }

    public IEnumerable<Hex> HexesWithinRange(int range)
    {
        for (int q = -range; q <= range; q++)
        {
            for (int r = Math.Max(-range, -q - range); r <= Math.Min(range, -q + range); r++)
            {
                yield return this + new Hex(q, r);
            }
        }
    }
}