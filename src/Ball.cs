using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace puzzle_bobble;

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
    private Color _color;

    public Ball(Color ballType) : base("ball")
    {
        _color = ballType;
        Position = new Vector2(200.0f, 200.0f);
        Velocity = new Vector2(-20, -20);
    }

    public override void LoadContent(ContentManager content)
    {
        // HACK: This is a temporary solution to load the correct texture
        // we might need to load all textures at once and store them in a dictionary.
        _texture = content.Load<Texture2D>($"Graphics/Ball/{_color}");
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: Implement ball movement
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw(_texture, Position, null, Microsoft.Xna.Framework.Color.White);
    }
}
