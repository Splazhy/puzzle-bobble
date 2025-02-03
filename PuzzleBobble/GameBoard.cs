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
    public static readonly int BOARD_WIDTH = 8;

    private SpriteFont? _debugfont;
    private Texture2D? _topBorder;

    private int _topRow;
    public int TopRow
    {
        get
        {
            _topRow = Math.Min(_topRow, hexMap.MinR);
            return _topRow;
        }
    }

    public bool IsInfinite { get; private set; }

    private readonly HexLayout hexLayout = new(
        HexOrientation.POINTY,
        new Vector2Double(HEX_SIZE, HEX_SIZE),
        new Vector2Double(HEX_INRADIUS * (1 - BOARD_WIDTH), HEX_SIZE) // -(HEX_WIDTH / 2 + HEX_WIDTH * 3) and HEX_HEIGHT / 2
    );


    public static readonly int BOARD_WIDTH_PX = HEX_WIDTH * BOARD_WIDTH;
    public static readonly int BOARD_HALF_WIDTH_PX = HEX_WIDTH * BOARD_WIDTH / 2;


    private SoundEffect? settleSfx;
    private SoundEffect? bombFuseSfx;
    private HexMap<BallData> hexMap = [];
    private readonly HexMap<TimeSpan> bombStartTimes = [];
    private readonly HexMap<TimeSpan> powerUpStartTimes = [];

    private BallData.Assets? _ballAssets;

    public delegate void BoardChangedHandler();
    public event BoardChangedHandler? BoardChanged;

    public delegate void BallsObtainedHandler(IEnumerable<BallData> balls);
    public event BallsObtainedHandler? BallsObtained;


    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50f / 3;

    /// <summary>
    /// For decorative animations
    /// </summary>
    private readonly Random _decoRand = new();

    private GameState _state = GameState.Playing;

    public GameBoard(Game game) : base("gameboard")
    {
        Position = new Vector2(0, -300f / 3);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _ballAssets = new BallData.Assets(content);
        _debugfont = content.Load<SpriteFont>("Fonts/Arial24");

        var level = Level.Generate(new Random());
        hexMap = level.ToHexRectMap();
        _topRow = hexMap.MinR;
        bombStartTimes.Constraint = hexMap.Constraint;

        foreach (var item in hexMap)
        {
            BallData ball = item.Value;
            ball.LoadAnimation(_ballAssets);
        }

        Position = new Vector2(0, (float)GetPreferredPos() - 20);
        Velocity.Y = ComputePreferredSpeed();

        // IsInfinite = true;

        settleSfx = content.Load<SoundEffect>("Audio/Sfx/glass_002");
        bombFuseSfx = content.Load<SoundEffect>("Audio/Sfx/fuse");

        _topBorder = content.Load<Texture2D>("Graphics/border_top");

        base.LoadContent(content);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // better than nothing I guess ( ͡° ͜ʖ ͡°)
        Debug.Assert(_ballAssets is not null, "Ball assets are not loaded.");
        Debug.Assert(_topBorder is not null, "Top border is not loaded.");

        if (!IsInfinite)
        {
            spriteBatch.Draw(
                _topBorder,
                ScreenPositionO(new Vector2(0, (float)GetTopEdgePos())),
                null,
                Color.White,
                0,
                new Vector2(_topBorder.Width / 2, _topBorder.Height),
                PIXEL_SIZE,
                SpriteEffects.None,
                0
            );
        }

        if (DebugOptions.GAMEBOARD_DRAW_ENTIRE_GRID)
        {
            foreach (var (hex, ball) in hexMap)
            {
                Vector2 p = hexLayout.HexToCenterPixel(hex).Downcast();
                ball.Draw(spriteBatch, gameTime, _ballAssets, ScreenPositionO(p));
            }
        }
        else
        {
            var minDrawRow = Math.Max(TopRow, ComputeClosestHex(new Vector2(0, -150 - BallData.BALL_SIZE)).R);
            for (int row = hexMap.MaxR; minDrawRow <= row; row--)
            {
                for (int col = 0; col < 8; col++)
                {
                    var bd = new OffsetCoord(col, row);
                    var hex = bd.ToHex();
                    if (hexMap[hex] is BallData ball)
                    {
                        Vector2 p = hexLayout.HexToCenterPixel(hex).Downcast();
                        ball.Draw(spriteBatch, gameTime, _ballAssets, ScreenPositionO(p));
                    }
                }
            }
        }

        DrawChildren(spriteBatch, gameTime);

        if (DebugOptions.GAMEBOARD_SHOW_POSITIONS)
        {
            spriteBatch.DrawString(
                _debugfont,
                $"pos: {Position}\ndelta: {GetPreferredPos() - Position.Y}\nvel: {Velocity}",
                ScreenPositionO(new Vector2(0, (int)GetBottomEdgePos())),
                Color.White
            );
        }
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
    private List<KeyValuePair<Hex, BallData>> ExplodeBalls(Hex sourceHex, out Queue<Hex> bombs)
    {
        bombs = [];
        BallData? mapBall = hexMap[sourceHex];
        if (mapBall is null) return [];

        // color pass
        HashSet<Hex> affected = [];
        Queue<Hex> pendingOrigins = new();
        HashSet<Hex> doneOrigins = [];
        pendingOrigins.Enqueue(sourceHex);

        while (0 < pendingOrigins.Count)
        {
            Hex current = pendingOrigins.Dequeue();
            if (doneOrigins.Contains(current)) continue;
            doneOrigins.Add(current);
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
                // having rainbows recursively explode more regions is too OP
                // foreach (var item in rainbows) pendingOrigins.Enqueue(item);
                foreach (var item in moreBombs) bombs.Enqueue(item);
            }
        }

        List<KeyValuePair<Hex, BallData>> explodingBalls = TakeBallsFromMap(affected);
        Velocity.Y += ComputePushbackSpeed(explodingBalls.Count);
        return explodingBalls;
    }

    private List<KeyValuePair<Hex, BallData>> ExplodeBomb(Hex sourceHex)
    {
        BallData? mapBall = hexMap[sourceHex];
        if (mapBall is null) return [];
        Debug.Assert(mapBall.Value.IsBomb);

        HashSet<Hex> affected = [];
        Queue<Hex> bombs = [];
        bombs.Enqueue(sourceHex);

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

        List<KeyValuePair<Hex, BallData>> explodingBalls = TakeBallsFromMap(affected);
        Velocity.Y += ComputePushbackSpeed(explodingBalls.Count);
        return explodingBalls;
    }

    private List<KeyValuePair<Hex, BallData>> RemoveFloatingBalls()
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

        return TakeBallsFromMap(floating);
    }

    private List<KeyValuePair<Hex, BallData>> TakeBallsFromMap(HashSet<Hex> affected)
    {
        List<KeyValuePair<Hex, BallData>> takenBalls = [];
        foreach (Hex hex in affected)
        {
            if (hexMap[hex] is BallData ball)
            {
                takenBalls.Add(new KeyValuePair<Hex, BallData>(hex, ball));
                hexMap[hex] = null;
            }
        }

        return takenBalls;
    }

    private double GetPreferredPos()
    {
        return 8 - GetBottomEdgePos();
    }

    private double GetBottomEdgePos()
    {
        return hexLayout.HexToCenterPixel(new Hex(0, hexMap.MaxR)).Y + BallData.BALL_SIZE / 2;
    }

    private double GetTopEdgePos()
    {
        return hexLayout.HexToCenterPixel(new Hex(0, TopRow)).Y - BallData.BALL_SIZE / 2;
    }

    public double GetMapTopEdgePos()
    {
        return GetTopEdgePos() + Position.Y;
    }

    public int GetMapBallCount()
    {
        return hexMap.Count;
    }
    public double GetDistanceFromDeath()
    {
        return DEATH_Y_POS - GetBottomEdgePos() - Position.Y;
    }

    public static readonly int DEATH_Y_POS = 86;
    public static readonly float FINAL_SPEED = 2f;
    public static readonly float PUSHDOWN_SPEED = 4f;
    public static readonly float LERP_AMOUNT = 5.0f;

    private float ComputePushbackSpeed(int ballCount)
    {
        if (ballCount < 3) return 0;
        // higher ball count means more pushback, but not as much as linear
        return -48 * MathF.Log2(ballCount);
    }

    private float ComputePreferredSpeed()
    {
        if (_state != GameState.Playing) return 0;
        double prefDistance = Math.Max(0, GetPreferredPos() - Position.Y);
        double deathDistance = GetDistanceFromDeath();
        float catchUpSpeed = (float)Math.Max(0, Math.Pow(prefDistance / 10, 1.6));
        float pushDownSpeed = (float)Math.Min(Math.Max(0, deathDistance / 8), PUSHDOWN_SPEED);

        return FINAL_SPEED + pushDownSpeed + catchUpSpeed;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Velocity.Y = float.Lerp(Velocity.Y, ComputePreferredSpeed(), LERP_AMOUNT * deltaTime);
        UpdatePosition(gameTime);

        if (_decoRand.NextSingle() < (gameTime.ElapsedGameTime.TotalSeconds / 7.5))
        {
            var topRandRow = ComputeClosestHex(new Vector2(0, -400f / 3)).R;
            if (0 < hexMap.MaxR - topRandRow)
            {
                var coord = new OffsetCoord(_decoRand.Next(0, 8), _decoRand.Next(topRandRow, hexMap.MaxR + 1));
                if (hexMap[coord] is BallData ball && !ball.IsPlayingShineAnimation(gameTime))
                {
                    ball.PlayShineAnimation(gameTime);
                }
            }
        }
        foreach (var (hex, startTime) in powerUpStartTimes)
        {
            var ball = hexMap[hex];
            if (ball is BallData bd && !bd.IsPlayingShineAnimation(gameTime))
            {
                bd.PlayShineAnimation(gameTime);
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
            UpdateBall(gameTime, ball);
        }

        foreach (var item in bombStartTimes)
        {
            Hex hex = item.Key;
            TimeSpan startTime = item.Value;
            if (
                hexMap[hex] is BallData bomb &&
                startTime + TimeSpan.FromSeconds(1.5) < gameTime.TotalGameTime
            )
            {
                BoardChanged?.Invoke();
                var explodingBalls = ExplodeBomb(hex);
                var fallBalls = RemoveFloatingBalls();

                foreach (var item2 in explodingBalls.Concat(fallBalls))
                {
                    if (powerUpStartTimes[item2.Key] is not null)
                    {
                        PowerUpObtained?.Invoke();
                        powerUpStartTimes[item2.Key] = null;
                    }
                    bombStartTimes[item2.Key] = null;
                }

                BallsObtained?.Invoke(explodingBalls.Concat(fallBalls).Select(kv => kv.Value));

                pendingChildren.AddRange(explodingBalls.ConvertAll((explodingBall) =>
                {
                    var b = new Ball(explodingBall.Value, Ball.State.Exploding)
                    {
                        Position = ConvertHexToCenterInner(explodingBall.Key)
                    };
                    return b;
                }));
                pendingChildren.AddRange(fallBalls.ConvertAll(fallingBall =>
                {
                    var b = new Ball(fallingBall.Value, Ball.State.Falling)
                    {
                        Position = ConvertHexToCenterInner(fallingBall.Key),
                        Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD, (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD)
                    };
                    return b;
                }));
            }
        }

        foreach (var (hex, startTime) in powerUpStartTimes)
        {
            if (startTime + TimeSpan.FromSeconds(10) < gameTime.TotalGameTime)
            {
                powerUpStartTimes[hex] = null;
            }
        }

        UpdatePendingAndDestroyedChildren();
    }

    private void UpdateBall(GameTime gameTime, Ball ball)
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
            if (150 + BallData.BALL_SIZE / 2 < SelfToParentRelPos(ball.Position).Y)
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
            return;
        }

        if (!(ball.GetState() == Ball.State.Moving || ball.GetState() == Ball.State.Falling)) return;

        if (IsInfinite && ball.GetState() == Ball.State.Moving && ball.Position.Y < GetTopEdgePos() + BallData.BALL_SIZE / 2)
        {
            ball.SetStasis();
            return;
        }

        if (CheckBallCollisionInner(ball.Position, out Hex ballClosestHex))
        {
            BoardChanged?.Invoke();
            if (_state == GameState.Success)
            {
                ball.Destroy();
                pendingChildren.Add(new Ball(ball.Data, Ball.State.Exploding)
                {
                    Position = ConvertHexToCenterInner(ballClosestHex)
                });
                return;
            }

            if (_state == GameState.Fail)
            {
                SetBallAt(ballClosestHex, ball.Data);
                ball.Destroy();
                hexMap[ballClosestHex]?.PlayPetrifyAnimation(gameTime);
                return;
            }

            SetBallAt(ballClosestHex, ball.Data);
            ball.Destroy();
            var explodingBalls = ExplodeBalls(ballClosestHex, out Queue<Hex> bombs);
            var fallBalls = RemoveFloatingBalls();

            Debug.Assert(bombFuseSfx is not null);
            foreach (var hex in bombs)
            {
                if (hexMap[hex] is BallData bomb)
                {
                    Debug.Assert(bomb.IsBomb);
                    if (bombStartTimes[hex] is null)
                    {
                        bombStartTimes[hex] = gameTime.TotalGameTime;
                        bomb.PlayAltAnimation(gameTime);
                        bombFuseSfx.Play();
                    }
                }
            }

            foreach (var item in explodingBalls.Concat(fallBalls))
            {
                if (powerUpStartTimes[item.Key] is not null)
                {
                    PowerUpObtained?.Invoke();
                    powerUpStartTimes[item.Key] = null;
                }
                bombStartTimes[item.Key] = null;
            }

            BallsObtained?.Invoke(explodingBalls.Concat(fallBalls).Select(kv => kv.Value));

            pendingChildren.AddRange(explodingBalls.ConvertAll(explodingBall =>
            {
                var b = new Ball(explodingBall.Value, Ball.State.Exploding)
                {
                    Position = ConvertHexToCenterInner(explodingBall.Key),
                };
                return b;
            }));
            pendingChildren.AddRange(fallBalls.ConvertAll(fallingBall =>
            {
                var b = new Ball(fallingBall.Value, Ball.State.Falling)
                {
                    Position = ConvertHexToCenterInner(fallingBall.Key),
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
                if (data.Value.IsBomb)
                {
                    bombStartTimes[ballClosestHex] = gameTime.TotalGameTime;
                    data.Value.PlayAltAnimation(gameTime);
                    bombFuseSfx.Play();
                }
            }

        }
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

    public BallData.BallStats GetBallStats(bool lucky)
    {
        BallData.BallStats stats = new();
        for (int row = hexMap.MaxR; hexMap.MinR <= row && stats.ColorCount < 25 && (!lucky || stats.ColorCounts.Count < 2); row--)
        {
            for (int i = 0; i < 8 && stats.ColorCount < 25 && (!lucky || stats.ColorCounts.Count < 2); i++)
            {
                var col = (BOARD_WIDTH / 2) +
                    (i / 2) * (i % 2 == 0 ? 1 : -1) +
                    (i % 2 == 0 ? 0 : -1);
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

    public void Fail(GameTime gameTime)
    {
        _state = GameState.Fail;
        foreach (var item in hexMap)
        {
            item.Value.PlayPetrifyAnimation(gameTime);
        }
    }

    public void Success()
    {
        _state = GameState.Success;
    }

    public void PlacePowerUp(Random pwupRand, GameTime gameTime)
    {
        if (hexMap.Count == 0) return;
        while (true)
        {
            var row = pwupRand.Next(Math.Max(hexMap.MinR, hexMap.MaxR - 6), hexMap.MaxR + 1);
            bool stagger = (row & 1) == 1;
            var col = pwupRand.Next(0, stagger ? 8 : 9);
            var coord = new OffsetCoord(col, row);
            if (hexMap[coord] is BallData ball)
            {
                powerUpStartTimes[coord] = gameTime.TotalGameTime;
                break;
            }
        }
    }

    public event PowerUpObtainedHandler? PowerUpObtained;
    public delegate void PowerUpObtainedHandler();

}
