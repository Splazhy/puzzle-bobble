using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Game1 : Game
{
    public SpriteFont? Font { get; private set; }
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SceneManager _sceneManager;
    private FrameCounter _frameCounter = new FrameCounter();
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        // _graphics.PreferredBackBufferWidth = 1280;
        // _graphics.PreferredBackBufferHeight = 720;

        // change framerate to vsync
        _graphics.SynchronizeWithVerticalRetrace = true;
        IsFixedTimeStep = false;

        _sceneManager = new SceneManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        GameObject.VirtualOrigin = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);

        Window.ClientSizeChanged += Window_ClientSizeChanged;

    }

    protected override void Initialize()
    {
        Myra.MyraEnvironment.Game = this;
        _sceneManager.Initialize(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Font = Content.Load<SpriteFont>("Fonts/Arial24");
        _sceneManager.LoadContent(this.Content);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _sceneManager.Update(gameTime);
        _frameCounter.Update(gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_spriteBatch is null)
        {
            return;
        }

        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // TODO: hide/show these using debug options
        _spriteBatch.DrawString(Font, $"Update Time: {_frameCounter.LastTimeSample}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(Font, $"FPS: {_frameCounter.AverageFramesPerSecond}", new Vector2(10, 40), Color.White);

        _sceneManager.Draw(_spriteBatch, gameTime);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void Window_ClientSizeChanged(object? sender, System.EventArgs e)
    {
        Window.ClientSizeChanged -= Window_ClientSizeChanged;

        // TODO: code that needs to be run on window size change
        GameObject.VirtualOrigin = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
        Window.ClientSizeChanged += Window_ClientSizeChanged;
    }
}
