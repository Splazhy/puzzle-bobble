using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PuzzleBobble.Scene;

public class MenuScene : AbstractScene
{
    private Game? _game;
    private Desktop? _desktop;

    private Texture2D? _logoTexture;

    public MenuScene() : base("scene_menu")
    {
    }

    public override void Initialize(Game game, SaveData sd)
    {
        _game = game;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _logoTexture = content.Load<Texture2D>("Graphics/logo");

        Button startBtn = new()

        {
            Content = new Label { Text = "Start" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        startBtn.Click += (sender, args) => ChangeUpperScene(new GameScene());

        var creditsBtn = new Button
        {
            Content = new Label { Text = "Credits" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        creditsBtn.Click += (sender, args) => ChangeScene(new CreditsScene());

        var quitBtn = new Button
        {
            Content = new Label { Text = "Quit" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        quitBtn.Click += (sender, args) => _game?.Exit();

        var homeBtn = new Button
        {
            Content = new Label { Text = "Home" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        homeBtn.Click += (sender, args) => ChangeScene(new CraftScene());

        VerticalStackPanel menu = new()

        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                startBtn,
                homeBtn,
                creditsBtn,
                quitBtn,
            }
        };

        _desktop = new Desktop
        {
            Root = menu
        };
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        Debug.Assert(_game is not null, "Game is not loaded.");
        if (GoBackTriggered)
        {
            _game.Exit();
        }
    }

    public override Desktop? DrawMyra()
    {
        return _desktop;
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_logoTexture is not null, "Logo texture is not loaded.");

        var wave = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * Math.PI * 1.2) * 2);


        spriteBatch.Draw(
            _logoTexture,
            ScreenPositionO(new Vector2(257, -140 + wave)),
            null,
            Color.White,
            0,
            new Vector2(_logoTexture.Width, 0),
            PIXEL_SIZE * (2f / 3f),
            SpriteEffects.None,
            0
        );
    }

}
