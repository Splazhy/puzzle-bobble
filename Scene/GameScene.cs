using System;
using System.Collections.Generic;
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
    private List<GameObject> _gameObjects = [];
    private List<GameObject> _pendingGameObjects = [];
    private SpriteFont? _font;
    private ContentManager? _content;

    private GameBoard? _gameBoard;

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;

    public override void Initialize(Game game)
    {
        Slingshot slingshot = new(game);
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
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }

        Debug.Assert(_gameBoard is not null && _content is not null);

        var movingBalls = _gameObjects.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.GetState() == Ball.State.Moving
        ).Cast<Ball>().ToList();

        var idleBalls = _gameObjects.FindAll(gameObject =>
            gameObject is Ball ball &&
            ball.GetState() == Ball.State.Idle
        ).Cast<Ball>().ToList();

        _gameObjects.ForEach(gameObject => gameObject.Update(gameTime, parentTranslate));

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
                if (!_gameBoard.IsBallAt(neighborHex)) continue;

                Vector2 neighborCenterPos = _gameBoard.ConvertHexToCenter(neighborHex);
                Circle neighborCircle = new(neighborCenterPos, GameBoard.HEX_INRADIUS);
                bool colliding = aheadCircle.Intersects(neighborCircle) > 0;
                if (!colliding) continue;

                _gameBoard.SetBallAt(ballClosestHex, movingBall.Data);
                var explodingBalls = _gameBoard.ExplodeBalls(ballClosestHex);
                var fallBalls = _gameBoard.RemoveFloatingBalls();

                _pendingGameObjects.AddRange(explodingBalls.ConvertAll(explodingBall =>
                {
                    var b = new Ball(explodingBall.Value, Ball.State.Exploding)
                    {
                        Position = explodingBall.Key,
                        // Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * EXPLOSION_SPREAD, (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * EXPLOSION_SPREAD)
                    };
                    return b;
                }));
                _pendingGameObjects.AddRange(fallBalls.ConvertAll(fallingBall =>
                {
                    var b = new Ball(fallingBall.Value, Ball.State.Falling)
                    {
                        Position = fallingBall.Key,
                        Velocity = new Vector2((_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD, (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD)
                    };
                    return b;
                }));

                movingBall.Destroy();
                break;
            }
        });

        _gameObjects.RemoveAll(gameObject => gameObject.Destroyed);
        // NOTE: we need to load content for every new game objects,
        // not sure if this is a design flaw or not.
        _pendingGameObjects.ForEach(gameObject => gameObject.LoadContent(_content));
        _gameObjects.AddRange(_pendingGameObjects);
        _pendingGameObjects.Clear();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        _gameObjects.ForEach(gameObject => gameObject.Draw(spriteBatch, gameTime, parentTranslate));
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
