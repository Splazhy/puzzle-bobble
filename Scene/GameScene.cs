using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble.Scene;

public class GameScene : AbstractScene
{
    private List<GameObject> _gameObjects = [];
    private List<GameObject> _pendingGameObjects = [];
    private SpriteFont? _font;
    private ContentManager? _content;

    private GameBoard? _gameBoard;

    public override void Initialize(Game game)
    {
        Slingshot slingshot = new Slingshot(game);
        _gameBoard = new GameBoard(game);
        slingshot.BallFired += ball => _pendingGameObjects.Add(ball);
        _gameObjects = [
            slingshot,
            _gameBoard,
        ];
        _pendingGameObjects = [];
    }

    public override void Deinitialize()
    {
        _gameObjects.Clear();
        _pendingGameObjects.Clear();
    }

    public override void LoadContent(ContentManager content)
    {
        _content = content;
        _gameObjects.ForEach(gameObject => gameObject.LoadContent(content));
        _font = content.Load<SpriteFont>("Fonts/Arial24");

        if (_gameBoard is null) return;
        _gameObjects.AddRange(_gameBoard.GetBalls());
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }

        if (_gameBoard is null || _content is null) return;

        _gameObjects.ForEach(gameObject => gameObject.Update(gameTime));

        var movingBalls = _gameObjects.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.GetState() == Ball.State.Moving
        ).Cast<Ball>().ToList();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        movingBalls.ForEach(movingBall =>
        {
            // FIXME: when ball goes too fast, it could overwrite another ball

            // use ahead position to check for collision so player won't see the ball
            // overlapping with balls on the grid as much (visual polish).
            var aheadPosition = movingBall.Position + movingBall.Velocity * deltaTime;
            var aheadCircle = new Circle(aheadPosition, movingBall.Circle.radius);
            Hex ballClosestHex = _gameBoard.ComputeClosestHex(aheadPosition);

            foreach (var dir in Hex.directions)
            {
                Hex neighborHex = ballClosestHex + dir;
                if (_gameBoard.GetBallAt(neighborHex) is Ball neighborBall)
                {
                    if (!neighborBall.IsCollideWith(aheadCircle)) continue;

                    _gameBoard.SetBallAt(ballClosestHex, movingBall);
                    _ = _gameBoard.ExplodeBalls(ballClosestHex);
                    _ = _gameBoard.RemoveFloatingBalls();

                    break;
                }
            }
        });

        _gameObjects.RemoveAll(gameObject => gameObject.Destroyed);
        // NOTE: we need to load content for every new game objects,
        // not sure if this is a design flaw or not.
        _pendingGameObjects.ForEach(gameObject => gameObject.LoadContent(_content));
        _gameObjects.AddRange(_pendingGameObjects);
        _pendingGameObjects.Clear();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _gameObjects.ForEach(gameObject => gameObject.Draw(spriteBatch, gameTime));
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
