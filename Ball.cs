using System.Diagnostics;
using Microsoft.Xna.Framework;
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

    private Texture2D? _spriteSheet;
    private AnimatedTexture2D? _explosionSpriteSheet;
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
    private Color _color; public Color GetColor() { return _color; }
    private State _state; public State GetState() { return _state; }

    private static readonly Vector2 GRAVITY = new Vector2(0, 9.8f * 100);

    public Ball(Color ballType, State state) : base("ball")
    {
        _color = ballType;
        _state = state;
    }

    public override void LoadContent(ContentManager content)
    {
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _spriteSheet = content.Load<Texture2D>("Graphics/balls");
        _explosionSpriteSheet = new AnimatedTexture2D(content.Load<Texture2D>("Graphics/balls_explode"), 7, 12, 0.02f, false);
        _explosionSpriteSheet.SetVFrame((int)_color);
        _explosionSpriteSheet.Play();
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (_state)
        {
            case State.Idle:
                break;
            case State.Moving:
                // TODO: fix this to take gameboard bounds into account
                if (Position.X - Circle.radius < -600 || 600 < Position.X + Circle.radius)
                {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                }
                if (Position.Y - Circle.radius < -600 || 600 < Position.Y + Circle.radius)
                {
                    Velocity = new Vector2(Velocity.X, -Velocity.Y);
                }
                Position += Velocity * deltaTime;
                break;
            case State.Exploding:
                if (_explosionSpriteSheet is null) break;
                _explosionSpriteSheet.Update(gameTime);
                if (_explosionSpriteSheet.IsFinished)
                    Destroy();
                break;
            case State.Falling:
                Velocity += GRAVITY * deltaTime;
                Position += Velocity * deltaTime;
                if (Position.Y > 1000) // TODO: remove this later when we handle this in GameScene
                    Destroy();
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        switch (_state)
        {
            case State.Exploding:
                if (_explosionSpriteSheet is null) break;
                _explosionSpriteSheet.Draw(
                    spriteBatch,
                    // FIXME: this position is not accurate (the y position is off by a bit)
                    // might be due to floating point precision errors of GameBoard.
                    new Vector2(ScreenPosition.X - 48, ScreenPosition.Y - 48),
                    0.0f,
                    Vector2.Zero,
                    Scale.X, // we assume that scale is uniform
                    Microsoft.Xna.Framework.Color.White
                );
                break;
            default:
                spriteBatch.Draw(
                    _spriteSheet,
                    // FIXME: this one has the same issue as above
                    new Rectangle((int)ScreenPosition.X, (int)ScreenPosition.Y, (int)(16 * Scale.X), (int)(16 * Scale.Y)),
                    new Rectangle((int)_color * 16, 0, 16, 16),
                    Microsoft.Xna.Framework.Color.White,
                    0.0f,
                    new Vector2(16 / 2, 16 / 2),
                    SpriteEffects.None,
                    0
                );
                break;
        }
    }

    public bool IsCollideWith(Ball other)
    {
        return Circle.Intersects(other.Circle) > 0;
    }
}
