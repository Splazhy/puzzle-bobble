using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;
public struct BallData
{
    public static readonly int BALL_TEXTURE_SIZE = 16;
    public static readonly int EXPLOSION_TEXTURE_SIZE = 32;
    public static readonly int BALL_SIZE = BALL_TEXTURE_SIZE * GameObject.PIXEL_SIZE;

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
            new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
            new Rectangle(color * BALL_TEXTURE_SIZE, 0, BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE),
            Color.White,
            0.0f,
            new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2),
            SpriteEffects.None,
            0
        );
    }

    public static Texture2D LoadBallSpritesheet(ContentManager content)
    {
        return content.Load<Texture2D>("Graphics/balls");
    }
    public static Texture2D LoadExplosionSpritesheet(ContentManager content)
    {
        return content.Load<Texture2D>("Graphics/balls_explode2");
    }
    public readonly AnimatedTexture2D CreateExplosionAnimation(ContentManager content)
    {
        var explosionSheet = LoadExplosionSpritesheet(content);
        return new AnimatedTexture2D(explosionSheet, new Rectangle(0, color * (explosionSheet.Height / 12), explosionSheet.Width, explosionSheet.Height / 12), 7, 1, 0.02f, false);
    }

    public class BallStats
    {
        public readonly Dictionary<int, int> ColorCounts = [];

        public void Add(IEnumerator<BallData> balls)
        {
            while (balls.MoveNext())
            {
                if (ColorCounts.TryGetValue(balls.Current.color, out int value))
                {
                    ColorCounts[balls.Current.color] = ++value;
                }
                else
                {
                    ColorCounts[balls.Current.color] = 1;
                }
            }
        }
    }
}