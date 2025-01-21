using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PuzzleBobble;

public class Slingshot : GameObject
{
    public class Guideline
    {
        private readonly Texture2D _texture;
        private readonly float _length;
        private readonly float _duration; // in seconds
        private float _progress; // 0.0f to 1.0f
        private readonly int _count;
        private float _timePassed;
        private Vector2 _origin;

        public Guideline(Texture2D texture, int drawCount, float lineLength, float loopDuration)
        {
            _texture = texture;
            _length = lineLength;
            _duration = loopDuration;
            _count = drawCount;
            _progress = 0.0f;
            _timePassed = 0.0f;
            _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
        }

        public void Update(GameTime gameTime)
        {
            _timePassed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _progress = _timePassed / _duration;
            if (_progress > 1.0f)
            {
                _progress = 0.0f;
                _timePassed = 0.0f;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 startPoint, float rotation, Vector2 scale)
        {
            var endPoint = new Vector2(
                startPoint.X + _length * MathF.Cos(rotation),
                startPoint.Y + _length * MathF.Sin(rotation)
            );

            for (int i = 0; i < _count; i++)
            {
                var subProgress = (_progress + (float)i / _count) % 1.0f;
                var subPosition = new Vector2(
                    startPoint.X + subProgress * (endPoint.X - startPoint.X),
                    startPoint.Y + subProgress * (endPoint.Y - startPoint.Y)
                );
                var actualScale = scale * (1.0f - subProgress);
                spriteBatch.Draw(
                    _texture,
                    subPosition,
                    null,
                    Color.White,
                    0,
                    _origin,
                    actualScale,
                    SpriteEffects.None,
                    0
                );
            }
        }
    }

    private Texture2D? _slingshotTexture;
    private Guideline? _guideline;
    private Texture2D? _ballSpriteSheet;
    private readonly float firerate; // shots per second
    private float _timeSinceLastFired;
    private BallData _ballData = new(0);
    public float BallSpeed = 1000.0f; // IDEA: make this property upgradable

    // Rotations are in radians, not degrees
    public static readonly float MIN_ROTATION = MathF.PI * -80.0f / 180.0f;
    public static readonly float MAX_ROTATION = MathF.PI * 80.0f / 180.0f;

    public event BallFiredHandler? BallFired;
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
        _guideline = new Guideline(content.Load<Texture2D>("Graphics/guideline"), 6, 120.0f, 3.0f);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        // TODO: Implement `IsJustPressed` method for new InputManager class
        // This code executes multiple times per a short key press,
        // resulting in undesired behavior.
        //
        // if (Keyboard.GetState().IsKeyDown(Keys.H))
        //     IsActive = !IsActive;

        _timeSinceLastFired += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _guideline?.Update(gameTime);

        MouseState mouseState = Mouse.GetState();
        Vector2 direction = new Vector2(mouseState.X, mouseState.Y) - (Position + parentTranslate);

        // +90 degrees to adjust for texture orientation
        direction.Rotate(MathF.PI / 2.0f);

        Rotation = MathF.Atan2(direction.Y, direction.X);
        Rotation = MathHelper.Clamp(Rotation, MIN_ROTATION, MAX_ROTATION);

        if (mouseState.LeftButton == ButtonState.Pressed && _timeSinceLastFired > 1 / firerate)
        {
            // Rotate back to 0 degrees
            float targetRotation = Rotation - MathF.PI / 2.0f;
            Ball newBall = new(_ballData, Ball.State.Moving)
            {
                Position = Position,
                Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * BallSpeed,
                Scale = Scale,
            };
            BallFired?.Invoke(newBall);
            _timeSinceLastFired = 0.0f;
            // Cycle through ball colors, just a fun experimentation
            _ballData = new((_ballData.color + 1) % Enum.GetNames(typeof(Ball.Color)).Length);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        if (_slingshotTexture is null || _ballSpriteSheet is null || _guideline is null)
        {
            return;
        }
        var scrPos = parentTranslate + Position;
        _guideline.Draw(spriteBatch, scrPos, Rotation - MathF.PI / 2, Scale);

        spriteBatch.Draw(
            _slingshotTexture,
            scrPos,
            null,
            Color.White,
            0.0f,
            new Vector2(_slingshotTexture.Width / 2, _slingshotTexture.Height / 2),
            Scale,
            SpriteEffects.None,
            0
        );

        _ballData.Draw(spriteBatch, _ballSpriteSheet, scrPos);
    }

}
