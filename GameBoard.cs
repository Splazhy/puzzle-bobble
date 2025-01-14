using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class GameBoard : GameObject
{
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

    private Texture2D[] ballTypes = null;
    // balltype is class with name, sprite texture, color, etc.
    // gameboard reference ball type using int for index into this array

    private int[,] board;
    private bool reduceWidthByHalfBall;

    private Hex debug_gridpos;
    private Vector2? debug_mousepos;
    public GameBoard(Game game) : base("gameboard")
    {
        board = new int[,] {
            {2,2,2,2,3,3,4,4},
            {2,2,2,2,3,3,4,4},
            {3,3,3,3,3,3,4,4},
            {4,4,4,4,3,3,4,4},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
            {0,1,0,0,0,0,0,0},
        };
        reduceWidthByHalfBall = true;

        Position = new Vector2((float)(HEX_WIDTH * -4), -300);
    }

    public override void LoadContent(ContentManager content)
    {
        ballTypes = [
            content.Load<Texture2D>("Graphics/Ball/red"),
            content.Load<Texture2D>("Graphics/Ball/green"),
            content.Load<Texture2D>("Graphics/Ball/blue"),
            content.Load<Texture2D>("Graphics/Ball/brown"),
        ];
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                if (reduceWidthByHalfBall && (y % 2) == 1 && x == board.GetLength(1) - 1) continue;

                int ball = board[y, x];
                if (ball == 0) continue;


                // TODO: Rewrite this in a way that it can be drawn at different scales and positions
                int hx = x - (y / 2);
                Vector2 p = hexLayout.HexToDrawLocation(new Hex(hx, y)).Downcast();
                spriteBatch.Draw(ballTypes[ball - 1], new Rectangle((int)(p.X + ScreenPosition.X), (int)(p.Y + ScreenPosition.Y), BALL_SIZE, BALL_SIZE), Color.White);
            }
        }


        if (debug_mousepos.HasValue)
        {
            Vector2 p = hexLayout.HexToDrawLocation(debug_gridpos).Downcast();
            spriteBatch.Draw(ballTypes[3], new Rectangle((int)(p.X + ScreenPosition.X), (int)(p.Y + ScreenPosition.Y), BALL_SIZE, BALL_SIZE), Color.White);
        }
    }


    public Hex ComputeClosestHex(Vector2 pos)
    {
        return hexLayout.PixelToHex(pos - Position).Round();
    }

    public bool IsBallAt(Hex hex)
    {
        OffsetCoord offset = hex.ToOffsetCoord();
        if (offset.row < 0 || board.GetLength(0) <= offset.row ||
            offset.col < 0 || board.GetLength(1) <= offset.col) return false;
        return board[offset.row, offset.col] != 0;
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
        OffsetCoord offset = hex.ToOffsetCoord();
        if (offset.row < 0 || board.GetLength(0) <= offset.row ||
            offset.col < 0 || board.GetLength(1) <= offset.col) return;
        board[offset.row, offset.col] = ball;
    }

    public void ExplodeBalls(Hex hex)
    {
        // TODO: this
    }

    public override void Update(GameTime gameTime)
    {

        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X - (int)VirtualOrigin.X;
        int mouseY = mouseState.Y - (int)VirtualOrigin.Y;

        debug_mousepos = new Vector2(mouseX, mouseY);
        debug_gridpos = ComputeClosestHex(debug_mousepos.Value);


        base.Update(gameTime);
    }

}