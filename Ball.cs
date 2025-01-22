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

    public void SetState(State state)
    {
        if (_state == state) return;

        switch (state)
        {
            case State.Exploding:
                // var randomPercent = _rand.NextSingle();
                if (explodeSfx is not null)
                    explodeSfx.Pitch = MAX_RANDOM_PITCH_RANGE * _rand.NextSingle() - (MAX_RANDOM_PITCH_RANGE / 2.0f);
                explosionAnimation?.Play(MAX_EXPLODE_DELAY * _rand.NextSingle());
                break;
            case State.Settle:
                shineAnimation?.Play();
                settleSfx?.Play();
                break;
            case State.Falling:
                Velocity = new Vector2(
                    (_rand.NextSingle() >= 0.5f ? -1 : 1) * _rand.NextSingle() * FALLING_SPREAD,
                    -_rand.NextSingle() * FALLING_SPREAD
                );
                break;
        }
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _spriteSheet = BallData.LoadBallSpritesheet(content);

        var explosionSheet = content.Load<Texture2D>("Graphics/balls_explode");
        explosionAnimation = new AnimatedTexture2D(explosionSheet, new Rectangle(0, Data.color * (explosionSheet.Height / 12), explosionSheet.Width, explosionSheet.Height / 12), 7, 1, 0.02f, false);
        explosionAnimation.Play();

        explodeSfx = content.Load<SoundEffect>($"Audio/Sfx/drop_00{_rand.Next(1, 4 + 1)}").CreateInstance();

        shineAnimation = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_shine"),
            9, 1, 0.01f, false
        );

        settleSfx = content.Load<SoundEffect>("Audio/Sfx/glass_002").CreateInstance();
    }

    public override List<GameObject> Update(GameTime gameTime, Vector2 parentTranslate)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case State.Idle:
                break;
            case State.Settle:
                if (shineAnimation is null) break;
                if (shineAnimation.IsFinished)
                    SetState(State.Idle);
                shineAnimation.Update(gameTime);
                break;
            case State.Moving:
                // TODO: fix this to take gameboard bounds into account
                UpdatePosition(gameTime);
                float right = 600 - Circle.radius;
                if (0 < Velocity.X && right < Position.X)
                {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                    Position = new Vector2(right - (Position.X - right), Position.Y); // reflect position across X=right
                }
                float left = -600 + Circle.radius;
                if (Velocity.X < 0 && Position.X < left)
                {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                    Position = new Vector2(left - (Position.X - left), Position.Y); // reflect position across X=left
                }
                float top = 600 - Circle.radius;
                if (0 < Velocity.Y && top < Position.Y)
                {
                    Velocity = new Vector2(Velocity.X, -Velocity.Y);
                    Position = new Vector2(Position.X, top - (Position.Y - top)); // reflect position across Y=top
                }
                float bottom = -600 + Circle.radius;
                if (Velocity.Y < 0 && Position.Y < bottom)
                {
                    Velocity = new Vector2(Velocity.X, -Velocity.Y);
                    Position = new Vector2(Position.X, bottom - (Position.Y - bottom)); // reflect position across Y=bottom
                }
                break;
            case State.Exploding:
                UpdatePosition(gameTime);
                Debug.Assert(explosionAnimation is not null);
                explosionAnimation.Update(gameTime);
                if (explosionAnimation.IsFinished)
                {
                    explodeSfx?.Play();
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
        return [];
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        Debug.Assert(_spriteSheet is not null);
        var scrPos = parentTranslate + Position;
        switch (_state)
        {
            case State.Exploding:
                Debug.Assert(explosionAnimation is not null);
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
            case State.Settle:
                Data.Draw(spriteBatch, _spriteSheet, scrPos);
                shineAnimation?.Draw(
                    spriteBatch,
                    new Rectangle((int)scrPos.X, (int)scrPos.Y, (int)(16 * Scale.X), (int)(16 * Scale.Y)),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(16 / 2, 16 / 2)
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
