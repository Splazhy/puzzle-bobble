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

    public static readonly float DEFAULT_SPEED = 20.0f;
    public static readonly float LERP_AMOUNT = 5.0f;
    public static readonly float EXPLODE_PUSHBACK_BONUS = -50.0f;

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


    private AnimatedTexturePlayer? shineAnimPlayer = null;
    private SoundEffect? settleSfx;
    private HexMap<BallData> hexMap = [];

    private BallData.Assets? _ballAssets;

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;

    public GameBoard(Game game) : base("gameboard")
    {
        Position = new Vector2(0, -300);

        Velocity.Y = DEFAULT_SPEED;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _ballAssets = new BallData.Assets(content);

        var level = Level.Load("3-4-connectHaft");
        for (int i = 0; i < 1; i++)
        {
            level.StackDown(Level.Load("3-4-connectHaft"));
        }
        for (int i = 0; i < 10; i++)
        {
            level.StackUp(Level.Load("3-4-connectHaft"));
        }
        hexMap = level.ToHexRectMap();

        foreach (var item in hexMap)
        {
            Hex hex = item.Key;
            BallData ball = item.Value;
            ball.LoadAnimation(_ballAssets);
        }

        Position = new Vector2(0, (float)GetPreferredPos());

        // IsInfinite = true;

        var animation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_shine"),
            9, 1, 0.01f, false
        );
        shineAnimPlayer = new AnimatedTexturePlayer(animation);

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
            ball.Draw(spriteBatch, gameTime, _ballAssets, ScreenPosition + p);
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
        for (int i = 0; i < 6; i++)
        {
            Hex neighbor = hex.Neighbor(i);
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

    // why keyvaluepair instead of tuple: https://stackoverflow.com/a/40826656/3623350
    // although haven't benchmarked it yet
    public List<KeyValuePair<Vector2, BallData>> ExplodeBalls(Hex sourceHex)
    {
        if (!IsValidHex(sourceHex)) return [];
        BallData? mapBall = hexMap[sourceHex];
        if (mapBall is null) return [];
        BallData specifiedBall = mapBall.Value;

        Queue<Hex> pending = [];
        pending.Enqueue(sourceHex);
        HashSet<Hex> connected = [];

        while (pending.Count > 0)
        {
            Hex current = pending.Dequeue();
            connected.Add(current);

            for (int i = 0; i < 6; i++)
            {
                Hex neighbor = current.Neighbor(i);
                if (IsValidHex(neighbor) && hexMap[neighbor] == specifiedBall && !connected.Contains(neighbor))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }

        if (connected.Count < 3)
        {
            return [];
        }

        List<KeyValuePair<Vector2, BallData>> explodingBalls = [];
        foreach (Hex hex in connected)
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

            foreach (var dir in Hex.directions)
            {
                Hex neighbor = current + dir;
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
        return 50 - GetBottomEdgePos();
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
                if (400 < SelfToParentRelPos(ball.Position).Y)
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

        // reduce the collision circle to be more forgiving to players
        Circle collisionCircle = new(ballPosition, BallData.BALL_SIZE / 2 * 0.8f);

        foreach (var dir in Hex.directions)
        {
            Hex neighborHex = ballClosestHex + dir;
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
        stats.Add(hexMap.GetValues().GetEnumerator());
        stats.Add(children.Concat(pendingChildren).OfType<Ball>().Where(ball =>
            ball.GetState() == Ball.State.Moving
        ).Select(ball => ball.Data).GetEnumerator());
        return stats;
    }

}
