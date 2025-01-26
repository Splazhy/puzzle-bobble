using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<Ball> Map;

    public Level(int rowCount, HexMap<Ball> map)
    {
        Map = map;
    }

public void StackDown(Level other)
    {
        Debug.Assert(other != this, "Cannot stack level with itself");

        int translateR = Map.MaxR + 1 - other.Map.MinR;

        foreach (var kv in other.Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;

            var offset = hex.ToOffsetCoord();
            Map[offset + new OffsetCoord(0, translateR)] = ball;
        }
    }

    public void StackUp(Level other)
    {
        Debug.Assert(other != this, "Cannot stack level with itself");

        int translateR = Map.MinR - other.Map.MaxR - 1;

        foreach (var kv in other.Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;

            var offset = hex.ToOffsetCoord();
            Map[offset + new OffsetCoord(0, translateR)] = ball;
        }
    }

    public static Level Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        HexMap<Ball> map = [];
        int rowCnt = lines.Length;
        for (int y = 0; y < lines.Length; y++)
        {
            string[] cells = lines[y].Trim().Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                Ball? value = cells[x] switch
                {
                    "." => null,
                    _ => new Ball((Ball.Color)cells[x][0] - 97, Ball.State.Idle)
                };
                map[hex] = value;
            }
        }
        Debug.Assert(map.MinOffsetCoord is not null);
        Debug.Assert(map.MaxOffsetCoord is not null);
        map.Constraint = HexMap<Ball>.Constraints.Rectangular(
            map.MinOffsetCoord.Value.Col,
            null,
            map.MaxOffsetCoord.Value.Col,
            null,
            true
        );
        return new Level(rowCnt, map);
    }

    public HexMap<Ball> ToHexRectMap()
    {
        return Map;
    }
}