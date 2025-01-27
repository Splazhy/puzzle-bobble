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
    public enum State
    {
        Stasis,
        Moving,
        Falling,
        Exploding,
    }

    public const float FALLING_SPREAD = 50;
    public const float MAX_EXPLODE_DELAY = 0.2f;
    public const float MAX_RANDOM_PITCH_RANGE = 0.2f;

    private static readonly Random _rand = new();
    private AnimatedTexture2D? explosionAnimation;
    private AnimatedTexture2D? _previewBallSpriteSheet;
    public Vector2? EstimatedCollisionPosition;

    private bool _soundPlayed = false;
    private float _soundDelay = 0.0f;

    private SoundEffectInstance? explodeSfx;
    private SoundEffectInstance? bounceSfx;
    public BallData Data { get; private set; }
    private BallData.Assets? _ballAssets;
    private State _state; public State GetState() { return _state; }

    private static readonly Vector2 GRAVITY = new(0, 9.8f * 100);

    public Ball(BallData data, State state) : base("ball")
    {
        Data = data;
        _state = state;
    }

    public void SetStasis()
    {
        Debug.Assert(_state == State.Moving, "Cannot set state to stasis when not moving.");
        _state = State.Stasis;
    }

    public void Unstasis()
    {
        Debug.Assert(_state == State.Stasis, "Cannot set state to moving when not in stasis.");
        _state = State.Moving;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _ballAssets = new BallData.Assets(content);
        Data.LoadAnimation(_ballAssets);

        explosionAnimation = Data.CreateExplosionAnimation(_ballAssets);
        float delay = MAX_EXPLODE_DELAY * _rand.NextSingle();
        explosionAnimation.TriggerPlayOnNextDraw(delay);

        explodeSfx = content.Load<SoundEffect>($"Audio/Sfx/drop_00{_rand.Next(1, 4 + 1)}").CreateInstance();
        explodeSfx.Pitch = MAX_RANDOM_PITCH_RANGE * _rand.NextSingle() - (MAX_RANDOM_PITCH_RANGE / 2.0f);
        _soundDelay = delay;

        bounceSfx = content.Load<SoundEffect>("Audio/Sfx/bong_001").CreateInstance();

        _previewBallSpriteSheet = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_preview"),
            4, 1, 0.1f, true);
        _previewBallSpriteSheet.TriggerPlayOnNextDraw();
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case State.Stasis:
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
                if (explosionAnimation.IsFinished(gameTime))
                {
                    Destroy();
                }
                break;
            case State.Falling:
                Velocity += GRAVITY * deltaTime;
                UpdatePosition(gameTime);
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
        Debug.Assert(_ballAssets is not null);

        if (EstimatedCollisionPosition is Vector2 ep)
        {
            Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball sprite sheet is not loaded.");
            Data.Draw(spriteBatch, gameTime, _ballAssets, ParentTranslate + ep, 0.5f);
            _previewBallSpriteSheet.Draw(
                spriteBatch,
                gameTime,
                new Rectangle((int)(ParentTranslate.X + ep.X), (int)(ParentTranslate.Y + ep.Y), BallData.BALL_SIZE, BallData.BALL_SIZE),
                Microsoft.Xna.Framework.Color.White * 0.75f,
                0.0f,
                new Vector2(BallData.BALL_TEXTURE_SIZE / 2, BallData.BALL_TEXTURE_SIZE / 2)
            );
        }


        switch (_state)
        {
            case State.Exploding:
                Debug.Assert(explosionAnimation is not null, "Explosion animation is not loaded.");
                explosionAnimation.Draw(
                    spriteBatch,
                    gameTime,
                    // FIXME: this position is not accurate (the y position is off by a bit)
                    // might be due to floating point precision errors of GameBoard.
                    new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, (int)(BallData.EXPLOSION_TEXTURE_SIZE * PixelScale.X), (int)(BallData.EXPLOSION_TEXTURE_SIZE * PixelScale.Y)),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(BallData.EXPLOSION_TEXTURE_SIZE / 2, BallData.EXPLOSION_TEXTURE_SIZE / 2)
                );
                break;
            case State.Stasis:
                Data.Draw(spriteBatch, gameTime, _ballAssets, ScreenPosition, 0.75f);
                break;
            default:
                Data.Draw(spriteBatch, gameTime, _ballAssets, ScreenPosition);
                break;
        }
    }
}
