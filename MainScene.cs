using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class MainScene : AbstractScene
{
    private List<GameObject> _gameObjects;
    private SpriteFont _font;

    public override void Initialize(Game game)
    {
        _gameObjects = [
            new Slingshot(game),
            new GameBoard(game)
        ];
    }

    public override void Deinitialize()
    {
        _gameObjects.Clear();
    }

    public override void LoadContent(ContentManager content)
    {
        _gameObjects.ForEach(gameObject => gameObject.LoadContent(content));
        _font = content.Load<SpriteFont>("Fonts/Arial24");
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            ChangeScene(Scenes.MENU);
        }
        _gameObjects.RemoveAll(gameObject => gameObject.Destroyed);
        _gameObjects.ForEach(gameObject => gameObject.Update(gameTime));
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _gameObjects.ForEach(gameObject => gameObject.Draw(spriteBatch, gameTime));
        spriteBatch.DrawString(_font, "Press q to go back to menu", new Vector2(100, 100), Color.White);
    }
}
