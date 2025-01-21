using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PuzzleBobble.Scene;

public class MenuScene : AbstractScene
{
    private Game? _game;
    private Desktop? _desktop;

    public override void Initialize(Game game)
    {
        _game = game;
    }

    public override void Deinitialize()
    {
    }

    public override void LoadContent(ContentManager content)
    {

        Button startBtn = new()

        {
            Content = new Label { Text = "Start" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        startBtn.Click += (sender, args) => ChangeScene(Scenes.GAME);

        var optionsBtn = new Button
        {
            Content = new Label { Text = "Options" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };

        var creditsBtn = new Button
        {
            Content = new Label { Text = "Credits" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        creditsBtn.Click += (sender, args) => ChangeScene(Scenes.CREDITS);

        var quitBtn = new Button
        {
            Content = new Label { Text = "Quit" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        quitBtn.Click += (sender, args) => _game?.Exit();

        var testBtn = new Button
        {
            Content = new Label { Text = "Test" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
        };
        testBtn.Click += (sender, args) => ChangeScene(Scenes.Craft);

        VerticalStackPanel menu = new()

        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                startBtn,
                optionsBtn,
                creditsBtn,
                quitBtn,
                testBtn
            }
        };

        _desktop = new Desktop
        {
            Root = menu
        };
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        _desktop?.Render();
    }
}
