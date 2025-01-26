using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Slingshot : GameObject
{

    private Texture2D? _slingshotTexture;
    private Texture2D? _ballSpriteSheet;
    private readonly float firerate; // shots per second
    private float _timeSinceLastFired;
    public BallData? Data = null;
    public float BallSpeed = 1000.0f; // IDEA: make this property upgradable

    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = MathF.PI * -80.0f / 180.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 80.0f / 180.0f;

    private const float MAX_RECOIL = 30.0f;
    private const float RECOIL_RECOVERY = 100.0f;
    private float visualRecoilOffset = 0.0f;

    public event BallFiredHandler? BallFired;
    public delegate void BallFiredHandler(Ball ball);

    public Slingshot(Game game) : base("slingshot")
    {
        Position = new Vector2(0, 300);
        Scale = new Vector2(3, 3);
        firerate = 3.0f;
        _timeSinceLastFired = 1 / firerate;
    }

    private ContentManager? _content;
    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        _slingshotTexture = content.Load<Texture2D>("Graphics/slingshot");
        _ballSpriteSheet = BallData.LoadBallSpritesheet(content);
        _content = content;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        // TODO: Implement `IsJustPressed` method for new InputManager class
        // This code executes multiple times per a short key press,
        // resulting in undesired behavior.
        //
        // if (Keyboard.GetState().IsKeyDown(Keys.H))
        //     IsActive = !IsActive;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeSinceLastFired += deltaTime;
        visualRecoilOffset = Math.Max(0.0f, visualRecoilOffset - RECOIL_RECOVERY * deltaTime);

        if (!IsActive) return;

        MouseState mouseState = Mouse.GetState();
        Vector2 direction = new Vector2(mouseState.X, mouseState.Y) - (Position + ParentTranslate);

        // +90 degrees to adjust for texture orientation
        direction.Rotate(MathF.PI / 2.0f);

        Rotation = MathF.Atan2(direction.Y, direction.X);
        Rotation = MathHelper.Clamp(Rotation, MIN_ROTATION, MAX_ROTATION);

        if (Data is BallData bd && mouseState.LeftButton == ButtonState.Pressed && _timeSinceLastFired > 1 / firerate)
        {
            // Rotate back to 0 degrees
            float targetRotation = Rotation - MathF.PI / 2.0f;
            Ball newBall = new(bd, Ball.State.Moving)
            {
                Position = Position,
                Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * BallSpeed,
                Scale = Scale,
            };
            // I'm thinking `BallFactory` class
            // then maybe `AbstractBallFactory` class
            // then maybe `AbstractBallFactorySingleton` class
            // then maybe `AbstractBallFactorySingletonBuilder` class
            // then burn the whole project to the ground
            Debug.Assert(_content is not null, "ContentManager is not initialized.");
            newBall.LoadContent(_content);

            BallFired?.Invoke(newBall);

            _timeSinceLastFired = 0.0f;
            // Cycle through ball colors, just a fun experimentation
            Data = null;

            visualRecoilOffset = MAX_RECOIL;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_slingshotTexture is not null, "Slingshot texture is not loaded.");
        Debug.Assert(_ballSpriteSheet is not null, "Ball sprite sheet is not loaded.");

        var scrPos = ParentTranslate + Position;
        spriteBatch.Draw(
            _slingshotTexture,
            scrPos + new Vector2(0, visualRecoilOffset),
            null,
            Color.White,
            0.0f,
            // anchors the texture from the top by 10 pixels no matter the height
            // so that the ball positioned in the center nicely.
            new Vector2(_slingshotTexture.Width / 2, 10),
            Scale,
            SpriteEffects.None,
            0
        );

        Data?.Draw(spriteBatch, _ballSpriteSheet, scrPos + new Vector2(0, visualRecoilOffset));
    }

}
