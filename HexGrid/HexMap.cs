using System;
using System.Collections.Generic;
using System.Linq;
namespace PuzzleBobble.HexGrid;

public class HexMap<T> : IEnumerable<KeyValuePair<Hex, T>> where T : struct
    // sorry nullable reference types, too lazy. maybe later
    // ------------------------------------------------------------------
    // welcome nullable reference types :)
    // ------------------------------------------------------------------
    // deported
{
    private readonly Dictionary<Hex, T> _map = [];

    public OffsetCoord? MinOffsetCoord { get; private set; }
    public OffsetCoord? MaxOffsetCoord { get; private set; }

    public int MaxR { get; private set; }
    public int MinR { get; private set; }

    private readonly Dictionary<int, int> _rCounts = [];

    public Func<Hex, bool> Constraint = (hex) => true;

    public static class Constraints
    {
        /// <summary>
        /// Returns a constraint that allows hexes within a rectangular area, based on offset coordinates.
        /// Coordinates are inclusive.
        /// </summary>
        public static Func<Hex, bool> Rectangular(
            int? left,
            int? top,
            int? right,
            int? bottom,
            bool reduceRightByHalfHex = false
        )
        {
            return (Hex hex) =>
            {
                OffsetCoord offset = hex.ToOffsetCoord();
                if (left is int l && offset.Col < l) return false;
                if (top is int t && offset.Row < t) return false;
                if (bottom is int b && b < offset.Row) return false;
                if (right is int r)
                {
                    if (r < offset.Col) return false;

                    if (reduceRightByHalfHex && offset.Col == r && offset.Row % 2 == 1) return false;
                }
                return true;
            };
        }
    }

    public HexMap()
    {
    }

    public HexMap(T[,] rectanglularData)
    {
        for (int y = 0; y < rectanglularData.GetLength(0); y++)
        {
            for (int x = 0; x < rectanglularData.GetLength(1); x++)
            {
                OffsetCoord offset = new(x, y);
                this[offset] = rectanglularData[y, x];
            }
        }

        MinOffsetCoord = new OffsetCoord(0, 0);
        MaxOffsetCoord = new OffsetCoord(rectanglularData.GetLength(1) - 1, rectanglularData.GetLength(0) - 1);
    }

    public HexMap(Dictionary<Hex, T> map)
    {
        _map = map;
    }

    public bool IsHexInMap(Hex hex) => Constraint(hex);
    public Dictionary<Hex, T>.KeyCollection GetKeys() => _map.Keys;
    public Dictionary<Hex, T>.ValueCollection GetValues() => _map.Values;

    public T? this[Hex hex]
    {
        get
        {
            bool exist = _map.TryGetValue(hex, out T value);
            if (!exist) return default;
            return value;
        }
        set
        {
            bool existed = _map.ContainsKey(hex);
            if (value is null)
            {
                _map.Remove(hex);
                if (existed) DecreaseRowCount(hex.R);
                return;
            }
            _map[hex] = (T)value;
            ExpandBounds(hex.ToOffsetCoord());
            if (!existed) IncreaseRowCount(hex.R);
        }
    }

    public T? this[OffsetCoord offset]
    {
        get => this[offset.ToHex()];
        set => this[offset.ToHex()] = value;
    }

    public T? this[int q, int r]
    {
        get => this[new Hex(q, r)];
        set => this[new Hex(q, r)] = value;
    }

    private void ExpandBounds(OffsetCoord offset)
    {
        if (MinOffsetCoord is OffsetCoord min && MaxOffsetCoord is OffsetCoord max)
        {
            MinOffsetCoord = min.Min(offset);
            MaxOffsetCoord = max.Max(offset);
        }
        else
        {
            MinOffsetCoord = offset;
            MaxOffsetCoord = offset;
        }

    }

    private void DecreaseRowCount(int r)
    {
        bool exist = _rCounts.TryGetValue(r, out int count);
        if (!exist) return;
        if (1 < count)
        {
            _rCounts[r] = count - 1;
            return;
        }

        _rCounts.Remove(r);
        if (_rCounts.Count == 0)
        {
            return;
        }
        if (r == MaxR)
        {
            MaxR = _rCounts.Keys.Max();
        }
        if (r == MinR)
        {
            MinR = _rCounts.Keys.Min();
        }
    }

    private void IncreaseRowCount(int r)
    {
        bool exist = _rCounts.TryGetValue(r, out int count);
        if (exist)
        {
            _rCounts[r] = count + 1;
            return;
        }

        if (_rCounts.Count == 0)
        {
            _rCounts[r] = 1;
            MaxR = r;
            MinR = r;
            return;
        }

        _rCounts[r] = 1;
        if (MaxR < r)
        {
            MaxR = r;
        }
        if (r < MinR)
        {
            MinR = r;
        }
    }

    public IEnumerator<KeyValuePair<Hex, T>> GetEnumerator()
    {
        return _map.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<Hex, T>> RectangleEntries()
    {
        if (MinOffsetCoord is OffsetCoord min && MaxOffsetCoord is OffsetCoord max)
        {
            for (int y = min.Row; y <= max.Row; y++)
            {
                for (int x = min.Col; x <= max.Col; x++)
                {
                    OffsetCoord offset = new(x, y);
                    Hex hex = offset.ToHex();
                    yield return new KeyValuePair<Hex, T>(hex, _map[hex]);
                }
            }
        }
        else
        {
            yield break;
        }
    }

}
