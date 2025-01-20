using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;

public class GameBoard : GameObject
{
    private readonly Game1 _game;


    // a packed grid of balls becomes a hexagon grid
    // https://www.redblobgames.com/grids/hexagons/
    public static readonly int BALL_SIZE = 48;
    public static readonly int HEX_INRADIUS = BALL_SIZE / 2;
    public static readonly double HEX_WIDTH = HEX_INRADIUS * 2;

    public static readonly double HEX_SIZE = HEX_WIDTH / Math.Sqrt(3);
    public static readonly double HEX_HEIGHT = HEX_SIZE * 2;

    private readonly HexLayout hexLayout = new(
        HexOrientation.POINTY,
        new Vector2Double(HEX_SIZE, HEX_SIZE),
        new Vector2Double(HEX_INRADIUS, HEX_SIZE) // HEX_WIDTH / 2 and HEX_HEIGHT / 2
    );

    private Texture2D? ballSpriteSheet = null;
    private AnimatedTextureInstancer? shineAnimation = null;

    private HexMap<BallData> hexMap = new();

    private readonly List<Hex> debug_hexes = [];
    private readonly List<Vector2> debug_points = [];
    public GameBoard(Game game) : base("gameboard")
    {
        _game = (Game1)game;

        Position = new Vector2((float)(HEX_WIDTH * -4), -300);
    }

    public override void LoadContent(ContentManager content)
    {
        var level = Level.Load("test");
        hexMap = level.ToHexRectMap();

        ballSpriteSheet = content.Load<Texture2D>("Graphics/balls");

        var animation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_shine"),
            9, 1, 0.01f, false
        );
        shineAnimation = new AnimatedTextureInstancer(animation);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (ballSpriteSheet is null) return;

        foreach (var item in hexMap)
        {
            Hex hex = item.Key;
            BallData? ballMaybe = item.Value;
            if (!ballMaybe.HasValue) continue;
            BallData ball = ballMaybe.Value;

            Vector2 p = hexLayout.HexToDrawLocation(hex).Downcast();
            ball.DrawPosByCorner(spriteBatch, ballSpriteSheet, p + ScreenPosition);
        }

        shineAnimation?.Draw(spriteBatch, gameTime);

        foreach (Hex hex in debug_hexes)
        {
            Vector2 p = hexLayout.HexToDrawLocation(hex).Downcast();
            spriteBatch.Draw(
                ballSpriteSheet,
                new Rectangle((int)(p.X + ScreenPosition.X), (int)(p.Y + ScreenPosition.Y), BALL_SIZE, BALL_SIZE),
                new Rectangle(0, 0, 16, 16),
                Color.White
            );

        }
        debug_hexes.Clear();

        foreach (Vector2 point in debug_points)
        {
            Vector2 p = point;
            spriteBatch.Draw(
                ballSpriteSheet,
                new Rectangle((int)(p.X + ScreenPosition.X - 4), (int)(p.Y + ScreenPosition.Y - 4), 9, 9),
                new Rectangle(16 * 11, 16 * 11, 16, 16),
                Color.White
            );

        }
        debug_points.Clear();
    }

    public void DebugDrawHex(Hex hex)
    {
        debug_hexes.Add(hex);
    }

    public void DebugDrawPoint(Vector2 point)
    {
        debug_points.Add(point - Position);
    }


    public Hex ComputeClosestHex(Vector2 pos)
    {
        return hexLayout.PixelToHex(pos - Position).Round();
    }

    public Vector2 ConvertHexToCenter(Hex hex)
    {
        return hexLayout.HexToPixel(hex).Downcast() + Position;
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
        if (!mapBall.HasValue) return [];
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
            // We want to play the shine animation when a ball is settled
            // and that ball doesn't cause any explosion.
            //
            // We currently only call this method on settled moving ball,
            // so we can assume that calling play shine animation here will
            // yield the expected outcome.
            var shinePosition = hexLayout.HexToDrawLocation(sourceHex).Downcast() + ScreenPosition;
            shineAnimation?.PlayAt(shinePosition, 0, Vector2.Zero, 3, Color.White);
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
        floating.UnionWith(hexMap.GetKeys().Where(kv => hexMap[kv].HasValue));

        Queue<Hex> bfsQueue = new();
        // Balls from the top row can't be floating
        foreach (var item in hexMap.Where(kv => kv.Key.R == 0))
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

    public override void Update(GameTime gameTime)
    {
        shineAnimation?.Update(gameTime);

        base.Update(gameTime);
    }

}
