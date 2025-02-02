using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using PuzzleBobble.HexGrid;
namespace PuzzleBobble;

public class Level
{
    public readonly HexMap<BallData> Map;

    private static readonly Random _rand = new();

    private static readonly Dictionary<string, Level> levels = [];
    private byte[]? _sourceTextHash;
    private bool _readonly;
    public readonly HashSet<int> BallColorsInLevel = [];
    public int BallCount => Map.Count;
    public int BallColorsCount => BallColorsInLevel.Count;

    /// <summary>
    /// create empty level
    /// </summary>
    public Level() : this(new HexMap<BallData>())
    {
    }
    public Level(HexMap<BallData> map)
    {
        Map = map;
        Map.Constraint = HexMap<BallData>.Constraints.Rectangular(
            0,
            null,
            7,
            null,
            true
        );

        foreach (var (_, ball) in Map)
        {
            if (ball.IsColor) BallColorsInLevel.Add(ball.value);
        }
    }

    private static Level CreateFromLines(string[] lines)
    {
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
        return new Level(map);
    }

    public static HexMap<BallData> CopyHexMap(HexMap<BallData> map)
    {
        HexMap<BallData> newMap = new();
        foreach (var kv in map)
        {
            newMap[kv.Key] = new(kv.Value);
        }
        newMap.Constraint = map.Constraint;
        return newMap;
    }

    public Level(Level other)
    {
        Map = CopyHexMap(other.Map);
        _sourceTextHash = other._sourceTextHash;
        BallColorsInLevel.UnionWith(other.BallColorsInLevel);
    }

    public void StackDown(Level other)
    {
        Debug.Assert(!_readonly, "Cannot modify readonly level. (make a clone first)");
        Debug.Assert(other != this, "Cannot stack level with itself");

        int translateR = (Map.MaxR == Map.MinR ? Map.MaxR : Map.MaxR + 1) - other.Map.MinR;

        foreach (var kv in other.Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;

            var offset = hex.ToOffsetCoord();
            Map[offset + new OffsetCoord(0, translateR)] = ball;
        }
        _sourceTextHash = null;
        BallColorsInLevel.UnionWith(other.BallColorsInLevel);
    }

    public void StackUp(Level other)
    {
        Debug.Assert(!_readonly, "Cannot modify readonly level. (make a clone first)");
        Debug.Assert(other != this, "Cannot stack level with itself");

        int translateR = Map.MinR - 1 - other.Map.MaxR;

        foreach (var kv in other.Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;

            var offset = hex.ToOffsetCoord();
            Map[offset + new OffsetCoord(0, translateR)] = ball;
        }
        _sourceTextHash = null;
        BallColorsInLevel.UnionWith(other.BallColorsInLevel);
    }

    public void ChangeColor(List<KeyValuePair<int, int>> changes)
    {
        Debug.Assert(!_readonly, "Cannot modify readonly level. (make a clone first)");

        foreach (var kv in Map)
        {
            var hex = kv.Key;
            var ball = kv.Value;

            foreach (var (from, to) in changes)
            {
                if (ball.value == from)
                {
                    Map[hex] = new BallData(to);
                }
            }
        }
        _sourceTextHash = null;

        foreach (var (from, to) in changes)
        {
            BallColorsInLevel.Remove(from);
            BallColorsInLevel.Add(to);
        }
    }

    public bool CheckStackUpConnection(Level other)
    {
        return other.CheckStackDownConnection(this);
    }

    public bool CheckStackDownConnection(Level other)
    {
        if (Map.Count == 0 || other.Map.Count == 0)
        {
            return true;
        }
        Debug.Assert((Map.MaxR & 1) != (other.Map.MinR & 1), "Cannot stack levels with same stagger on level boundary");
        int translateR = (Map.MaxR == Map.MinR ? Map.MaxR : Map.MaxR + 1) - other.Map.MinR;

        HexMap<BallData> testMap = new();
        Queue<Hex> bfsQueue = [];
        // take only the boundary between the two levels
        // assume the rest of the levels are connected
        for (int col = 0; col < 8; col++)
        {
            if (Map[new OffsetCoord(col, Map.MaxR)] is not null)
            {
                testMap[new OffsetCoord(col, Map.MaxR)] = new BallData(0);
                bfsQueue.Enqueue(new OffsetCoord(col, Map.MaxR).ToHex());
            }
            if (other.Map[new OffsetCoord(col, other.Map.MinR)] is not null)
            {
                testMap[new OffsetCoord(col, translateR + other.Map.MinR)] = new BallData(0); ;
            }
        }

        HashSet<Hex> connected = [];
        while (bfsQueue.Count > 0)
        {
            Hex hex = bfsQueue.Dequeue();
            connected.Add(hex);

            foreach (var neighbor in hex.Neighbors())
            {
                if (testMap[neighbor] is not null && !connected.Contains(neighbor))
                {
                    bfsQueue.Enqueue(neighbor);
                }
            }
        }

        return connected.Count == testMap.Count;
    }

    private static Level Load(string levelName)
    {
        string textContent = System.IO.File.ReadAllText($"Content/Levels/{levelName}.txt");
        byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(textContent));

        if (levels.TryGetValue(levelName, out var level))
        {
            Debug.Assert(level._sourceTextHash != null);
            if (level._sourceTextHash.SequenceEqual(hash))
            {
                return level;
            }
        }

        string[] lines = textContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var newLevel = CreateFromLines(lines);
        newLevel._sourceTextHash = hash;
        newLevel._readonly = true;
        levels[levelName] = newLevel;
        return newLevel;
    }

    private static void LoadAllToMemory()
    {
        string[] levelNames = System.IO.Directory.GetFiles("Content/Levels", "*.txt")
            .Select(System.IO.Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => !name.StartsWith("test"))
            .ToArray();
        foreach (var levelName in levelNames)
        {
            Load(levelName);
        }
    }

    public static Level Generate(Random random)
    {
        LoadAllToMemory();
        var levelNames = levels.Keys.Where(
            name => !name.StartsWith("test")
        ).ToArray();

        List<int> ballColorsUsed = [];
        while (ballColorsUsed.Count < 6)
        {
            var color = BallData.RandomColor(random);
            if (!ballColorsUsed.Contains(color))
            {
                ballColorsUsed.Add(color);
            }
        }

        var totalLevelPool = levels.Where(
            pair => !pair.Key.StartsWith("test")
        ).ToList();
        var usedLevelNames = new List<string>();

        Level level = new();
        while (level.BallCount < 300)
        {
            int usingTypeCount;
            if (level.BallCount < 50) { usingTypeCount = 3; }
            else if (level.BallCount < 150) { usingTypeCount = 4; }
            else if (level.BallCount < 250) { usingTypeCount = 5; }
            else { usingTypeCount = 6; }

            var lowLevelPool = totalLevelPool.Where(
                pair => pair.Value.BallColorsCount < usingTypeCount
            ).ToList();

            var mainLevelPool = totalLevelPool.Where(
                pair => pair.Value.BallColorsCount == usingTypeCount
            ).ToList();

            while (true)
            {

                var chosenPool = _rand.NextSingle() < 0.75 ? mainLevelPool : lowLevelPool;
                if (chosenPool.Count == 0)
                {
                    chosenPool = (chosenPool == mainLevelPool) ? lowLevelPool : mainLevelPool;
                    if (chosenPool.Count == 0) { throw new Exception("Could not generate level"); }
                }

                var poolIndex = random.Next(chosenPool.Count);
                var (levelName, newLevel) = chosenPool[poolIndex];
                chosenPool.RemoveAt(poolIndex);

                if (!level.CheckStackUpConnection(newLevel))
                {
                    continue;
                }

                usedLevelNames.Add(levelName);

                Level coloredLevel = new(newLevel);
                var availableColors = ballColorsUsed.Take(usingTypeCount).ToList();
                var colorChanges = new List<KeyValuePair<int, int>>();
                foreach (var color in newLevel.BallColorsInLevel)
                {
                    var newColor = availableColors[random.Next(availableColors.Count)];
                    colorChanges.Add(new KeyValuePair<int, int>(color, newColor));
                    availableColors.Remove(newColor);
                }
                coloredLevel.ChangeColor(colorChanges);

                level.StackUp(coloredLevel);
                break;
            }
        }

        Debug.WriteLine(string.Join(" + ", usedLevelNames));

        return level;
    }

    public HexMap<BallData> ToHexRectMap()
    {
        return CopyHexMap(Map);
    }
}
