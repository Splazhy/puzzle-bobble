using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class Ball : GameObject
{
    public enum Color
    {
        // Starts at 1 to reserve 0 for empty space
        Brown = 1,
        Green,
        Red,
        Blue,
        Pink,
        // NOTE: Will need to add more colors
    }

    public enum State
    {
        Idle,
        Moving,
        Falling,
    }

    private Texture2D _texture;
    public Circle Circle
    {
        get
        {
            Debug.Assert(Scale.X == Scale.Y, "Non-uniform scaling is not supported for collision detection.");
            Debug.Assert(_texture.Width == _texture.Height, "Non-square textures are not supported for collision detection.");
            return new Circle(Position, _texture.Width / 2 * Scale.X);
        }
    }
    private Color _color;
    public State state;
    private Viewport _viewport;

    public Ball(Color ballType, Viewport viewport) : base("ball")
    {
        _viewport = viewport;
        state = State.Moving;
        _color = ballType;
    }

    public override void LoadContent(ContentManager content)
    {
        // XNA caches textures, so we don't need to worry about loading the same texture multiple times
        _texture = content.Load<Texture2D>($"Graphics/Ball/{_color}");
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (state)
        {
            case State.Idle:
                break;
            case State.Moving:
                // TODO: fix this to take gameboard bounds into account
                if (Position.X - Circle.radius < 0 || Position.X + Circle.radius > _viewport.Width)
                {
                    Velocity = new Vector2(-Velocity.X, Velocity.Y);
                }
                if (Position.Y - Circle.radius < 0 || Position.Y + Circle.radius > _viewport.Height)
                {
                    Velocity = new Vector2(Velocity.X, -Velocity.Y);
                }
                Position += Velocity * deltaTime;
                break;
            case State.Falling:
                Position += new Vector2(0, 1);
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw(
            _texture,
            Position,
            null,
            Microsoft.Xna.Framework.Color.White,
            Rotation,
            new Vector2(_texture.Width / 2, _texture.Height / 2),
            Scale,
            SpriteEffects.None,
            0
        );
    }

    public bool IsCollideWith(Ball other)
    {
        return Circle.Intersects(other.Circle) > 0;
    }
}
