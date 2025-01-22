using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class Ball : GameObject
{
    public enum Color
    {
        // The order of the colors is significant,
        // as it is connected to the sprite sheet.
        Red = 0,
        Orange,
        Yellow,
        Green,
        Teal,
        Sky,
        Blue,
        Lavender,
        Purple,
        Pink,
        White,
        Black,
    }

    public enum State
    {
        Idle,
        Moving,
        Falling,
        Exploding,
        Settle,
    }

    public const float FALLING_SPREAD = 50;
    public const float MAX_EXPLODE_DELAY = 0.2f;
    public const float MAX_RANDOM_PITCH_RANGE = 0.2f;

    private static Random _rand = new Random();
    private Texture2D? _spriteSheet;
    private AnimatedTexture2D? explosionAnimation;
    private AnimatedTexture2D? shineAnimation;

    private SoundEffectInstance? explodeSfx;
    private SoundEffectInstance? settleSfx;
    private SoundEffectInstance? bounceSfx;

    public Circle Circle
    {
        get
        {
            Debug.Assert(Scale.X == Scale.Y, "Non-uniform scaling is not supported for collision detection.");
            // We use sprite sheet now, so this assertion is no longer valid
            // Debug.Assert(_texture.Width == _texture.Height, "Non-square textures are not supported for collision detection.");
            return new Circle(GlobalPosition, 16 / 2 * Scale.X);
        }
    }
    private Color _color;
    public Color GetColor() { return _color; }

    private State _state;
    public State GetState() { return _state; }

    private static readonly Vector2 GRAVITY = new Vector2(0, 9.8f * 100);

    public Ball(Color ballType, State state) : base("ball")
    {
        _color = ballType;
        _state = state;
    }

    public void SetState(State state)
    {
        if (_state == state) return;

        switch (state)
        {
            case State.Idle:
                Velocity = Vector2.Zero;
                break;
            case State.Exploding:
                Debug.Assert(explodeSfx is not null, "Explode sound effect is not loaded.");
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                explodeSfx.Pitch = MAX_RANDOM_PITCH_RANGE * _rand.NextSingle() - (MAX_RANDOM_PITCH_RANGE / 2.0f);
                explosionAnimation.Play(MAX_EXPLODE_DELAY * _rand.NextSingle());
                break;
            case State.Settle:
                Debug.Assert(settleSfx is not null, "Settle sound effect is not loaded.");
                Debug.Assert(shineAnimation is not null, "Shine animation is not loaded.");
                settleSfx.Play();
                shineAnimation.Play();
                break;
            case State.Falling:
                Velocity = new Vector2(
                    (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD,
                    -_rand.NextSingle() * FALLING_SPREAD
                );
                break;
        }
        _state = state;
    }

    public override void LoadContent(ContentManager content)
    {
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _spriteSheet = content.Load<Texture2D>("Graphics/balls");

        explosionAnimation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/balls_explode"),
            7, 12, 0.02f, false
        );
        explosionAnimation.SetVFrame((int)_color);
        explodeSfx = content.Load<SoundEffect>($"Audio/Sfx/drop_00{_rand.Next(1, 4 + 1)}").CreateInstance();

        shineAnimation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_shine"),
            9, 1, 0.01f, false
        );
        settleSfx = content.Load<SoundEffect>("Audio/Sfx/glass_002").CreateInstance();

        bounceSfx = content.Load<SoundEffect>("Audio/Sfx/bong_001").CreateInstance();

        base.LoadContent(content);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case State.Idle:
                break;
            case State.Settle:
                Debug.Assert(shineAnimation is not null, "Shine animation is not loaded.");
                if (shineAnimation.IsFinished)
                    SetState(State.Idle);
                shineAnimation.Update(gameTime);
                break;
            case State.Moving:
                if (
                    (Position.X - Circle.radius < -192 && Velocity.X < 0) ||
                    (192 < Position.X + Circle.radius && 0 < Velocity.X)
                ) {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                    Debug.Assert(bounceSfx is not null, "Bounce sound effect is not loaded.");
                    bounceSfx.Volume = MathF.Abs(Vector2.Dot(Vector2.Normalize(Velocity), Vector2.UnitX));
                    bounceSfx.Play();
                }
                Position += Velocity * deltaTime;
                break;
            case State.Exploding:
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                explosionAnimation.Update(gameTime);
                if (explosionAnimation.IsFinished)
                {
                    explodeSfx?.Play();
                    Destroy();
                }
                break;
            case State.Falling:
                if (
                    (Position.X - Circle.radius < -192 && Velocity.X < 0) ||
                    (192 < Position.X + Circle.radius && 0 < Velocity.X)
                ) {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                    Debug.Assert(bounceSfx is not null, "Bounce sound effect is not loaded.");
                    bounceSfx.Volume = MathF.Abs(Vector2.Dot(Vector2.Normalize(Velocity), Vector2.UnitX));
                    bounceSfx.Play();
                }
                Velocity += GRAVITY * deltaTime;
                Position += Velocity * deltaTime;
                if (Position.Y > 1000) // TODO: remove this later when we handle this in GameScene
                    Destroy();
                break;
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        switch (_state)
        {
            case State.Exploding:
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                explosionAnimation.Draw(
                    spriteBatch,
                    new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, (int)(32 * Scale.X), (int)(32 * Scale.Y)),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(32 / 2, 32 / 2)
                );
                break;
            case State.Settle:
                drawBall(spriteBatch, gameTime);
                shineAnimation?.Draw(
                    spriteBatch,
                    new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, (int)(16 * Scale.X), (int)(16 * Scale.Y)),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(16 / 2, 16 / 2)
                );
                break;
            default:
                drawBall(spriteBatch, gameTime);
                break;
        }

        base.Draw(spriteBatch, gameTime);
    }

    private void drawBall(SpriteBatch spriteBatch, GameTime gameTime)
    {
        var ScreenPosition = GlobalPosition + Game1.WindowCenter;
        spriteBatch.Draw(
            _spriteSheet,
            new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, (int)(16 * Scale.X), (int)(16 * Scale.Y)),
            new Rectangle((int)_color * 16, 0, 16, 16),
            Microsoft.Xna.Framework.Color.White,
            0.0f,
            new Vector2(16 / 2, 16 / 2),
            SpriteEffects.None,
            0
        );
    }

    public bool IsCollideWith(Ball other)
    {
        return Circle.Intersects(other.Circle) > 0;
    }

    public bool IsCollideWith(Circle other)
    {
        return Circle.Intersects(other) > 0;
    }
}
