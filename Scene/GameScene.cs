using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble.Scene;

public class GameScene : AbstractScene
{
    private SpriteFont? _font;
    private ContentManager? _content;

    private GameBoard? _gameBoard;

    public override void Initialize(Game game)
    {
        Slingshot slingshot = new Slingshot(game);
        DeathLine deathline = new DeathLine(game);
        _gameBoard = new GameBoard(game);
        slingshot.BallFired += ball => Root.AddChildDeferred(ball);
        Root.AddChildren([
            _gameBoard,
            deathline,
            slingshot,
        ]);
    }

    public override void Deinitialize()
    {
        Root.ClearChildren();
    }

    public override void LoadContent(ContentManager content)
    {
        _content = content;
        _font = content.Load<SpriteFont>("Fonts/Arial24");

        Root.LoadContent(content);
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }

        Debug.Assert(_gameBoard is not null, "_gameBoard is not initialized");
        Debug.Assert(_content is not null, "_content is not initialized");

        Root.Update(gameTime);

        var movingBalls = Root.Children.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.GetState() == Ball.State.Moving
        ).Cast<Ball>().ToList();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        movingBalls.ForEach(movingBall =>
        {
            // FIXME: when ball goes too fast, it could overwrite another ball

            // use ahead position to check for collision so player won't see the ball
            // overlapping with balls on the grid as much (visual polish).
            var targetPosition = movingBall.GlobalPosition + movingBall.Velocity * deltaTime;
            var targetCircle = new Circle(targetPosition, movingBall.Circle.radius);
            Hex ballClosestHex = _gameBoard.ComputeClosestHex(targetPosition);
            if (_gameBoard.IsBallAt(ballClosestHex))
            {
                // fallback to current position if ahead position is invalid
                // this exists to handle the case where the ball bounces too close into a ball.
                targetPosition = movingBall.GlobalPosition;
                targetCircle = movingBall.Circle;
                ballClosestHex = _gameBoard.ComputeClosestHex(targetPosition);
                if (_gameBoard.IsBallAt(ballClosestHex))
                {
                    movingBall.SetState(Ball.State.Exploding);
                    return;
                }
            }

            foreach (var dir in Hex.directions)
            {
                Hex neighborHex = ballClosestHex + dir;
                if (_gameBoard.GetBallAt(neighborHex) is Ball neighborBall)
                {
                    if (!neighborBall.IsCollideWith(targetCircle)) continue;

                    _gameBoard.SetBallAt(ballClosestHex, movingBall);
                    var explodingBalls = _gameBoard.ExplodeBalls(ballClosestHex);
                    var fallingBalls = _gameBoard.RemoveFloatingBalls();

                    // HACK: remove the ball from the list to prevent
                    // double Update() call on the same ball.
                    explodingBalls.Remove(movingBall);

                    Root.AddChildrenDeferred(explodingBalls);
                    Root.AddChildrenDeferred(fallingBalls);

                    break;
                }
            }
        });

    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Root.Draw(spriteBatch, gameTime);
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
