using System;
using System.Diagnostics;

public readonly struct Hex
{
    public readonly int q;
    public readonly int r;

    public Hex(int q_, int r_)
    {
        q = q_;
        r = r_;
    }

    public Hex(int q_, int r_, int s_)
    {
        Debug.Assert(q_ + r_ + s_ == 0);
        q = q_;
        r = r_;
    }

    public int s
    {
        get
        {
            return -q - r;
        }
    }

    public override bool Equals(object obj) => obj is Hex other && Equals(other);

    public bool Equals(Hex other) => q == other.q && r == other.r;
    public override int GetHashCode() => (q, r).GetHashCode();
    public static bool operator ==(Hex lhs, Hex rhs) => lhs.Equals(rhs);
    public static bool operator !=(Hex lhs, Hex rhs) => !(lhs == rhs);

    public static Hex operator +(Hex a, Hex b) => new(a.q + b.q, a.r + b.r, a.s + b.s);
    public static Hex operator -(Hex a, Hex b) => new(a.q - b.q, a.r - b.r, a.s - b.s);
    public static Hex operator *(Hex a, int k) => new(a.q * k, a.r * k, a.s * k);

    public int Length() => (Math.Abs(q) + Math.Abs(r) + Math.Abs(s)) / 2;
    public int Distance(Hex other)
    {
        return (this - other).Length();
    }

    static readonly Hex[] directions = [
        new(1,0,-1),  new(1, -1, 0), new(0, -1, 1),
        new(-1, 0, 1), new(-1, 1, 0), new(0, 1, -1)
    ];

    public static Hex Direction(int direction)
    {
        Debug.Assert(0 <= direction && direction < 6);
        return directions[direction];
    }

    public Hex Neighbor(int direction) => this + Direction(direction);

    public OffsetCoord ToOffsetCoord()
    {
        int col = q + (r + OffsetCoord.ODD * (r & 1)) / 2;
        int row = r;
        return new OffsetCoord(col, row);
    }
}