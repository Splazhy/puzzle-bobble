using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble.Scene;

public class GameScene : AbstractScene
{
    private List<GameObject> _gameObjects;
    private List<GameObject> _pendingGameObjects;
    private SpriteFont _font;
    private ContentManager _content;

    private GameBoard _gameBoard;

    public override void Initialize(Game game)
    {
        Slingshot slingshot = new Slingshot(game);
        _gameBoard = new GameBoard(game);
        slingshot.BallFired += SpawnBall;
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
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }
        _gameObjects.RemoveAll(gameObject => gameObject.Destroyed);
        // FIXME: urgh, we're loading content for every new ball
        _pendingGameObjects.ForEach(gameObject => gameObject.LoadContent(_content));
        _gameObjects.AddRange(_pendingGameObjects);
        _pendingGameObjects.Clear();

        var movingBalls = _gameObjects.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.state == Ball.State.Moving
        ).Cast<Ball>().ToList();

        var idleBalls = _gameObjects.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.state == Ball.State.Idle
        ).Cast<Ball>().ToList();

        movingBalls.ForEach(movingBall =>
        {
            Hex ballClosestHex = _gameBoard.ComputeClosestHex(movingBall.Position);
            // TODO: better collision detection, possibly by taking ball velocity direction into account
            bool colliding = _gameBoard.IsBallSurronding(ballClosestHex);

            if (colliding)
            {
                // TODO: correctly set ball color
                _gameBoard.SetBallAt(ballClosestHex, 4);
                _gameBoard.ExplodeBalls(ballClosestHex);
                movingBall.Destroy();
            }
        });

        _gameObjects.ForEach(gameObject => gameObject.Update(gameTime));
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _gameObjects.ForEach(gameObject => gameObject.Draw(spriteBatch, gameTime));
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }

    private void SpawnBall(Ball ball)
    {
        _pendingGameObjects.Add(ball);
    }
}
