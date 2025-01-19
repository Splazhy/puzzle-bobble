using System;
using System.Collections.Generic;
using System.Linq;
namespace PuzzleBobble.HexGrid;

public class HexMap<T> : IEnumerable<KeyValuePair<Hex, T>> where T : struct
    // sorry nullable reference types, too lazy. maybe later
{
    private Dictionary<Hex, T> _map = new Dictionary<Hex, T>();

    public OffsetCoord? minOffsetCoord { get; private set; }
    public OffsetCoord? maxOffsetCoord { get; private set; }

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
                if (left is int l && offset.col < l) return false;
                if (top is int t && offset.row < t) return false;
                if (bottom is int b && b < offset.row) return false;
                if (right is int r)
                {
                    if (r < offset.col) return false;

                    if (reduceRightByHalfHex && offset.col == r && offset.row % 2 == 1) return false;
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
                OffsetCoord offset = new OffsetCoord(x, y);
                this[offset] = rectanglularData[y, x];
            }
        }

        minOffsetCoord = new OffsetCoord(0, 0);
        maxOffsetCoord = new OffsetCoord(rectanglularData.GetLength(1) - 1, rectanglularData.GetLength(0) - 1);
    }

    public HexMap(Dictionary<Hex, T> map)
    {
        _map = map;
    }

    public bool IsHexInMap(Hex hex) => Constraint(hex);
    public Dictionary<Hex, T>.KeyCollection GetKeys() => _map.Keys;

    public T? this[Hex hex]
    {
        get
        {
            bool exist = _map.TryGetValue(hex, out T value);
            if (!exist) return null;
            return value;
        }
        set
        {
            if (value == null)
            {
                _map.Remove(hex);
                return;
            }
            _map[hex] = (T)value;
            ExpandBounds(hex.ToOffsetCoord());
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
        if (minOffsetCoord is OffsetCoord min && maxOffsetCoord is OffsetCoord max)
        {
            minOffsetCoord = min.Min(offset);
            maxOffsetCoord = max.Max(offset);
        }
        else
        {
            minOffsetCoord = offset;
            maxOffsetCoord = offset;
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
        if (minOffsetCoord is OffsetCoord min && maxOffsetCoord is OffsetCoord max)
        {
            for (int y = min.row; y <= max.row; y++)
            {
                for (int x = min.col; x <= max.col; x++)
                {
                    OffsetCoord offset = new OffsetCoord(x, y);
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
