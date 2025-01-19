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
    private Game1 _game;
    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private Random _rand;
    private const float FALLING_SPREAD = 50;

    public event BallsExplodedHandler BallsExploded;
    public delegate void BallsExplodedHandler(List<Ball> explodingBalls);

    public event FloatingBallsFellHandler FloatingBallsFell;
    public delegate void FloatingBallsFellHandler(List<Ball> floatingBalls);

    // a packed grid of balls becomes a hexagon grid
    // https://www.redblobgames.com/grids/hexagons/
    public static readonly int BALL_SIZE = 48;
    public static readonly int HEX_INRADIUS = BALL_SIZE / 2;
    public static readonly double HEX_WIDTH = HEX_INRADIUS * 2;

    public static readonly double HEX_SIZE = HEX_WIDTH / Math.Sqrt(3);
    public static readonly double HEX_HEIGHT = HEX_SIZE * 2;

    private HexLayout hexLayout = new HexLayout(
        HexOrientation.POINTY,
        new Vector2Double(HEX_SIZE, HEX_SIZE),
        new Vector2Double(HEX_INRADIUS, HEX_SIZE) // HEX_WIDTH / 2 and HEX_HEIGHT / 2
    );

    private Texture2D ballSpriteSheet = null;
    private AnimatedTextureInstancer shineAnimation = null;

    private List<Hex> _pendingBallRemoval = [];

    private HexRectMap<int> hexMap;
    private bool reduceWidthByHalfBall;

    private Hex debug_gridpos;
    private Vector2? debug_mousepos;

    private List<Hex> debug_hexes = new List<Hex>();
    private List<Vector2> debug_points = new List<Vector2>();
    public GameBoard(Game game) : base("gameboard")
    {
        _game = (Game1)game;
        _rand = new Random();

        reduceWidthByHalfBall = false;

        Position = new Vector2((float)(HEX_WIDTH * -4), -300);
    }

    public override void LoadContent(ContentManager content)
    {
        var level = Level<int>.Load("test");
        hexMap = new HexRectMap<int>(level);

        ballSpriteSheet = content.Load<Texture2D>("Graphics/balls");

        var animation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_shine"),
            9, 1, 0.01f, false
        );
        shineAnimation = new AnimatedTextureInstancer(animation);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var item in hexMap)
        {
            Hex hex = item.Key;
            int ball = item.Value;
            if (ball == 0) continue;

            Vector2 p = hexLayout.HexToDrawLocation(hex).Downcast();
            spriteBatch.Draw(
                ballSpriteSheet,
                new Rectangle((int)(p.X + ScreenPosition.X), (int)(p.Y + ScreenPosition.Y), BALL_SIZE, BALL_SIZE),
                new Rectangle((ball - 1) * 16, 0, 16, 16),
                Color.White
            );
        }

        shineAnimation.Draw(spriteBatch, gameTime);

        if (debug_mousepos.HasValue)
        {
            spriteBatch.DrawString(
                _game.Font,
                $"{debug_gridpos.q}, {debug_gridpos.r}",
                Mouse.GetState().Position.ToVector2(),
                Color.White
            );
            // Vector2 p = hexLayout.HexToDrawLocation(debug_gridpos).Downcast();
            // spriteBatch.Draw(
            //     ballSpriteSheet,
            //     new Rectangle((int)(p.X + ScreenPosition.X), (int)(p.Y + ScreenPosition.Y), BALL_SIZE, BALL_SIZE),
            //     new Rectangle(0, 0, 16, 16),
            //     Color.White
            // );
        }

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
        return hexMap[hex] != 0;
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

    public void SetBallAt(Hex hex, int ball)
    {
        if (!IsValidHex(hex)) return;
        hexMap[hex] = ball;
    }

    public void ExplodeBalls(Hex sourceHex)
    {
        if (!IsValidHex(sourceHex)) return;
        int specifiedBall = hexMap[sourceHex];
        if (specifiedBall == 0) return;

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
            shineAnimation.PlayAt(shinePosition, 0, Vector2.Zero, 3, Color.White);
            return;
        }

        List<Ball> explodingBalls = [];
        foreach (Hex hex in connected)
        {
            explodingBalls.Add(new Ball((Ball.Color)hexMap[hex] - 1, Ball.State.Exploding)
            {
                Position = ConvertHexToCenter(hex),
                Scale = new Vector2(3, 3),
            });
        }

        // We don't want to remove the balls immediately
        // because the ball will disappear for one frame,
        // causing visual hiccup.
        _pendingBallRemoval.AddRange(connected);

        BallsExploded?.Invoke(explodingBalls);
    }

    public void RemoveFloatingBalls()
    {
        HashSet<Hex> floating = [];
        floating.UnionWith(hexMap.GetKeys().Where(kv => hexMap[kv] != 0 && !_pendingBallRemoval.Contains(kv)));

        Queue<Hex> bfsQueue = new Queue<Hex>();
        // Balls from the top row can't be floating
        foreach (var item in hexMap.Where(kv => kv.Key.r == 0))
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

        if (floating.Count == 0) return;

        List<Ball> fallingBalls = [];
        foreach (Hex hex in floating)
        {
            fallingBalls.Add(new Ball((Ball.Color)hexMap[hex] - 1, Ball.State.Falling)
            {
                Position = ConvertHexToCenter(hex),
                Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD, 0),
                Scale = new Vector2(3, 3),
            });
        }

        // We don't want to remove the balls immediately
        // because the ball will disappear for one frame,
        // causing visual hiccup.
        _pendingBallRemoval.AddRange(floating);

        FloatingBallsFell?.Invoke(fallingBalls);
    }

    public override void Update(GameTime gameTime)
    {
        shineAnimation.Update(gameTime);

        foreach (Hex hex in _pendingBallRemoval)
        {
            hexMap[hex] = 0;
        }
        _pendingBallRemoval.Clear();

        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X - (int)VirtualOrigin.X;
        int mouseY = mouseState.Y - (int)VirtualOrigin.Y;

        debug_mousepos = new Vector2(mouseX, mouseY);
        debug_gridpos = ComputeClosestHex(debug_mousepos.Value);


        base.Update(gameTime);
    }

}
