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
    public static readonly int BALL_SIZE = 48;
    public static readonly int HEX_INRADIUS = BALL_SIZE / 2;
    public static readonly int HEX_WIDTH = HEX_INRADIUS * 2;

    public static readonly double HEX_SIZE = HEX_WIDTH / Math.Sqrt(3);
    public static readonly double HEX_HEIGHT = HEX_SIZE * 2;


    private int _topRow;
    public int TopRow
    {
        get
        {
            _topRow = Math.Min(_topRow, hexMap.MinR);
            return _topRow;
        }
    }

    private readonly HexLayout hexLayout = new(
        HexOrientation.POINTY,
        new Vector2Double(HEX_SIZE, HEX_SIZE),
        new Vector2Double(HEX_INRADIUS * -7, HEX_SIZE) // -(HEX_WIDTH / 2 + HEX_WIDTH * 3) and HEX_HEIGHT / 2
    );

    public static readonly int BOARD_WIDTH_PX = HEX_WIDTH * 8;
    public static readonly int BOARD_HALF_WIDTH_PX = HEX_WIDTH * 4;

    private Texture2D? ballSpriteSheet = null;

    private Texture2D? background = null;
    private Texture2D? leftBorder = null;
    private Texture2D? rightBorder = null;

    private AnimatedTexturePlayer? shineAnimPlayer = null;
    private SoundEffect? settleSfx;
    private HexMap<BallData> hexMap = [];

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;

    public GameBoard(Game game) : base("gameboard")
    {
        Position = new Vector2(0, -300);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        ballSpriteSheet = BallData.LoadBallSpritesheet(content);
        background = content.Load<Texture2D>("Graphics/board_bg");
        leftBorder = content.Load<Texture2D>("Graphics/border_left");
        rightBorder = content.Load<Texture2D>("Graphics/border_right");

        var level = Level.Load("test");
        hexMap = level.ToHexRectMap();

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
        Debug.Assert(ballSpriteSheet is not null, "Ball spritesheet is not loaded.");
        Debug.Assert(background is not null, "Background is not loaded.");
        Debug.Assert(leftBorder is not null, "Left border is not loaded.");
        Debug.Assert(rightBorder is not null, "Right border is not loaded.");

        var pX = ParentTranslate.X;
        spriteBatch.Draw(background, new Vector2(pX - BOARD_HALF_WIDTH_PX, 0), null, Color.White, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
        spriteBatch.Draw(leftBorder, new Vector2(pX - BOARD_HALF_WIDTH_PX - leftBorder.Width * 3, 0), null, Color.White, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
        spriteBatch.Draw(rightBorder, new Vector2(pX + BOARD_HALF_WIDTH_PX, 0), null, Color.White, 0, Vector2.Zero, 3, SpriteEffects.None, 0);

        foreach (var item in hexMap)
        {
            Hex hex = item.Key;
            BallData ball = item.Value;

            Vector2 p = hexLayout.HexToCenterPixel(hex).Downcast();
            ball.Draw(spriteBatch, ballSpriteSheet, ScreenPosition + p);
        }

        DrawChildren(spriteBatch, gameTime);
    }

    public Hex ComputeClosestHex(Vector2 pos)
    {
        return hexLayout.PixelToHex(pos).Round();
    }

    public Vector2 ConvertHexToCenter(Hex hex)
    {
        return hexLayout.HexToCenterPixel(hex).Downcast();
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
                explodingBalls.Add(new KeyValuePair<Vector2, BallData>(ConvertHexToCenter(hex), ball));
                hexMap[hex] = null;
            }
        }

        return explodingBalls;
    }

    public List<KeyValuePair<Vector2, BallData>> RemoveFloatingBalls()
    {
        HashSet<Hex> floating = [];
        floating.UnionWith(hexMap.GetKeys());

        // No balls on the board
        if (floating.Count == 0) return [];

        Queue<Hex> bfsQueue = new Queue<Hex>();
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
                fallingBalls.Add(new KeyValuePair<Vector2, BallData>(ConvertHexToCenter(hex), ball));
                hexMap[hex] = null;
            }
        }

        return fallingBalls;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        UpdatePosition(gameTime);

        UpdateChildren(gameTime);

        var allBalls = children.OfType<Ball>();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var ball in allBalls)
        {
            var circle = ball.Circle;

            float right = BOARD_HALF_WIDTH_PX - circle.radius;
            if (0 < ball.Velocity.X && right < ball.Position.X)
            {
                ball.BounceOverX(right);
            }
            float left = -BOARD_HALF_WIDTH_PX + circle.radius;
            if (ball.Velocity.X < 0 && ball.Position.X < left)
            {
                ball.BounceOverX(left);
            }
            if (ball.GetState() != Ball.State.Moving) continue;

            // FIXME: when ball goes too fast, it could overwrite another ball
            // balls have already applied velocity into their position
            Hex ballClosestHex = ComputeClosestHex(ball.Position);

            foreach (var dir in Hex.directions)
            {
                Hex neighborHex = ballClosestHex + dir;
                if (!IsBallAt(neighborHex) && TopRow <= neighborHex.R) continue;

                Vector2 neighborCenterPos = ConvertHexToCenter(neighborHex);
                Circle neighborCircle = new(neighborCenterPos, GameBoard.HEX_INRADIUS);
                bool colliding = circle.Intersects(neighborCircle) > 0;
                if (!colliding) continue;

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
                    Debug.Assert(shineAnimPlayer is not null && settleSfx is not null);
                    // We want to play the shine animation when a ball is settled
                    // and that ball doesn't cause any explosion.
                    //
                    // We currently only call this method on settled moving ball,
                    // so we can assume that calling play shine animation here will
                    // yield the expected outcome.
                    var shinePosition = hexLayout.HexToCenterPixel(ballClosestHex).Downcast();
                    var shineObj = shineAnimPlayer.PlayAt(shinePosition, new Vector2(16 * 3, 16 * 3), Color.White, 0, new Vector2(8, 8));
                    pendingChildren.Add(shineObj);
                    settleSfx.Play();
                }

                ball.Destroy();
                break;
            }
        }
        ;

        UpdatePendingAndDestroyedChildren();
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
