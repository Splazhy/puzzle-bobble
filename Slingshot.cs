using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Slingshot : GameObject
{
    private Texture2D _texture;
    private float firerate; // shots per second
    private float _timeSinceLastFired;

    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = -MathF.PI * 1.0f / 3.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 1.0f / 3.0f;

    public event BallFiredHandler BallFired;
    public delegate void BallFiredHandler(Ball ball);

    public Slingshot(Game game) : base("slingshot")
    {
        Position = new Vector2(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 1.125f);
        Scale = new Vector2(0.5f, 0.5f);
        firerate = 3.0f;
        _timeSinceLastFired = 1 / firerate;
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

        _timeSinceLastFired += (float)gameTime.ElapsedGameTime.TotalSeconds;

        MouseState mouseState = Mouse.GetState();
        int mouseX = mouseState.X;
        int mouseY = mouseState.Y;

        Vector2 direction = new Vector2(mouseX, mouseY) - Position;

        // +90 degrees to adjust for texture orientation
        direction.Rotate(MathF.PI / 2.0f);

        Rotation = MathF.Atan2(direction.Y, direction.X);
        Rotation = MathHelper.Clamp(Rotation, MIN_ROTATION, MAX_ROTATION);

        if (mouseState.LeftButton == ButtonState.Pressed && _timeSinceLastFired > 1 / firerate)
        {
            // Rotate back to 0 degrees
            float targetRotation = Rotation - MathF.PI / 2.0f;
            Ball newBall = new Ball(Ball.Color.Brown);
            newBall.Position = Position;
            newBall.Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * 500;
            newBall.Scale = Scale;
            BallFired?.Invoke(newBall);
            _timeSinceLastFired = 0.0f;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw(
            _texture,
            Position,
            null,
            Color.White,
            Rotation,
            new Vector2(_texture.Width / 2, _texture.Height / 2),
            Scale,
            SpriteEffects.None,
            0
        );
    }

}
