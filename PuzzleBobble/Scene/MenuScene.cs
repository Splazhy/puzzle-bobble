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
    private bool _escKeyDown = true;

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

        Button startBtn = new()

        {
            Content = new Label { Text = "Start" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        startBtn.Click += (sender, args) => ChangeScene(new GameScene());

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
        Debug.Assert(_game is not null, "Game is not loaded.");
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            if (!_escKeyDown) _game.Exit();
            _escKeyDown = true;
        }
        else
        {
            _escKeyDown = false;
        }
    }

    public override Desktop? DrawMyra()
    {
        return _desktop;
    }

}
