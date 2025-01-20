using System.Collections.Generic;
using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<BallData> Map;

    public Level(HexMap<BallData> map)
    {
        Map = map;
    }

    public static Level Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        HexMap<BallData> map = [];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] cells = lines[y].Trim().Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                BallData? value = cells[x] switch
                {
                    "." => null,
                    _ => new BallData((int)cells[x][0] - 97)
                };
                map[hex] = value;
            }
        }
        Debug.Assert(map.minOffsetCoord != null);
        Debug.Assert(map.maxOffsetCoord != null);
        map.Constraint = HexMap<BallData>.Constraints.Rectangular(
            map.minOffsetCoord.Value.col,
            map.minOffsetCoord.Value.row,
            map.maxOffsetCoord.Value.col,
            null,
            true
        );
        return new Level(map);
    }

    public HexMap<BallData> ToHexRectMap()
    {
        return Map;
    }
}