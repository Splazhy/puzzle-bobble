using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public int TopRow { get; private set; } = 0;
    public int FarLeftCol { get; private set; } = 0;
    public int RowCount { get; private set; }

    public readonly HexMap<BallData> Map;

    public Level(int rowCount, HexMap<BallData> map)
    {
        RowCount = rowCount;
        Map = map;
    }

    public void Stack(Level level)
    {
        Debug.Assert(level != this, "Cannot stack level with itself");
        TopRow -= level.RowCount;
        FarLeftCol += level.RowCount / 2;
        RowCount += level.RowCount;
        foreach (var kv in level.Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;
            var newR = hex.R + TopRow;
            Map[new Hex(hex.Q + FarLeftCol, hex.R + TopRow)] = ball;
        }
    }

    public static Level Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        HexMap<BallData> map = [];
        int rowCnt = lines.Length;
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
            null,
            map.MaxOffsetCoord.Value.Col,
            null,
            true
        );
        return new Level(rowCnt, map);
    }

    public HexMap<BallData> ToHexRectMap()
    {
        return Map;
    }
}
