using System;
using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<BallData> Map;

    private static readonly Random _rand = new();

    public Level(HexMap<BallData> map)
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
        HexMap<BallData> map = [];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] cells = lines[y].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < cells.Length; x++)
            {
                OffsetCoord offset = new(x, y);
                Hex hex = offset.ToHex();
                BallData? value = cells[x] switch
                {
                    "." => null,
                    // TODO
                    _ => BallData.FromCode(cells[x]),
                };
                map[hex] = value;
            }
        }
        Debug.Assert(map.MinOffsetCoord != null);
        Debug.Assert(map.MaxOffsetCoord != null);
        map.Constraint = HexMap<BallData>.Constraints.Rectangular(
            0,
            null,
            7,
            null,
            true
        );
        return new Level(map);
    }

    public static Level Generate(Random random)
    {
        var level = Load("3-4-connectHalf");
        for (int i = 0; i < 1; i++)
        {
            level.StackDown(Load("3-4-connectHalf"));
        }
        for (int i = 0; i < 10; i++)
        {
            level.StackUp(Load("3-4-connectHalf"));
        }
        return level;
    }

    public HexMap<BallData> ToHexRectMap()
    {
        return Map;
    }
}
