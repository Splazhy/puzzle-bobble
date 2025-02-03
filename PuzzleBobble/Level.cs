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

    public void FlipHorizontal()
    {
        Debug.Assert(!_readonly, "Cannot modify readonly level. (make a clone first)");
        for (int row = Map.MinR; row <= Map.MaxR; row++)
        {
            var stagger = (row & 1) == 1;
            var rowLength = 8 - (stagger ? 1 : 0);
            for (int col = 0; col < rowLength / 2; col++)
            {
                var hex1 = new OffsetCoord(col, row).ToHex();
                var hex2 = new OffsetCoord(rowLength - 1 - col, row).ToHex();
                (Map[hex1], Map[hex2]) = (Map[hex2], Map[hex1]);
            }
        }
        _sourceTextHash = null;
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
        string textContent = Levels.GetLevel(levelName);
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
        List<string> levelNames = Levels.GetLevelNames()
            .Where(name => !name.StartsWith("test"))
            .ToList();
        foreach (var levelName in levelNames)
        {
            Load(levelName);
        }
    }

    private static readonly int TARGET_BALL_COUNT = 300;

    public static Level Generate(Random random)
    {
        LoadAllToMemory();
        var levelNames = levels.Keys.Where(
            name => !name.StartsWith("test")
        ).ToArray();

        List<int> ballColorsUsed = [];
        while (ballColorsUsed.Count < 7)
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
        while (level.BallCount < TARGET_BALL_COUNT)
        {
            var percentage = level.BallCount / (double)TARGET_BALL_COUNT;
            int usingTypeCount;
            if (percentage < 0.2) { usingTypeCount = 3; }
            else if (percentage < 0.5) { usingTypeCount = 4; }
            else if (percentage < 0.75) { usingTypeCount = 5; }
            else { usingTypeCount = 6; }

            var lowLevelPool = totalLevelPool.Where(
                pair => pair.Value.BallColorsCount < usingTypeCount
            ).ToList();

            var mainLevelPool = totalLevelPool.Where(
                pair => pair.Value.BallColorsCount == usingTypeCount
            ).ToList();

            var highLevelPool = totalLevelPool.Where(
                pair => usingTypeCount < pair.Value.BallColorsCount
            ).ToList();

            List<(List<KeyValuePair<string, Level>>, double)> availablePools = [
                (mainLevelPool, 3),
                (lowLevelPool, 1),
                (highLevelPool, 0.7),
            ];

            while (true)
            {
                availablePools = availablePools.Where(pair => 0 < pair.Item1.Count).ToList();
                if (availablePools.Count == 0)
                {
                    throw new Exception("Could not generate level");
                }

                var totalWeight = availablePools.Sum(pair => pair.Item2);
                var chosenSum = random.NextDouble() * totalWeight;
                var (chosenPool, _) = availablePools.First(
                    pair =>
                    {
                        chosenSum -= pair.Item2;
                        return chosenSum <= 0;
                    }
                );

                var poolIndex = random.Next(chosenPool.Count);
                var (levelName, newLevel) = chosenPool[poolIndex];
                chosenPool.RemoveAt(poolIndex);

                if (!level.CheckStackUpConnection(newLevel))
                {
                    continue;
                }

                usedLevelNames.Add(levelName);

                Level coloredLevel = new(newLevel);
                var availableColors = new List<int>();
                var colorChanges = new List<KeyValuePair<int, int>>();
                foreach (var color in newLevel.BallColorsInLevel)
                {
                    if (availableColors.Count == 0)
                    {
                        availableColors.AddRange(ballColorsUsed.Take(usingTypeCount));
                    }
                    var newI = random.Next(availableColors.Count);
                    var newColor = availableColors[newI];
                    availableColors.RemoveAt(newI);
                    colorChanges.Add(new KeyValuePair<int, int>(color, newColor));
                    availableColors.Remove(newColor);
                }
                coloredLevel.ChangeColor(colorChanges);

                if (random.NextDouble() < 0.5)
                {
                    coloredLevel.FlipHorizontal();
                }

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
