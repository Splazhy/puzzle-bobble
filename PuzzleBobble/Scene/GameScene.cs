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
    private SpriteFont? _font;
    private Slingshot? _slingshot;
    private GameBoard? _gameBoard;
    private Guideline? _guideline;
    private DeathLine? _deathline;

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;


    private GameState _state = GameState.Playing;
    private bool _keyPDown = false;

    private bool _boardChanged = false;

    public GameScene() : base("scene_game")
    {
    }

    public override void Initialize(Game game)
    {
        _slingshot = new(game);
        _gameBoard = new GameBoard(game);
        _deathline = new(GameBoard.DEATH_Y_POS);
        BoardBackground boardBackground = new(_gameBoard);

        _guideline = new Guideline(
            _gameBoard,
            _slingshot
        );

        _slingshot.BallFired += ball =>
        {
            // semi-hacky solution!
            _guideline.Recalculate();
            ball.EstimatedCollisionPosition = _guideline.LastCollidePosition - _gameBoard.Position;
            _gameBoard.AddChildDeferred(ball);
        };
        _gameBoard.BoardChanged += () =>
        {
            _boardChanged = true;
        };
        children = [
            boardBackground,
            _deathline,
            _gameBoard,
            _guideline,
            _slingshot,
        ];
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _font = content.Load<SpriteFont>("Fonts/Arial24");

        if (_gameBoard is null) return;
    }

    private void Pause()
    {
        Debug.Assert(_state == GameState.Playing);
        _state = GameState.Paused;
        foreach (var child in children)
        {
            child.IsActive = false;
        }
    }

    private void Unpause()
    {
        Debug.Assert(_state == GameState.Paused);
        _state = GameState.Playing;
        foreach (var child in children)
        {
            child.IsActive = true;
        }
    }

    private void Fail(GameTime gameTime)
    {
        Debug.Assert(_state == GameState.Playing);
        Debug.Assert(_gameBoard is not null && _slingshot is not null);
        _state = GameState.Fail;
        _gameBoard.Fail(gameTime);
        _slingshot.Fail();
    }

    private void Success()
    {
        Debug.Assert(_state == GameState.Playing);
        _state = GameState.Success;
        foreach (var child in children)
        {
            child.IsActive = false;
        }
    }

    private void Success()
    {
        Debug.Assert(_state == State.Playing);
        _state = State.Success;
        foreach (var child in children)
        {
            child.IsActive = false;
        }
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        if (Keyboard.GetState().IsKeyDown(Keys.P))
        {
            if (!_keyPDown)
            {
                if (_state == GameState.Paused)
                {
                    Unpause();
                }
                else if (_state == GameState.Playing)
                {
                    Pause();
                }
                _keyPDown = true;
            }
        }
        else
        {
            _keyPDown = false;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(new MenuScene());
        }

        UpdateChildren(gameTime);
        Debug.Assert(_gameBoard != null && _slingshot != null && _deathline != null, "GameBoard, Slingshot, or DeathLine is not loaded");
        if (_state == GameState.Playing)
        {
            if (_gameBoard.GetMapBallCount() == 0)
            {
                Success();
            }
            else
            {
                if (_slingshot.CheckNextData || _boardChanged)
                {
                    var bs = _gameBoard.GetBallStats();
                    if (_slingshot.NextData is not BallData data || !bs.Check(data))
                    {
                        var nextBall = bs.GetNextBall(_rand);
                        _slingshot.SetNextData(nextBall);
                    }
                    _boardChanged = false;
                }

                if (_gameBoard.GetDistanceFromDeath() <= 0)
                {
                    Fail(gameTime);
                }
                else if (_gameBoard.GetDistanceFromDeath() < GameBoard.HEX_VERTICAL_SPACING * 3.5)
                {
                    _deathline.Show(gameTime);
                }
                else if (GameBoard.HEX_VERTICAL_SPACING * 5 < _gameBoard.GetDistanceFromDeath())
                {
                    _deathline.Hide(gameTime);
                }
            }
        }

        UpdatePendingAndDestroyedChildren();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        DrawChildren(spriteBatch, gameTime);
        spriteBatch.DrawString(_font, "Press q to go back to menu\nPress p to pause", new Vector2(100, 200), Color.White);

        switch (_state)
        {
            case GameState.Paused:
                spriteBatch.DrawString(_font, "Paused", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
            case GameState.Fail:
                spriteBatch.DrawString(_font, "Fail", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
            case GameState.Success:
                spriteBatch.DrawString(_font, "Success", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
        }
    }
}
