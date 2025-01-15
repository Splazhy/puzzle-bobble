using System.Collections.Generic;

class HexRectMap<T> : IEnumerable<KeyValuePair<Hex, T>>
{
    private Dictionary<Hex, T> _map = new Dictionary<Hex, T>();


    private OffsetCoord maxOffset = new OffsetCoord(0, 0);

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

        maxOffset = new OffsetCoord(rectanglularData.GetLength(1) - 1, rectanglularData.GetLength(0) - 1);
    }

    public bool IsHexInMap(Hex hex) => _map.ContainsKey(hex);

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
        for (int y = 0; y <= maxOffset.row; y++)
        {
            for (int x = 0; x <= maxOffset.col; x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                yield return new KeyValuePair<Hex, T>(hex, _map[hex]);
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


}