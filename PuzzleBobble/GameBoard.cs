using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;

public class GameBoard : GameObject
{
    // a packed grid of balls becomes a hexagon grid
    // https://www.redblobgames.com/grids/hexagons/
    public static readonly int HEX_INRADIUS = BallData.BALL_SIZE / 2;
    public static readonly int HEX_WIDTH = HEX_INRADIUS * 2;

    public static readonly double HEX_SIZE = HEX_WIDTH / Math.Sqrt(3);
    public static readonly double HEX_HEIGHT = HEX_SIZE * 2;
    public static readonly double HEX_VERTICAL_SPACING = HEX_HEIGHT * 0.75;

    public static readonly float DEFAULT_SPEED = 20.0f / 3;
    public static readonly float LERP_AMOUNT = 5.0f;
    public static readonly float EXPLODE_PUSHBACK_BONUS = -50.0f / 3;

    private int _topRow;
    public int TopRow
    {
        get
        {
            _topRow = Math.Min(_topRow, hexMap.MinR);
            return _topRow;
        }
    }

    bool IsInfinite;

    private readonly HexLayout hexLayout = new(
        HexOrientation.POINTY,
        new Vector2Double(HEX_SIZE, HEX_SIZE),
        new Vector2Double(HEX_INRADIUS * -7, HEX_SIZE) // -(HEX_WIDTH / 2 + HEX_WIDTH * 3) and HEX_HEIGHT / 2
    );

    public static readonly int BOARD_WIDTH_PX = HEX_WIDTH * 8;
    public static readonly int BOARD_HALF_WIDTH_PX = HEX_WIDTH * 4;


    private SoundEffect? settleSfx;
    private HexMap<BallData> hexMap = [];

    private BallData.Assets? _ballAssets;

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50f / 3;
    private const float EXPLOSION_SPREAD = 50f / 3;

    /// <summary>
    /// For decorative animations
    /// </summary>
    private readonly Random _decoRand = new();

    public GameBoard(Game game) : base("gameboard")
    {
        Position = new Vector2(0, -300f / 3);

        Velocity.Y = DEFAULT_SPEED;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _ballAssets = new BallData.Assets(content);

        var level = Level.Load("test-bombpass");
        // var level = Level.Load("3-4-connectHaft");
        // for (int i = 0; i < 1; i++)
        // {
        //     level.StackDown(Level.Load("3-4-connectHaft"));
        // }
        // for (int i = 0; i < 10; i++)
        // {
        //     level.StackUp(Level.Load("3-4-connectHaft"));
        // }
        hexMap = level.ToHexRectMap();

        foreach (var item in hexMap)
        {
            BallData ball = item.Value;
            ball.LoadAnimation(_ballAssets);
        }

        Position = new Vector2(0, (float)GetPreferredPos());

        // IsInfinite = true;

        settleSfx = content.Load<SoundEffect>("Audio/Sfx/glass_002");

        base.LoadContent(content);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // better than nothing I guess ( ͡° ͜ʖ ͡°)
        Debug.Assert(_ballAssets is not null, "Ball assets are not loaded.");

        foreach (var item in hexMap)
        {
            Hex hex = item.Key;
            BallData ball = item.Value;

            Vector2 p = hexLayout.HexToCenterPixel(hex).Downcast();
            ball.Draw(spriteBatch, gameTime, _ballAssets, ScreenPositionO(p));
        }

        DrawChildren(spriteBatch, gameTime);
    }

    private Hex ComputeClosestHexInner(Vector2 pos)
    {
        return hexLayout.PixelToHex(pos).Round();
    }

    private Vector2 ConvertHexToCenterInner(Hex hex)
    {
        return hexLayout.HexToCenterPixel(hex).Downcast();
    }

    public Hex ComputeClosestHex(Vector2 pos)
    {
        return ComputeClosestHexInner(ParentToSelfRelPos(pos));
    }

    public Vector2 ConvertHexToCenter(Hex hex)
    {
        return SelfToParentRelPos(ConvertHexToCenterInner(hex));
    }

    public bool IsValidHex(Hex hex)
    {
        return hexMap.IsHexInMap(hex);
    }

    public bool IsBallAt(Hex hex)
    {
        if (!IsValidHex(hex)) return false;
        return hexMap[hex].HasValue;
    }

    public bool IsBallSurronding(Hex hex)
    {
        foreach (Hex neighbor in hex.Neighbors())
        {
            if (IsBallAt(neighbor)) return true;
        }
        return false;
    }

    public void SetBallAt(Hex hex, BallData ball)
    {
        if (!IsValidHex(hex)) return;
        hexMap[hex] = ball;
        Debug.Assert(_ballAssets is not null);
        ball.LoadAnimation(_ballAssets);
    }

    /// <summary>
    /// search for connected balls of the same color, passing through rainbow balls
    /// 
    /// returns empty if the region is smaller than 3 balls, including rainbows
    /// </summary>
    /// <param name="sourceHex"></param>
    /// <param name="regionHexes">contains connected color and rainbow balls</param>
    /// <param name="connectedRainbows">contains only rainbow balls</param>
    /// <param name="connectedBombs">contains only adjacent bombs</param>
    private void ColorRegionSearch(Hex sourceHex, out HashSet<Hex> regionHexes, out HashSet<Hex> connectedRainbows, out HashSet<Hex> connectedBombs)
    {
        regionHexes = [];
        connectedRainbows = [];
        connectedBombs = [];
        if (!IsValidHex(sourceHex)) return;
        BallData? mapBall = hexMap[sourceHex];
        if (mapBall is null) return;

        BallData specifiedBall = mapBall.Value;

        Queue<Hex> pending = new();
        pending.Enqueue(sourceHex);

        while (0 < pending.Count)
        {
            Hex current = pending.Dequeue();
            regionHexes.Add(current);

            foreach (Hex neighbor in current.Neighbors())
            {
                if (hexMap[neighbor] == specifiedBall && !regionHexes.Contains(neighbor))
                {
                    pending.Enqueue(neighbor);
                }
                else if (hexMap[neighbor] is BallData data)
                {
                    if (data.IsRainbow)
                    {
                        connectedRainbows.Add(neighbor);
                        if (!regionHexes.Contains(neighbor))
                        {
                            pending.Enqueue(neighbor);
                        }
                    }
                    else if (data.IsBomb)
                    {
                        connectedBombs.Add(neighbor);
                    }
                }
            }
        }

        if (regionHexes.Count < 3)
        {
            regionHexes.Clear();
            connectedRainbows.Clear();
            connectedBombs.Clear();
            return;
        }
    }

    // why keyvaluepair instead of tuple: https://stackoverflow.com/a/40826656/3623350
    // although haven't benchmarked it yet
    public List<KeyValuePair<Vector2, BallData>> ExplodeBalls(Hex sourceHex)
    {
        if (!IsValidHex(sourceHex)) return [];
        BallData? mapBall = hexMap[sourceHex];
        if (mapBall is null) return [];

        // color pass
        HashSet<Hex> affected = [];
        Queue<Hex> bombs = [];
        Queue<Hex> pendingOrigins = new();
        pendingOrigins.Enqueue(sourceHex);

        while (0 < pendingOrigins.Count)
        {
            Hex current = pendingOrigins.Dequeue();
            if (hexMap[current] is not BallData currData) continue;
            if (currData.IsBomb || currData.IsStone) continue;

            if (currData.IsRainbow)
            {
                foreach (Hex neighbor in current.Neighbors())
                {
                    if (!affected.Contains(neighbor))
                    {
                        pendingOrigins.Enqueue(neighbor);
                    }
                }
            }
            if (currData.IsColor)
            {
                ColorRegionSearch(current, out HashSet<Hex> regionHexes, out HashSet<Hex> rainbows, out HashSet<Hex> moreBombs);
                affected.UnionWith(regionHexes);
                foreach (var item in rainbows) pendingOrigins.Enqueue(item);
                foreach (var item in moreBombs) bombs.Enqueue(item);
            }
        }

        // bomb pass
        while (0 < bombs.Count)
        {
            Hex current = bombs.Dequeue();
            if (hexMap[current] is not BallData currData) continue;
            Debug.Assert(currData.IsBomb);

            foreach (Hex inRange in current.HexesWithinRange(2))
            {
                if (hexMap[inRange] is BallData data)
                {
                    if (data.IsBomb)
                    {
                        if (!affected.Contains(inRange))
                        {
                            bombs.Enqueue(inRange);
                        }
                    }
                    affected.Add(inRange);
                }
            }
        }

        if (affected.Count == 0)
        {
            return [];
        }

        List<KeyValuePair<Vector2, BallData>> explodingBalls = [];
        foreach (Hex hex in affected)
        {
            if (hexMap[hex] is BallData ball)
            {
                explodingBalls.Add(new KeyValuePair<Vector2, BallData>(ConvertHexToCenterInner(hex), ball));
                hexMap[hex] = null;
            }
        }

        Velocity.Y = EXPLODE_PUSHBACK_BONUS * explodingBalls.Count;

        return explodingBalls;
    }

    public List<KeyValuePair<Vector2, BallData>> RemoveFloatingBalls()
    {
        HashSet<Hex> floating = [];
        floating.UnionWith(hexMap.GetKeys());

        // No balls on the board
        if (floating.Count == 0) return [];

        Queue<Hex> bfsQueue = new();
        // Balls from the top row can't be floating
        foreach (var item in hexMap.Where(kv => kv.Key.R == TopRow))
        {
            Hex hex = item.Key;
            if (!IsBallAt(hex)) continue;
            bfsQueue.Enqueue(hex);
        }

        // Remove all connected balls from the floating set
        while (bfsQueue.Count > 0)
        {
            Hex current = bfsQueue.Dequeue();
            if (!floating.Contains(current)) continue;
            floating.Remove(current);

            foreach (var neighbor in current.Neighbors())
            {
                if (!IsBallAt(neighbor)) continue;
                bfsQueue.Enqueue(neighbor);
            }
        }

        if (floating.Count == 0) return [];

        List<KeyValuePair<Vector2, BallData>> fallingBalls = [];
        foreach (Hex hex in floating)
        {
            if (hexMap[hex] is BallData ball)
            {
                fallingBalls.Add(new KeyValuePair<Vector2, BallData>(ConvertHexToCenterInner(hex), ball));
                hexMap[hex] = null;
            }
        }

        return fallingBalls;
    }

    private double GetPreferredPos()
    {
        return (50f / 3) - GetBottomEdgePos();
    }

    private double GetBottomEdgePos()
    {
        return hexLayout.HexToCenterPixel(new Hex(0, hexMap.MaxR)).Y + BallData.BALL_SIZE / 2;
    }

    private double GetTopEdgePos()
    {
        return hexLayout.HexToCenterPixel(new Hex(0, TopRow)).Y - BallData.BALL_SIZE / 2;
    }

    public int GetMapBallCount()
    {
        return hexMap.Count;
    }

    public double GetMapBottomEdge()
    {
        return GetBottomEdgePos() + Position.Y;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float catchUpSpeed = (float)Math.Max(0, (GetPreferredPos() - Position.Y) / 4);
        Velocity.Y = float.Lerp(Velocity.Y, DEFAULT_SPEED + catchUpSpeed, LERP_AMOUNT * deltaTime);
        UpdatePosition(gameTime);

        if (_decoRand.NextSingle() < (gameTime.ElapsedGameTime.TotalSeconds / 7.5))
        {
            var topRandRow = ComputeClosestHex(new Vector2(0, -400f / 3)).R;
            var coord = new OffsetCoord(_decoRand.Next(0, 8), _decoRand.Next(topRandRow, hexMap.MaxR + 1));
            if (hexMap[coord] is BallData ball)
            {
                ball.PlayShineAnimation(gameTime);
            }
        }

        // PROOF OF CONCEPT
        // if (hexMap.MaxR - hexMap.MinR < 7)
        // {
        //     var l = new Level(hexMap);
        //     l.StackUp(Level.Load("3-4-connectHaft"));
        // }
        // END

        UpdateChildren(gameTime);

        var allBalls = children.OfType<Ball>();

        foreach (var ball in allBalls)
        {
            float right = BOARD_HALF_WIDTH_PX - HEX_INRADIUS;
            if (0 < ball.Velocity.X && right < ball.Position.X)
            {
                ball.BounceOverX(right);
            }
            float left = -BOARD_HALF_WIDTH_PX + HEX_INRADIUS;
            if (ball.Velocity.X < 0 && ball.Position.X < left)
            {
                ball.BounceOverX(left);
            }

            if (ball.GetState() == Ball.State.Falling)
            {
                if (400f / 3 < SelfToParentRelPos(ball.Position).Y)
                {
                    ball.Destroy();
                }
            }

            if (ball.GetState() == Ball.State.Stasis)
            {
                if (GetTopEdgePos() + BallData.BALL_SIZE / 2 < ball.Position.Y)
                {
                    ball.Unstasis();
                }
                continue;
            }

            if (!(ball.GetState() == Ball.State.Moving || ball.GetState() == Ball.State.Falling)) continue;

            if (IsInfinite && ball.GetState() == Ball.State.Moving && ball.Position.Y < GetTopEdgePos() + BallData.BALL_SIZE / 2)
            {
                ball.SetStasis();
                continue;
            }

            if (CheckBallCollisionInner(ball.Position, out Hex ballClosestHex))
            {
                SetBallAt(ballClosestHex, ball.Data);
                var explodingBalls = ExplodeBalls(ballClosestHex);
                var fallBalls = RemoveFloatingBalls();

                pendingChildren.AddRange(explodingBalls.ConvertAll(explodingBall =>
                {
                    var b = new Ball(explodingBall.Value, Ball.State.Exploding)
                    {
                        Position = explodingBall.Key,
                        // Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * EXPLOSION_SPREAD, (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * EXPLOSION_SPREAD)
                    };
                    return b;
                }));
                pendingChildren.AddRange(fallBalls.ConvertAll(fallingBall =>
                {
                    var b = new Ball(fallingBall.Value, Ball.State.Falling)
                    {
                        Position = fallingBall.Key,
                        Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD, (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD)
                    };
                    return b;
                }));

                if (explodingBalls.Count == 0)
                {
                    var data = hexMap[ballClosestHex];
                    Debug.Assert(data is not null && settleSfx is not null);
                    // Let this ball shine ( ◡̀_◡́)ᕤ
                    data.Value.PlayShineAnimation(gameTime);
                    settleSfx.Play();
                }

                ball.Destroy();
            }
        }
        ;

        UpdatePendingAndDestroyedChildren();
    }

    public bool CheckBallCollision(Vector2 ballPosition, out Hex ballClosestHex)
    {
        return CheckBallCollisionInner(ParentToSelfRelPos(ballPosition), out ballClosestHex);
    }

    private bool CheckBallCollisionInner(Vector2 ballPosition, out Hex ballClosestHex)
    {
        ballClosestHex = ComputeClosestHexInner(ballPosition);

        if (IsBallAt(ballClosestHex) || (!IsInfinite && ballClosestHex.R < TopRow))
        {
            return true;
        }

        // reduce the collision circle to be more forgiving to players
        Circle collisionCircle = new(ballPosition, BallData.BALL_SIZE / 2 * 0.8f);

        foreach (Hex neighborHex in ballClosestHex.Neighbors())
        {
            if (!IsBallAt(neighborHex) && (IsInfinite || TopRow <= neighborHex.R))
            {
                continue;
            }

            Vector2 neighborCenterPos = ConvertHexToCenterInner(neighborHex);
            Circle neighborCircle = new(neighborCenterPos, BallData.BALL_SIZE / 2);
            bool colliding = collisionCircle.Intersects(neighborCircle) > 0;

            if (colliding) return true;
        }
        return false;
    }

    public BallData.BallStats GetBallStats()
    {
        BallData.BallStats stats = new();
        for (int row = hexMap.MaxR; hexMap.MinR <= row && stats.Count < 25; row--)
        {
            for (int col = 0; col < 8 && stats.Count < 25; col++)
            {
                var bd = new OffsetCoord(col, row);
                if (hexMap[bd] is BallData ball)
                {
                    stats.Add(ball);
                }
            }
        }
        stats.Add(children.Concat(pendingChildren).OfType<Ball>().Where(ball =>
            ball.GetState() == Ball.State.Moving
        ).Select(ball => ball.Data).GetEnumerator());
        return stats;
    }

}
