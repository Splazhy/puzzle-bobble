using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Slingshot : GameObject
{
    private Viewport _viewport;
    private Texture2D _slingshotTexture;
    private Texture2D _arrowTexture;
    private Texture2D _ballSpriteSheet;
    private float firerate; // shots per second
    private float _timeSinceLastFired;
    private Ball.Color _ballColor;
    public float BallSpeed = 1000.0f; // IDEA: make this property upgradable

    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = MathF.PI * -80.0f / 180.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 80.0f / 180.0f;

    public event BallFiredHandler BallFired;
    public delegate void BallFiredHandler(Ball ball);

    public Slingshot(Game game) : base("slingshot")
    {
        Position = new Vector2(0, 300);
        Scale = new Vector2(3, 3);
        firerate = 3.0f;
        _timeSinceLastFired = 1 / firerate;
    }

    public override void LoadContent(ContentManager content)
    {
        _slingshotTexture = content.Load<Texture2D>("Graphics/slingshot");
        _ballSpriteSheet = content.Load<Texture2D>("Graphics/balls");
        _arrowTexture = content.Load<Texture2D>("Graphics/arrow");
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
        int mouseX = mouseState.X - (int)VirtualOrigin.X;
        int mouseY = mouseState.Y - (int)VirtualOrigin.Y;

        Vector2 direction = new Vector2(mouseX, mouseY) - Position;

        // +90 degrees to adjust for texture orientation
        direction.Rotate(MathF.PI / 2.0f);

        Rotation = MathF.Atan2(direction.Y, direction.X);
        Rotation = MathHelper.Clamp(Rotation, MIN_ROTATION, MAX_ROTATION);

        if (mouseState.LeftButton == ButtonState.Pressed && _timeSinceLastFired > 1 / firerate)
        {
            // Rotate back to 0 degrees
            float targetRotation = Rotation - MathF.PI / 2.0f;
            Ball newBall = new Ball(_ballColor, _viewport);
            newBall.Position = Position;
            newBall.Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * BallSpeed;
            newBall.Scale = Scale;
            BallFired?.Invoke(newBall);
            _timeSinceLastFired = 0.0f;
            // Cycle through ball colors, just a fun experimentation
            _ballColor = (Ball.Color)(((int)_ballColor + 1) % Enum.GetNames(typeof(Ball.Color)).Length);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw(
            _slingshotTexture,
            ScreenPosition,
            null,
            Color.White,
            0.0f,
            new Vector2(_slingshotTexture.Width / 2, _slingshotTexture.Height / 2),
            Scale,
            SpriteEffects.None,
            0
        );
        spriteBatch.Draw(
            _arrowTexture,
            ScreenPosition,
            null,
            Color.White,
            Rotation,
            new Vector2(_arrowTexture.Width / 2, 20),
            Scale,
            SpriteEffects.None,
            0
        );
        spriteBatch.Draw(
            _ballSpriteSheet,
            new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, 48, 48),
            new Rectangle((int)_ballColor * 16, 0, 16, 16),
            Microsoft.Xna.Framework.Color.White,
            0.0f,
            new Vector2(16 / 2, 16 / 2),
            SpriteEffects.None,
            0
        );
    }

}
