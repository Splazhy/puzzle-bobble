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
    public static readonly int BALL_SIZE = 64;
    public static readonly int HEX_INRADIUS = BALL_SIZE / 2;
    public static readonly double HEX_WIDTH = HEX_INRADIUS * 2;

    public static readonly double HEX_SIZE = HEX_WIDTH / Math.Sqrt(3);
    public static readonly double HEX_HEIGHT = HEX_SIZE * 2;

    private Texture2D[] ballTypes = null;
    // balltype is class with name, sprite texture, color, etc.
    // gameboard reference ball type using int for index into this array

    private int[,] board;
    private bool reduceWidthByHalfBall;

    private (int, int)? debug_gridpos;
    private Vector2? debug_mousepos;
    public GameBoard(Game game) : base("gameboard")
    {
        board = new int[,] {
            {2,2,2,2,0,0,0,0},
            {2,2,2,2,0,0,0,0},
            {3,3,3,3,0,0,0,0},
            {4,4,4,4,0,0,0,0},
            {0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0},
        };
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

                double rowOffset = (y % 2) == 1 ? (HEX_WIDTH / 2) : 0;
                if (debug_gridpos.HasValue && debug_gridpos.Value.Item1 == x && debug_gridpos.Value.Item2 == y)
                {
                    spriteBatch.Draw(ballTypes[0], new Rectangle((int)(x * HEX_WIDTH + rowOffset), (int)(y * HEX_HEIGHT * (3 / 4.0)), BALL_SIZE, BALL_SIZE), Color.White);
                    continue;
                }

                int ball = board[y, x];
                if (ball == 0) continue;


                // TODO: Rewrite this in a way that it can be drawn at different scales and positions
                spriteBatch.Draw(ballTypes[ball - 1], new Rectangle((int)(x * HEX_WIDTH + rowOffset), (int)(y * HEX_HEIGHT * (3 / 4.0)), BALL_SIZE, BALL_SIZE), Color.White);

            }
        }


        if (debug_mousepos.HasValue)
        {
            spriteBatch.Draw(ballTypes[3], new Rectangle((int)debug_mousepos.Value.X, (int)debug_mousepos.Value.Y, BALL_SIZE, BALL_SIZE), Color.White);
        }
    }



    /// <param name="pos">ball's top left corner</param>
    // TODO: fix for hexagon grid https://lospec.com/palette-list/resurrect-64
    public (int, int)? ComputeClosestGridPoint(Vector2 pos)
    {
        Vector2 ballCenterPos = pos + new Vector2(BALL_SIZE / 2, BALL_SIZE / 2);
        int gridY = (int)Math.Floor(ballCenterPos.Y / BALL_SIZE);

        int rowOffset = (gridY % 2) == 1 ? (BALL_SIZE / 2) : 0;
        int gridX = (int)Math.Floor((ballCenterPos.X - rowOffset) / BALL_SIZE);

        if (gridX < 0 || board.GetLength(1) <= gridX ||
            gridY < 0 || board.GetLength(0) <= gridY)
        {
            return null;
        }


        return (gridX, gridY);
    }

    public override void Update(GameTime gameTime)
    {

        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X;
        int mouseY = mouseState.Y;

        debug_mousepos = new Vector2(mouseX, mouseY);
        debug_gridpos = ComputeClosestGridPoint(debug_mousepos.Value);


        base.Update(gameTime);
    }

}