using System.Collections.Generic;
using System.Linq;

class HexRectMap<T> : IEnumerable<KeyValuePair<Hex, T>>
{
    private Dictionary<Hex, T> _map = new Dictionary<Hex, T>();

    public HexRectMap(T[,] rectanglularData)
    {
        for (int y = 0; y < rectanglularData.GetLength(0); y++)
        {
            for (int x = 0; x < rectanglularData.GetLength(1); x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                _map[hex] = rectanglularData[y, x];
            }
        }
    }

    public HexRectMap(PuzzleBobble.Level<T> level)
    {
        _map = level.Map;
    }

    public bool IsHexInMap(Hex hex) => _map.ContainsKey(hex);
    public Dictionary<Hex, T>.KeyCollection GetKeys() => _map.Keys;

    public T this[Hex hex]
    {
        get => _map[hex];
        set => _map[hex] = value;
    }

    public T this[OffsetCoord offset]
    {
        get => this[offset.ToHex()];
        set => this[offset.ToHex()] = value;
    }

    public T this[int q, int r]
    {
        get => this[new Hex(q, r)];
        set => this[new Hex(q, r)] = value;
    }

    public IEnumerator<KeyValuePair<Hex, T>> GetEnumerator()
    {
        return _map.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
