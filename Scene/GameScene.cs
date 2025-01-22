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
        Slingshot slingshot = new(game);
        _gameBoard = new GameBoard(game);
        slingshot.BallFired += ball => _gameBoard.AddBallFromSlingshot(ball);
        children = [
            slingshot,
            _gameBoard,
        ];
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _font = content.Load<SpriteFont>("Fonts/Arial24");
    }

    public override List<GameObject> Update(GameTime gameTime, Vector2 parentTranslate)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }

        UpdateChildren(gameTime, parentTranslate);
        UpdatePendingAndDestroyedChildren();

        return [];
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        DrawChildren(spriteBatch, gameTime, parentTranslate);
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
