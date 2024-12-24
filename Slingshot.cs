using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace puzzle_bobble;

public class Slingshot : DrawableGameComponent
{
    // Rotations in this class are in radians
    public static readonly float MIN_ROTATION = -MathF.PI * 1.0f / 3.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 1.0f / 3.0f;
    protected float rotation = 0.0f;

    private Texture2D _texture;
    private SpriteBatch _spriteBatch;
    private Vector2 slingshotPosition;

    public Slingshot(Game game) : base(game)
    {
        slingshotPosition = new Vector2(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2);
    }

    public override void Initialize()
    {
        Console.WriteLine("initialize");
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        _texture = Game.Content.Load<Texture2D>("slingshot");

        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X;
        int mouseY = mouseState.Y;

        Vector2 direction = new Vector2(mouseX, mouseY) - slingshotPosition;
        direction.Rotate(MathF.PI / 2.0f);
        rotation = MathF.Atan2(direction.Y, direction.X);
        rotation = MathHelper.Clamp(rotation, MIN_ROTATION, MAX_ROTATION);
    }

    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        _spriteBatch.Draw(
            _texture,
            slingshotPosition,
            null,
            Color.White,
            rotation,
            new Vector2(_texture.Width / 2, _texture.Height / 2),
            1.0f,
            SpriteEffects.None,
            0
        );
        _spriteBatch.End();

        base.Draw(gameTime);
    }

}
