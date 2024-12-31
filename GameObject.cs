using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace puzzle_bobble;

public class GameObject
{
    protected Texture2D _texture { get; set; }

    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    public Vector2 Velocity { get; set; }

    public readonly string Name;

    public Rectangle Rectangle
    {
        get
        {
            return new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                (int)(_texture.Width * Scale.X),
                (int)(_texture.Height * Scale.Y)
            );
        }
    }

    // Is this object being updated in main game loop?
    public bool IsActive { get; set; }

    // Is this object being drawn in main game loop?
    public bool IsVisible { get; set; }

    public bool Destroyed { get; private set; }

    // We treat GameObject contructor like Initialize method
    public GameObject(string name)
    {
        Position = Vector2.Zero;
        Rotation = 0.0f;
        Scale = Vector2.One;
        Velocity = Vector2.Zero;
        Name = name;
        IsActive = true;
        IsVisible = true;
        Destroyed = false;
    }

    public virtual void LoadContent(ContentManager content)
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
    }

    public void Destroy()
    {
        Destroyed = true;
    }
}
