using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace PuzzleBobble.Scene;

public class CreditsScene : AbstractScene
{
    private Desktop? _desktop;

    public override void Initialize(Game game)
    {
    }

    public override void Deinitialize()
    {
    }

    public override void LoadContent(ContentManager content)
    {
        string[] names = [
            "65050067 Kandanai Chaiyo",
            "65050251 Nutchapol Salawej",
            "65050415 Teeratas Thiangtham"
        ];

        var credits = new VerticalStackPanel
        {
            Widgets = {
                new Label { Text = "Credits", HorizontalAlignment = HorizontalAlignment.Center },
                new Label { Text = String.Join("\n", names) },
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var backBtn = new Button
        {
            Content = new Label { Text = "Back" },
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20),
            Padding = new Thickness(20, 10),
        };
        backBtn.Click += (sender, args) => ChangeScene(Scenes.MENU);

        var panel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = {
                credits,
                backBtn
            }
        };
        _desktop = new Desktop
        {
            Root = panel
        };
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _desktop?.Render();
    }
}
