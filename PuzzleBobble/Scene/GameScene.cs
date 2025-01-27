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
    private static readonly int DEATH_Y_POS = 260;
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

    private enum State
    {
        Playing,
        Fail,
        Success,
        Paused
    }
    private State _state = State.Playing;
    private bool _keyPDown = false;

    public GameScene() : base("scene_game")
    {
    }

    public override void Initialize(Game game)
    {
        _slingshot = new(game);
        _deathline = new(DEATH_Y_POS);
        _gameBoard = new GameBoard(game);
        BoardBackground boardBackground = new(_gameBoard);

        _guideline = new Guideline(
            _gameBoard,
            _slingshot,
            96, 45.0f
        );

        _slingshot.BallFired += ball =>
        {
            // hacky solution!
            ball.EstimatedCollisionPosition = _guideline.LastCollidePosition - _gameBoard.Position;
            _gameBoard.AddChildDeferred(ball);
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
        Debug.Assert(_state == State.Playing);
        _state = State.Paused;
        foreach (var child in children)
        {
            child.IsActive = false;
        }
    }

    private void Unpause()
    {
        Debug.Assert(_state == State.Paused);
        _state = State.Playing;
        foreach (var child in children)
        {
            child.IsActive = true;
        }
    }

    private void Fail()
    {
        Debug.Assert(_state == State.Playing);
        _state = State.Fail;
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
                if (_state == State.Paused)
                {
                    Unpause();
                }
                else if (_state == State.Playing)
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
        if (_state == State.Playing)
        {
            if (_gameBoard.GetMapBallCount() == 0)
            {
                Success();
            }
            else
            {
                if (_slingshot.Data == null)
                {
                    // TODO: move this into BallData or smth
                    var bs = _gameBoard.GetBallStats();
                    var colors = bs.ColorCounts.Keys.ToList();
                    if (0 < colors.Count)
                    {
                        var color = colors[_rand.Next(colors.Count)];
                        _slingshot.SetData(new BallData(color));
                    }
                }

                if (DEATH_Y_POS < _gameBoard.GetMapBottomEdge())
                {
                    Fail();
                }
                else if (DEATH_Y_POS - GameBoard.HEX_VERTICAL_SPACING * 4 < _gameBoard.GetMapBottomEdge())
                {
                    _deathline.Show(gameTime);
                }
                else if (_gameBoard.GetMapBottomEdge() < DEATH_Y_POS - GameBoard.HEX_VERTICAL_SPACING * 5.5)
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
            case State.Paused:
                spriteBatch.DrawString(_font, "Paused", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
            case State.Fail:
                spriteBatch.DrawString(_font, "Fail", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
            case State.Success:
                spriteBatch.DrawString(_font, "Success", new Vector2(100, ScreenPosition.Y), Color.White);
                break;
        }
    }
}
