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

        // 192 -> gameboard border
        // 24 -> ball radius
        private static float LeftBorder { get { return Game1.WindowCenter.X - 192 + 24; } }
        private static float RightBorder { get { return Game1.WindowCenter.X + 192 - 24; } }
        public void Draw(SpriteBatch spriteBatch, Vector2 startPoint, float rotation, Vector2 scale)
        {
            var direction = Vector2.Normalize( new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)));
            for (int i = 0; i < _count; i++)
            {
                var subProgress = (_progress + (float)i / _count) % 1.0f;
                var actualScale = scale * (1.0f - subProgress);
                var lengthLeft = _length * subProgress;
                var tmpDirection = direction;
                var s = startPoint;
                Vector2 subPosition;
                while (true)
                {
                    var e = s + (tmpDirection * lengthLeft);
                    var slope = (e.Y - s.Y) / (e.X - s.X);
                    Vector2? bouncePoint = null;

                    if (e.X > s.X && e.X > RightBorder)
                    {
                        bouncePoint = new Vector2(RightBorder, slope * (RightBorder - s.X) + s.Y);
                    }
                    else if (e.X < s.X && e.X < LeftBorder)
                    {
                        bouncePoint = new Vector2(LeftBorder, slope * (LeftBorder - s.X) + s.Y);
                    }

                    lengthLeft -= Vector2.Distance(s, bouncePoint ?? e);
                    s = bouncePoint ?? e;
                    tmpDirection = new Vector2(-tmpDirection.X, tmpDirection.Y);
                    if (bouncePoint is null || lengthLeft <= 0)
                    {
                        subPosition = e;
                        break;
                    }
                }
                spriteBatch.Draw(
                    _texture,
                    subPosition,
                    null,
                    Color.White * (0.25f * (1.0f - subProgress)),
                    0,
                    _origin,
                    scale,
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
        _guideline = new Guideline(
            content.Load<Texture2D>("Graphics/guideline_full"),
            24, 1200.0f, 15.0f
        );

        _content = content;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
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
        Vector2 direction = new Vector2(mouseState.X, mouseState.Y) - (Position + parentTranslate);

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
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        Debug.Assert(_slingshotTexture is not null, "Slingshot texture is not loaded.");
        Debug.Assert(_ballSpriteSheet is not null, "Ball sprite sheet is not loaded.");
        Debug.Assert(_guideline is not null, "Guideline is not loaded.");

        var scrPos = parentTranslate + Position;
        _guideline.Draw(spriteBatch, scrPos, Rotation - MathF.PI / 2, Scale);
        spriteBatch.Draw(
            _slingshotTexture,
            scrPos,
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

        Data?.Draw(spriteBatch, _ballSpriteSheet, scrPos);
    }

}
