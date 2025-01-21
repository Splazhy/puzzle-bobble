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
        private Texture2D _texture;
        private float _length;
        private float _duration; // in seconds
        private float _progress; // 0.0f to 1.0f
        private int _count;
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
    private float firerate; // shots per second
    private float _timeSinceLastFired;
    private Ball.Color _ballColor;
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

    public override void LoadContent(ContentManager content)
    {
        _slingshotTexture = content.Load<Texture2D>("Graphics/slingshot");
        _ballSpriteSheet = content.Load<Texture2D>("Graphics/balls");
        _guideline = new Guideline(content.Load<Texture2D>("Graphics/guideline"), 6, 120.0f, 3.0f);
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: Implement `IsJustPressed` method for new InputManager class
        // This code executes multiple times per a short key press,
        // resulting in undesired behavior.
        //
        // if (Keyboard.GetState().IsKeyDown(Keys.H))
        //     IsActive = !IsActive;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeSinceLastFired += deltaTime;
        visualRecoilOffset = Math.Max(0.0f, visualRecoilOffset - RECOIL_RECOVERY * deltaTime);

        _guideline?.Update(gameTime);

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
            Ball newBall = new Ball(_ballColor, Ball.State.Moving)
            {
                Position = Position,
                Velocity = new Vector2(MathF.Cos(targetRotation), MathF.Sin(targetRotation)) * BallSpeed,
                Scale = Scale,
            };
            BallFired?.Invoke(newBall);
            _timeSinceLastFired = 0.0f;
            // Cycle through ball colors, just a fun experimentation
            _ballColor = (Ball.Color)(((int)_ballColor + 1) % Enum.GetNames(typeof(Ball.Color)).Length);
            visualRecoilOffset = MAX_RECOIL;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (_slingshotTexture is null || _ballSpriteSheet is null || _guideline is null)
        {
            return;
        }
        _guideline.Draw(spriteBatch, ScreenPosition, Rotation - MathF.PI / 2, Scale);

        spriteBatch.Draw(
            _slingshotTexture,
            new Vector2(ScreenPosition.X, ScreenPosition.Y + visualRecoilOffset),
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

        spriteBatch.Draw(
            _ballSpriteSheet,
            new Rectangle((int)ScreenPosition.X, (int)(ScreenPosition.Y + visualRecoilOffset), 48, 48),
            new Rectangle((int)_ballColor * 16, 0, 16, 16),
            Color.White,
            0.0f,
            new Vector2(16 / 2, 16 / 2),
            SpriteEffects.None,
            0
        );
    }

}
