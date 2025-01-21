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

    private Texture2D? ballSpriteSheet = null;

    // For these textures below to reference from,
    // they must stay in place while the gameboard moves down.
    private Vector2 startPosition;
    private Texture2D? background = null;
    private Texture2D? leftBorder = null;
    private Texture2D? rightBorder = null;

    private HexMap<Ball> hexMap = new HexMap<Ball>();

    private Hex debug_gridpos;
    private Vector2? debug_mousepos;

    private List<Hex> debug_hexes = new List<Hex>();
    private List<Vector2> debug_points = new List<Vector2>();
    public GameBoard(Game game) : base("gameboard")
    {
        _game = (Game1)game;

        Position = new Vector2((float)(HEX_WIDTH * -4), -300);
        startPosition = Position;
    }

    public List<Ball> GetBalls()
    {
        return [.. hexMap.GetValues()];
    }

    public override void LoadContent(ContentManager content)
    {
        ballSpriteSheet = content.Load<Texture2D>("Graphics/balls");
        background = content.Load<Texture2D>("Graphics/board_bg");
        leftBorder = content.Load<Texture2D>("Graphics/border_left");
        rightBorder = content.Load<Texture2D>("Graphics/border_right");

        var level = Level.Load("test");
        hexMap = level.ToHexRectMap();
        foreach (var kv in hexMap)
        {
            Hex hex = kv.Key;
            Ball ball = kv.Value;
            ball.Scale = new Vector2(BALL_SIZE / 16, BALL_SIZE / 16);
            ball.Position = ConvertHexToCenter(hex);
            ball.LoadContent(content);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // i'm starting to hate nullable reference types now >:(
        if (background is null) return;
        if (leftBorder is null) return;
        if (rightBorder is null) return;

        var startScreenPosition = startPosition + VirtualOrigin;
        // `14 * 6`
        // 14 is the height of each row (16 - 2 dithered pixels)
        // 6 is the number of rows
        spriteBatch.Draw(background, startScreenPosition, null, Color.White, 0, new Vector2(0, 14 * 6), 3, SpriteEffects.None, 0);
        spriteBatch.Draw(leftBorder, new Vector2(startScreenPosition.X - leftBorder.Width * 3, startScreenPosition.Y), null, Color.White, 0, new Vector2(0, 14 * 6), 3, SpriteEffects.None, 0);
        spriteBatch.Draw(rightBorder, new Vector2(startScreenPosition.X + background.Width * 3, startScreenPosition.Y), null, Color.White, 0, new Vector2(0, 14 * 6), 3, SpriteEffects.None, 0);

        if (debug_mousepos.HasValue)
        {
            // spriteBatch.DrawString(
            //     _game.Font,
            //     $"{debug_gridpos.q}, {debug_gridpos.r}",
            //     Mouse.GetState().Position.ToVector2(),
            //     Color.White
            // );
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
        return hexMap[hex] is not null;
    }

    public Ball? GetBallAt(Hex hex)
    {
        if (!IsValidHex(hex)) return null;
        return hexMap[hex];
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

    public void SetBallAt(Hex hex, Ball ball)
    {
        if (!IsValidHex(hex)) return;
        ball.Position = ConvertHexToCenter(hex);
        ball.SetState(Ball.State.Idle);
        hexMap[hex] = ball;
    }

    public List<Ball> ExplodeBalls(Hex sourceHex)
    {
        if (!IsValidHex(sourceHex)) return [];
        Ball? mapBall = hexMap[sourceHex];
        if (mapBall is null) return [];
        Ball specifiedBall = mapBall;

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
                if (
                    IsValidHex(neighbor) &&
                    hexMap[neighbor] is Ball neighborBall &&
                    neighborBall.GetColor() == specifiedBall.GetColor() &&
                    !connected.Contains(neighbor))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }

        if (connected.Count < 3)
        {
            // Let this ball shine ( ◡̀_◡́)ᕤ
            specifiedBall.SetState(Ball.State.Settle);
            return [];
        }

        List<Ball> explodingBalls = [];
        foreach (Hex hex in connected)
        {
            if (hexMap[hex] is Ball ball)
            {
                ball.SetState(Ball.State.Exploding);
                explodingBalls.Add(ball);
                hexMap[hex] = null;
            }
        }

        return explodingBalls;
    }

    public List<Ball> RemoveFloatingBalls()
    {
        HashSet<Hex> floating = [];
        floating.UnionWith(hexMap.GetKeys());

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

        if (floating.Count == 0) return [];

        List<Ball> fallingBalls = [];
        foreach (Hex hex in floating)
        {
            if (hexMap[hex] is Ball ball)
            {
                ball.SetState(Ball.State.Falling);
                fallingBalls.Add(ball);
                hexMap[hex] = null;
            }
        }


        return fallingBalls;
    }

    bool spaceWasDown = false;
    public override void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X - (int)VirtualOrigin.X;
        int mouseY = mouseState.Y - (int)VirtualOrigin.Y;

        debug_mousepos = new Vector2(mouseX, mouseY);
        debug_gridpos = ComputeClosestHex(debug_mousepos.Value);

        // move down by 3/4 of the hex height (aka one row)
        if (Keyboard.GetState().IsKeyDown(Keys.Space) && !spaceWasDown)
        {
            Position += new Vector2(0, (float)HEX_HEIGHT * (3.0f / 4.0f));
        }
        spaceWasDown = Keyboard.GetState().IsKeyDown(Keys.Space);

        // someone said that this is expensive
        foreach (var kv in hexMap)
        {
            kv.Value.Position = ConvertHexToCenter(kv.Key);
        }


        base.Update(gameTime);
    }

}
