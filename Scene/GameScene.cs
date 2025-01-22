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

    public GameScene() : base("scene_game")
    {
    }

    public override void Initialize(Game game)
    {
        _slingshot = new(game);
        DeathLine deathline = new(game);
        _gameBoard = new GameBoard(game);
        _slingshot.BallFired += ball => _gameBoard.AddBallFromSlingshot(ball);
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

    public override List<GameObject> Update(GameTime gameTime, Vector2 parentTranslate)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }

        UpdateChildren(gameTime, parentTranslate);
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

        return [];
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        DrawChildren(spriteBatch, gameTime, parentTranslate);
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
