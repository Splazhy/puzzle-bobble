using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<Ball> Map;

    public Level(HexMap<Ball> map)
    {
        Map = map;
    }

    public static Level Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        HexMap<Ball> map = [];
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
        Debug.Assert(map.minOffsetCoord is not null);
        Debug.Assert(map.maxOffsetCoord is not null);
        map.Constraint = HexMap<Ball>.Constraints.Rectangular(
            map.minOffsetCoord.Value.col,
            map.minOffsetCoord.Value.row,
            map.maxOffsetCoord.Value.col,
            null,
            true
        );
        return new Level(map);
    }

    public HexMap<Ball> ToHexRectMap()
    {
        return Map;
    }
}