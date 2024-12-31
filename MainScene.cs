using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace puzzle_bobble;

public class MainScene : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch SpriteBatch;
    private List<GameObject> _gameObjects;

    public MainScene()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _gameObjects = new List<GameObject>();

        // NOTE: Add game objects to the scene here
        _gameObjects.Add(new Slingshot(this));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        _gameObjects.ForEach(gameObject => gameObject.LoadContent(Content));
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _gameObjects.RemoveAll(gameObject => gameObject.Destroyed);
        _gameObjects.ForEach(gameObject => gameObject.Update(gameTime));

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _gameObjects.ForEach(gameObject => gameObject.Draw(SpriteBatch, gameTime));

        base.Draw(gameTime);
    }
}
