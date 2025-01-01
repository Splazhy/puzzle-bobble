using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class GameBoard : GameObject
{
    private Texture2D[] ballTypes = null;
    // balltype is class with name, sprite texture, color, etc.
    // gameboard reference ball type using int for index into this array

    private int[,] board;
    private bool reduceWidthByHalfBall;
    public GameBoard(Game game) : base("gameboard")
    {
        board = new int[,] {
            {1,1,1,1},
            {2,2,2,2},
            {3,3,3,3},
            {4,4,4,4},
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

                int ball = board[y, x];
                if (ball == 0) continue;

                int rowOffset = (y % 2) == 1 ? 32 : 0;

                // TODO: Rewrite this in a way that it can be drawn at different scales and positions
                spriteBatch.Draw(ballTypes[ball - 1], new Rectangle(x * 64 + rowOffset, y * 64, 64, 64), Color.White);
            }
        }
    }

}
