using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using PuzzleBobble.Easer;

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

    enum PowerUp
    {
        Precognition,
        Lucky,
        Special
    }

    private readonly Dictionary<PowerUp, TimeSpan> _powerUpEndTimes = [];
    private int _powerUpsPending;
    private TimeSpan? _timeUntilNextPowerUp = null;
    private readonly Random _powerUpRand = new();
    private int _specialsPending;

    private SaveData? _saveData;
    private long _playHistoryId;
    private TimeSpan? _lastUpdateTime;
    private TimeSpan _startTime;

    public override Color BackgroundColor => new(49, 54, 56);

    public GameScene() : base("scene_game")
    {
    }

    public override void Initialize(Game game, SaveData sd)
    {
        _saveData = sd;
        _playHistoryId = sd.CreateNewPlayHistoryEntry(DateTime.Now);

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
        _gameBoard.PowerUpObtained += () =>
        {
            _powerUpsPending++;
        };
        _gameBoard.BallsObtained += (balls) =>
        {
            Dictionary<string, int> _ballObtainedCounts = [];

            foreach (var ball in balls)
            {
                if (_ballObtainedCounts.ContainsKey(ball.ToSQLValue()))
                {
                    _ballObtainedCounts[ball.ToSQLValue()]++;
                }
                else
                {
                    _ballObtainedCounts[ball.ToSQLValue()] = 1;
                }
            }

            foreach (var (ball, count) in _ballObtainedCounts)
            {
                _saveData.AddToPlayHistoryDetail(_playHistoryId, $"ball-{ball}", count);
            }
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
                    var bs = _gameBoard.GetBallStats(_powerUpEndTimes.ContainsKey(PowerUp.Lucky));
                    if (_slingshot.NextData is not BallData data || !bs.Check(data))
                    {
                        BallData? nextBall;
                        if (0 < _specialsPending)
                        {
                            _specialsPending--;
                            nextBall = new BallData(BallData.RandomUsefulSpecial(_rand));
                        }
                        else
                        {
                            nextBall = bs.GetNextBall(_rand);
                        }
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

        if (_timeUntilNextPowerUp is null)
        {
            _timeUntilNextPowerUp = gameTime.TotalGameTime + TimeSpan.FromSeconds(15 + _powerUpRand.NextSingle() * 15);
        }
        else if (_timeUntilNextPowerUp < gameTime.TotalGameTime)
        {
            _gameBoard.PlacePowerUp(_powerUpRand, gameTime);
            _timeUntilNextPowerUp = null;
        }

        while (0 < _powerUpsPending)
        {
            _powerUpsPending--;
            var powerUps = Enum.GetValues<PowerUp>();
            var chosenPowerUp = powerUps[_powerUpRand.Next(powerUps.Length)];
            switch (chosenPowerUp)
            {
                case PowerUp.Precognition:
                    _powerUpEndTimes[chosenPowerUp] = gameTime.TotalGameTime + TimeSpan.FromSeconds(15);
                    _guideline.SetPowerUp(gameTime, true);
                    break;
                case PowerUp.Lucky:
                    _powerUpEndTimes[chosenPowerUp] = gameTime.TotalGameTime + TimeSpan.FromSeconds(5);
                    break;
                case PowerUp.Special:
                    _specialsPending++;
                    _powerUpEndTimes[chosenPowerUp] = gameTime.TotalGameTime + TimeSpan.FromSeconds(1.5);
                    break;
            }
        }

        foreach (var (powerUp, endTime) in _powerUpEndTimes)
        {
            if (endTime < gameTime.TotalGameTime)
            {
                switch (powerUp)
                {
                    case PowerUp.Precognition:
                        _guideline.SetPowerUp(gameTime, false);
                        break;
                }
                _powerUpEndTimes.Remove(powerUp);
            }
        }

        UpdatePendingAndDestroyedChildren();

        UpdateSaveData(gameTime);
    }

    private void UpdateSaveData(GameTime gameTime)
    {
        if (_lastUpdateTime is not TimeSpan ut)
        {
            _lastUpdateTime = gameTime.TotalGameTime;
            _startTime = gameTime.TotalGameTime;
            return;
        }

        if (gameTime.TotalGameTime < _lastUpdateTime + TimeSpan.FromSeconds(1))
        {
            return;
        }
        _lastUpdateTime = gameTime.TotalGameTime;

        Debug.Assert(_saveData is not null, "SaveData is not loaded.");

        var elapsedTime = ((_finishTime ?? gameTime.TotalGameTime) - _startTime).TotalSeconds;
        _saveData.UpdatePlayHistoryEntry(_playHistoryId, elapsedTime, _state == GameState.Paused ? GameState.Playing : _state);
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

        foreach (var ((pwup, endTime), index) in _powerUpEndTimes.Select((item, index) => (item, index)))
        {
            var powerUpString = pwup.ToString();
            var drawPosX = ScreenPositionO(new Vector2(GameBoard.BOARD_HALF_WIDTH_PX + 20, 0)).X;
            var drawPosY = ScreenPosition.Y - index * 30;
            var secsLeft = (endTime - gameTime.TotalGameTime).TotalSeconds;
            var alpha = EasingFunctions.PowerInOut(2)(Math.Min(1, secsLeft));
            spriteBatch.DrawString(_font, powerUpString, new Vector2(drawPosX, drawPosY), Color.White * (float)alpha);
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
