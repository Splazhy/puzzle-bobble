using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;
public struct BallData
{
    public int color;

    public BallData(int color)
    {
        this.color = color;
    }

    public static bool operator ==(BallData a, BallData b) => a.color == b.color;
    public static bool operator !=(BallData a, BallData b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is BallData data && this == data;
    public override readonly int GetHashCode() => color.GetHashCode();


    /// <summary>
    /// Draw the ball at the given screen position, with the ball centered at the position.
    /// </summary>
    public readonly void Draw(SpriteBatch spriteBatch, Texture2D spritesheet, Vector2 screenPosition)
    {
        spriteBatch.Draw(
            spritesheet,
            new Rectangle((int)screenPosition.X, (int)screenPosition.Y, (int)(16 * 3), (int)(16 * 3)),
            new Rectangle(color * 16, 0, 16, 16),
            Color.White,
            0.0f,
            new Vector2(16 / 2, 16 / 2),
            SpriteEffects.None,
            0
        );
    }

    /// <summary>
    /// Draw the ball at the given screen position, with the ball's top-left corner at the position.
    /// </summary>
    public readonly void DrawPosByCorner(SpriteBatch spriteBatch, Texture2D spritesheet, Vector2 screenPosition)
    // find better way later ?
    {
        spriteBatch.Draw(
            spritesheet,
            new Rectangle((int)screenPosition.X, (int)screenPosition.Y, (int)(16 * 3), (int)(16 * 3)),
            new Rectangle(color * 16, 0, 16, 16),
            Color.White
        );
    }


}