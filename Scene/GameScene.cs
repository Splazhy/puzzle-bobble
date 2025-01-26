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

    /// <summary>
    /// For random falling velocity of falling balls
    /// </summary>
    private readonly Random _rand = new();
    private const float FALLING_SPREAD = 50;
    private const float EXPLOSION_SPREAD = 50;

    public bool Paused = false;
    private bool _keyPDown = false;

    public GameScene() : base("scene_game")
    {
    }

    public override void Initialize(Game game)
    {
        _slingshot = new(game);
        DeathLine deathline = new(game);
        _gameBoard = new GameBoard(game);
        _slingshot.BallFired += ball => _gameBoard.AddChildDeferred(ball);
        children = [
            _gameBoard,
            deathline,
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
        Paused = true;
        foreach (var child in children)
        {
            child.IsActive = false;
        }
    }

    private void Unpause()
    {
        Paused = false;
        foreach (var child in children)
        {
            child.IsActive = true;
        }
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        if (Keyboard.GetState().IsKeyDown(Keys.P))
        {
            if (!_keyPDown)
            {
                if (Paused)
                {
                    Unpause();
                }
                else
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
        Debug.Assert(_gameBoard != null && _slingshot != null);
        if (_slingshot.Data == null)
        {
            // TODO: move this into BallData or smth
            var bs = _gameBoard.GetBallStats();
            var colors = bs.ColorCounts.Keys.ToList();
            if (0 < colors.Count)
            {
                var color = colors[_rand.Next(colors.Count)];
                _slingshot.Data = new BallData(color);
            }
        }
        UpdatePendingAndDestroyedChildren();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        DrawChildren(spriteBatch, gameTime);
        spriteBatch.DrawString(_font, "Press q to go back to menu\nPress p to pause", new Vector2(100, 200), Color.White);
    }
}
