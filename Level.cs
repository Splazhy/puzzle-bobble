using System.Collections.Generic;

namespace PuzzleBobble;

public class Level<T>
{
    public readonly Dictionary<Hex, T> Map;

    public Level(Dictionary<Hex, T> map)
    {
        Map = map;
    }

    public static Level<int> Load(string levelName)
    {
        string[] lines = System.IO.File.ReadAllLines($"Content/Levels/{levelName}.txt");
        Dictionary<Hex, int> map = [];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] cells = lines[y].Trim().Split(' ');
            for (int x = 0; x < cells.Length; x++)
            {
                OffsetCoord offset = new OffsetCoord(x, y);
                Hex hex = offset.ToHex();
                int value = cells[x] switch
                {
                    "." => 0,
                    _ => (int)cells[x][0] - 96
                };
                map[hex] = value;
            }
        }
        return new Level<int>(map);
    }
}