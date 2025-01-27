using System;
using System.Diagnostics;

namespace PuzzleBobble.HexGrid;

public readonly struct HexFrac
{
    public readonly double q;
    public readonly double r;
    public readonly double s;

    public HexFrac(double q, double r, double s)
    {
        this.q = q;
        this.r = r;
        this.s = s;
    }


    public override bool Equals(object? obj) => obj is HexFrac other && Equals(other);

    public bool Equals(HexFrac other) => q == other.q && r == other.r;
    public override int GetHashCode() => (q, r).GetHashCode();
    public static bool operator ==(HexFrac lhs, HexFrac rhs) => lhs.Equals(rhs);
    public static bool operator !=(HexFrac lhs, HexFrac rhs) => !(lhs == rhs);

    public static HexFrac operator +(HexFrac a, HexFrac b) => new(a.q + b.q, a.r + b.r, a.s + b.s);
    public static HexFrac operator -(HexFrac a, HexFrac b) => new(a.q - b.q, a.r - b.r, a.s - b.s);
    public static HexFrac operator *(HexFrac a, int k) => new(a.q * k, a.r * k, a.s * k);


    static readonly HexFrac[] directions = [
        new(1,0,-1),  new(1, -1, 0), new(0, -1, 1),
        new(-1, 0, 1), new(-1, 1, 0), new(0, 1, -1)
    ];

    public static HexFrac Direction(int direction)
    {
        Debug.Assert(0 <= direction && direction < 6);
        return directions[direction];
    }

    public HexFrac Neighbor(int direction) => this + Direction(direction);

    public Hex Round()
    {
        int q = (int)Math.Round(this.q);
        int r = (int)Math.Round(this.r);
        int s = (int)Math.Round(this.s);
        double q_diff = Math.Abs(q - this.q);
        double r_diff = Math.Abs(r - this.r);
        double s_diff = Math.Abs(s - this.s);
        if (q_diff > r_diff && q_diff > s_diff)
        {
            q = -r - s;
        }
        else if (r_diff > s_diff)
        {
            r = -q - s;
        }
        else
        {
            s = -q - r;
        }
        return new Hex(q, r, s);
    }
}