using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace puzzle_bobble;

public class GameBoard : DrawableGameComponent
{
    private Texture2D[] ballTypes = null;
    // balltype is class with name, sprite texture, color, etc.
    // gameboard reference ball type using int for index into this array

    private SpriteBatch _spriteBatch;
    private int[,] board;
    private bool reduceWidthByHalfBall;
    public GameBoard(Game game) : base(game)
    {
        board = new int[,] {
            {1,1,1,1},
            {2,2,2,2},
            {3,3,3,3},
            {4,4,4,4},
        };

    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        ballTypes = [
            Game.Content.Load<Texture2D>("redBall"),
            Game.Content.Load<Texture2D>("greenBall"),
            Game.Content.Load<Texture2D>("blueBall"),
            Game.Content.Load<Texture2D>("yellowBall"),
        ];


    }

    public override void Initialize()
    {
        base.Initialize();
        // TODO in capital letters
    }

    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                if (reduceWidthByHalfBall && (y % 2) == 1 && x == board.GetLength(1) - 1) continue;

                int ball = board[y, x];
                if (ball == 0) continue;

                int rowOffset = (y % 2) == 1 ? 32 : 0;
                _spriteBatch.Draw(ballTypes[ball - 1], new Rectangle(x * 64 + rowOffset, y * 64, 64, 64), Color.White);
            }
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }


}