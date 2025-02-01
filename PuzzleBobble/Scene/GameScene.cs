using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PuzzleBobble.Scene;

public class GameScene : AbstractScene
{
    private SpriteFont? _font;
    private Slingshot? _slingshot;
    private GameBoard? _gameBoard;
    private Guideline? _guideline;
    private DeathLine? _deathline;
    private Desktop? _desktop;

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;
    private bool _firstFrame = true;


    private GameState _state = GameState.Playing;
    private TimeSpan? _finishTime = null;
    private bool _escKeyDown = false;

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
            if (_guideline.PoweredUp)
            {
                // semi-hacky solution!
                _guideline.Recalculate();
                ball.EstimatedCollisionPosition = _guideline.LastCollidePosition - _gameBoard.Position;
            }
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


        var resumeBtn = new Button
        {
            Content = new Label { Text = "Resume" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        resumeBtn.Click += (sender, args) => Unpause();

        Button menuBtn = new()
        {
            Content = new Label { Text = "Back to Menu" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        menuBtn.Click += (sender, args) => ChangeScene(new MenuScene());

        var pauseMenu = new VerticalStackPanel
        {
            Widgets = {
                new Label { Text = "Paused", HorizontalAlignment = HorizontalAlignment.Center },
                resumeBtn,
                menuBtn
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _desktop = new Desktop
        {
            Root = pauseMenu
        };
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
        Debug.Assert(_gameBoard is not null && _slingshot is not null && _guideline is not null);
        _state = GameState.Fail;
        _finishTime = gameTime.TotalGameTime;
        _gameBoard.Fail(gameTime);
        _slingshot.Fail(gameTime);
        _guideline.TurnOff(gameTime);
    }

    private void Success(GameTime gameTime)
    {
        Debug.Assert(_state == GameState.Playing);
        Debug.Assert(_gameBoard is not null && _slingshot is not null && _guideline is not null);
        _state = GameState.Success;
        _finishTime = gameTime.TotalGameTime;
        _gameBoard.Success();
        _slingshot.Success(gameTime);
        _guideline.TurnOff(gameTime);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        Debug.Assert(_gameBoard != null && _slingshot != null && _deathline != null && _guideline != null, "GameBoard, Slingshot, DeathLine, or Guideline is not loaded");
        if (_firstFrame)
        {
            _firstFrame = false;
            _guideline.TurnOn(gameTime);
        }
        base.Update(gameTime, parentTranslate);
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            if (!_escKeyDown)
            {
                if (_state == GameState.Paused)
                {
                    Unpause();
                }
                else if (_state == GameState.Playing)
                {
                    Pause();
                }
                _escKeyDown = true;
            }
        }
        else
        {
            _escKeyDown = false;
        }

        if (_state == GameState.Fail || _state == GameState.Success)
        {
            Debug.Assert(_finishTime is not null, "Finish time is not set.");
            if (TimeSpan.FromSeconds(3) < gameTime.TotalGameTime - _finishTime)
            {
                ChangeScene(new MenuScene());
            }
        }

        UpdateChildren(gameTime);
        if (_state == GameState.Playing)
        {
            if (_gameBoard.GetMapBallCount() == 0)
            {
                Success(gameTime);
            }
            else
            {
                if (_slingshot.CheckNextData || _boardChanged)
                {
                    var bs = _gameBoard.GetBallStats();
                    if (_slingshot.NextData is not BallData data || !bs.Check(data))
                    {
                        var nextBall = bs.GetNextBall(_rand);
                        _slingshot.SetNextData(gameTime, nextBall);
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

        // TODO: replace this with proper powerup system
        _guideline.SetPowerUp(gameTime, Mouse.GetState().RightButton == ButtonState.Pressed);

        UpdatePendingAndDestroyedChildren();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_desktop is not null, "Desktop is not loaded.");
        DrawChildren(spriteBatch, gameTime);
        spriteBatch.DrawString(_font, "Press esc to pause", new Vector2(100, 200), Color.White);

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

    public override Desktop? DrawMyra()
    {
        if (_state == GameState.Paused)
        {
            return _desktop;
        }
        return null;
    }

}
