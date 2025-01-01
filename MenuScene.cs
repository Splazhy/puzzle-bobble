using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class MenuScene : AbstractScene
{
    private SpriteFont _font;

    public override void Initialize(Game game)
    {
    }

    public override void Deinitialize()
    {
    }

    public override void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/Arial24");
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Enter))
        {
            ChangeScene(Scenes.MAIN);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.DrawString(_font, "Menu Scene", new Vector2(100, 100), Color.White);
        spriteBatch.DrawString(_font, "Press Enter to change scene", new Vector2(100, 150), Color.White);
    }
}
