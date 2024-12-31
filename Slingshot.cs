using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace puzzle_bobble;

public class Slingshot : GameObject
{
    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = -MathF.PI * 1.0f / 3.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 1.0f / 3.0f;
    protected float rotation = 0.0f;

    public Slingshot(Game game) : base("slingshot")
    {
        Position = new Vector2(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
        Scale = new Vector2(0.5f, 0.5f);
    }

    public override void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("Graphics/slingshot");
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: Implement `IsJustPressed` method for new InputManager class
        // This code executes multiple times per a short key press,
        // resulting in undesired behavior.
        //
        // if (Keyboard.GetState().IsKeyDown(Keys.H))
        //     IsActive = !IsActive;

        if (Keyboard.GetState().IsKeyDown(Keys.OemPlus))
            Scale += new Vector2(0.01f, 0.01f);
        if (Keyboard.GetState().IsKeyDown(Keys.OemMinus))
            Scale -= new Vector2(0.01f, 0.01f);

        if (!IsActive) return;

        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X;
        int mouseY = mouseState.Y;

        Vector2 direction = new Vector2(mouseX, mouseY) - Position;
        direction.Rotate(MathF.PI / 2.0f);
        rotation = MathF.Atan2(direction.Y, direction.X);
        rotation = MathHelper.Clamp(rotation, MIN_ROTATION, MAX_ROTATION);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Begin();
        spriteBatch.Draw(
            _texture,
            Position,
            null,
            Color.White,
            rotation,
            new Vector2(_texture.Width / 2, _texture.Height / 2),
            Scale,
            SpriteEffects.None,
            0
        );
        spriteBatch.End();
    }

}
