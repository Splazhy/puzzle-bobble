using System.Collections.Generic;
using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<int> Map;

    public Level(HexMap<int> map)
    {
        Map = map;
    }

    public static Level Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        HexMap<int> map = [];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] cells = lines[y].Trim().Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                int? value = cells[x] switch
                {
                    "." => null,
                    _ => (int)cells[x][0] - 96
                };
                map[hex] = value;
            }
        }
        Debug.Assert(map.minOffsetCoord != null);
        Debug.Assert(map.maxOffsetCoord != null);
        map.Constraint = HexMap<int>.Constraints.Rectangular(
            map.minOffsetCoord.Value.col,
            map.minOffsetCoord.Value.row,
            map.maxOffsetCoord.Value.col,
            null,
            true
        );
        return new Level(map);
    }

    public HexMap<int> ToHexRectMap()
    {
        return Map;
    }
}