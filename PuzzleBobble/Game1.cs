using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Game1 : Game
{
    public SpriteFont? Font { get; private set; }
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private readonly SceneManager _sceneManager;
    private readonly FrameCounter _frameCounter = new();
    private Vector2 _screenCenter;

    private TimeSpan _updateTimeMeasured;
    private TimeSpan _drawOrdersTimeMeasured;
    private TimeSpan _drawCallTimeMeasured;
    private readonly Stopwatch _stopwatch = new();
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1600,
            PreferredBackBufferHeight = 900,

            // change framerate to vsync
            SynchronizeWithVerticalRetrace = true
        };
        IsFixedTimeStep = false;

        _sceneManager = new SceneManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        Window.ClientSizeChanged += Window_ClientSizeChanged;

    }

    protected override void Initialize()
    {
        Myra.MyraEnvironment.Game = this;
        _sceneManager.Initialize(this);

        base.Initialize();
        _screenCenter = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Font = Content.Load<SpriteFont>("Fonts/Arial24");
        _sceneManager.LoadContent(this.Content);
    }

    protected override void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        _stopwatch.Restart();
        _sceneManager.Update(gameTime, _screenCenter);
        _stopwatch.Stop();
        _updateTimeMeasured = _stopwatch.Elapsed;

        _frameCounter.Update(gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Debug.Assert(_spriteBatch is not null, "SpriteBatch is not loaded.");

        GraphicsDevice.Clear(Color.RosyBrown);

        _spriteBatch.Begin(samplerState: SamplerState.PointWrap, blendState: BlendState.AlphaBlend);
        // TODO: hide/show these using debug options
        _spriteBatch.DrawString(Font, $"Update Time: {_updateTimeMeasured.TotalMilliseconds:#.###}ms", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(Font, $"Draw Orders Time: {_drawOrdersTimeMeasured.TotalMilliseconds:#.###}ms", new Vector2(10, 40), Color.White);
        _spriteBatch.DrawString(Font, $"Draw Call Time: {_drawCallTimeMeasured.TotalMilliseconds:#.###}ms", new Vector2(10, 70), Color.White);
        _spriteBatch.DrawString(Font, $"FPS: {_frameCounter.AverageFramesPerSecond}", new Vector2(10, 100), Color.White);

        _stopwatch.Restart();
        _sceneManager.Draw(_spriteBatch, gameTime);
        _stopwatch.Stop();
        _drawOrdersTimeMeasured = _stopwatch.Elapsed;

        _stopwatch.Restart();
        _spriteBatch.End();
        _stopwatch.Stop();
        _drawCallTimeMeasured = _stopwatch.Elapsed;

        _sceneManager.DrawMyra()?.Render();

        base.Draw(gameTime);
    }

    private void Window_ClientSizeChanged(object? sender, System.EventArgs e)
    {
        Window.ClientSizeChanged -= Window_ClientSizeChanged;

        // TODO: code that needs to be run on window size change
        _screenCenter = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
        Window.ClientSizeChanged += Window_ClientSizeChanged;
    }
}
