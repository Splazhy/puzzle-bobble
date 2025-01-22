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
                OffsetCoord offset = new(x, y);
                Hex hex = offset.ToHex();
                BallData? value = cells[x] switch
                {
                    "." => null,
                    _ => new BallData((int)cells[x][0] - 97)
                };
                map[hex] = value;
            }
        }
        Debug.Assert(map.MinOffsetCoord != null);
        Debug.Assert(map.MaxOffsetCoord != null);
        map.Constraint = HexMap<BallData>.Constraints.Rectangular(
            map.MinOffsetCoord.Value.Col,
            map.MinOffsetCoord.Value.Row,
            map.MaxOffsetCoord.Value.Col,
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
