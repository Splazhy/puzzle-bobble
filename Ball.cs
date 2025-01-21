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
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _spriteSheet = BallData.LoadBallSpritesheet(content);
        _explosionSpriteSheet = new AnimatedTexture2D(content.Load<Texture2D>("Graphics/balls_explode"), 7, 12, 0.02f, false);
        _explosionSpriteSheet.SetVFrame(Data.color);
        _explosionSpriteSheet.Play();
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
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
                Position += Velocity * deltaTime;
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

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        var scrPos = parentTranslate + Position;
        switch (_state)
        {
            case State.Exploding:
                if (_explosionSpriteSheet is null) break;
                _explosionSpriteSheet.Draw(
                    spriteBatch,
                    scrPos + new Vector2(-48, -48),
                    0.0f,
                    Vector2.Zero,
                    Scale.X, // we assume that scale is uniform
                    Microsoft.Xna.Framework.Color.White
                );
                break;
            default:
                if (_spriteSheet is null) break;
                Data.Draw(spriteBatch, _spriteSheet, scrPos);
                break;
        }
    }

    public bool IsCollideWith(Ball other)
    {
        return Circle.Intersects(other.Circle) > 0;
    }
}
