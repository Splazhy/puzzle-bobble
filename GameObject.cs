using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class GameObject
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }

    public Vector2 Velocity { get; set; }

    public readonly string Name;

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

    public virtual void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
    }

    public void Destroy()
    {
        Destroyed = true;
    }
}
