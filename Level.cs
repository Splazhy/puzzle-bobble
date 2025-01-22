using System.Diagnostics;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public int TopRow {get; private set;} = 0;
    public int FarLeftCol {get; private set;} = 0;
    public int RowCount {get; private set;}
    public readonly HexMap<Ball> Map;

    public Level(int rowCount, HexMap<Ball> map)
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
            var newR = hex.r + TopRow;
            Map[new Hex(hex.q + FarLeftCol, hex.r + TopRow)] = ball;
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
        Debug.Assert(map.minOffsetCoord is not null);
        Debug.Assert(map.maxOffsetCoord is not null);
        map.Constraint = HexMap<Ball>.Constraints.Rectangular(
            map.minOffsetCoord.Value.col,
            null,
            map.maxOffsetCoord.Value.col,
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