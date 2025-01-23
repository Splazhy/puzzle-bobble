using System;
using System.Collections.Generic;
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
    }

    public const float FALLING_SPREAD = 50;
    public const float MAX_EXPLODE_DELAY = 0.2f;
    public const float MAX_RANDOM_PITCH_RANGE = 0.2f;

    private static Random _rand = new Random();
    private Texture2D? _spriteSheet;
    private AnimatedTexture2D? explosionAnimation;

    private bool _soundPlayed = false;
    private float _soundDelay = 0.0f;

    private SoundEffectInstance? explodeSfx;
    private SoundEffectInstance? bounceSfx;

    public Circle Circle
    {
        get
        {
            Debug.Assert(Scale.X == Scale.Y, "Non-uniform scaling is not supported for collision detection.");
            // We use sprite sheet now, so this assertion is no longer valid
            // Debug.Assert(_texture.Width == _texture.Height, "Non-square textures are not supported for collision detection.");
            return new Circle(Position, 16 / 2 * Scale.X);
        }
    }
    public BallData Data { get; private set; }
    private readonly State _state; public State GetState() { return _state; }

    private static readonly Vector2 GRAVITY = new(0, 9.8f * 100);

    public Ball(BallData data, State state) : base("ball")
    {
        Data = data;
        _state = state;
        Scale = new Vector2(3, 3);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _spriteSheet = BallData.LoadBallSpritesheet(content);

        explosionAnimation = Data.CreateExplosionAnimation(content);
        float delay = MAX_EXPLODE_DELAY * _rand.NextSingle();
        explosionAnimation.Play(delay);

        explodeSfx = content.Load<SoundEffect>($"Audio/Sfx/drop_00{_rand.Next(1, 4 + 1)}").CreateInstance();
        explodeSfx.Pitch = MAX_RANDOM_PITCH_RANGE * _rand.NextSingle() - (MAX_RANDOM_PITCH_RANGE / 2.0f);
        _soundDelay = delay;

        bounceSfx = content.Load<SoundEffect>("Audio/Sfx/bong_001").CreateInstance();

        base.LoadContent(content);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case State.Idle:
                break;
            case State.Moving:
                UpdatePosition(gameTime);
                break;
            case State.Exploding:
                UpdatePosition(gameTime);
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                if (!_soundPlayed)
                {
                    if (_soundDelay <= 0)
                    {
                        explodeSfx?.Play();
                        _soundPlayed = true;
                    }
                    else
                    {
                        _soundDelay -= deltaTime;
                    }
                }
                explosionAnimation.Update(gameTime);
                if (explosionAnimation.IsFinished)
                {
                    Destroy();
                }
                break;
            case State.Falling:
                Velocity += GRAVITY * deltaTime;
                UpdatePosition(gameTime);
                if (Position.Y > 1000) // TODO: remove this later when we handle this in GameScene
                    Destroy();
                break;
        }
    }

    public void BounceOverX(float x)
    {
        Velocity = new Vector2(-Velocity.X, Velocity.Y);
        Position = new Vector2(x - (Position.X - x), Position.Y);

        Debug.Assert(bounceSfx is not null, "Bounce sound effect is not loaded.");
        bounceSfx.Volume = MathF.Abs(Vector2.Dot(Vector2.Normalize(Velocity), Vector2.UnitX));
        bounceSfx.Play();
    }

    public void BounceOverY(float y)
    {
        Velocity = new Vector2(Velocity.X, -Velocity.Y);
        Position = new Vector2(Position.X, y - (Position.Y - y));

        Debug.Assert(bounceSfx is not null, "Bounce sound effect is not loaded.");
        bounceSfx.Volume = MathF.Abs(Vector2.Dot(Vector2.Normalize(Velocity), Vector2.UnitY));
        bounceSfx.Play();
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_spriteSheet is not null);
        var scrPos = ParentTranslate + Position;
        switch (_state)
        {
            case State.Exploding:
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                explosionAnimation.Draw(
                    spriteBatch,
                    // FIXME: this position is not accurate (the y position is off by a bit)
                    // might be due to floating point precision errors of GameBoard.
                    new Rectangle((int)scrPos.X, (int)scrPos.Y, (int)(32 * Scale.X), (int)(32 * Scale.Y)),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(32 / 2, 32 / 2)
                );
                break;
            default:
                Data.Draw(spriteBatch, _spriteSheet, scrPos);
                break;
        }
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
