using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;
public readonly struct BallData
{
    public readonly int value;
    public readonly bool IsColor => 0 <= value;
    public readonly bool IsRainbow => value == -1;

    public BallData(int value)
    {
        this.value = value;
    }

    public class Assets
    {
        public readonly Texture2D BallSpritesheet;
        public readonly Texture2D ExplosionSpritesheet;

        public Assets(ContentManager content)
        {
            BallSpritesheet = LoadBallSpritesheet(content);
            ExplosionSpritesheet = LoadExplosionSpritesheet(content);
        }
    }

    public readonly struct AnimState
    {
        public readonly AnimatedTexture2D anim;
        public AnimState(AnimatedTexture2D anim)
        {
            this.anim = anim;
        }
    }

    public AnimState? CreateAnimationState(Assets assets)
    {
        if (IsColor)
        {
            return null;
        }
        if (IsRainbow)
        {
            return new AnimState(new AnimatedTexture2D(assets.BallSpritesheet, new Rectangle(0, 0, 16 * 10, 16), 10, 1, 0.15f, true));
        }
        return null;
    }

    public static bool operator ==(BallData a, BallData b) => a.value == b.value;
    public static bool operator !=(BallData a, BallData b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is BallData data && this == data;
    public override readonly int GetHashCode() => value.GetHashCode();


    /// <summary>
    /// Draw the ball at the given screen position, with the ball centered at the position.
    /// </summary>
    public readonly void Draw(SpriteBatch spriteBatch, GameTime gameTime, Assets assets, AnimState? animState, Vector2 screenPosition)
    {
        if (IsColor)
        {
            spriteBatch.Draw(
                assets.BallSpritesheet,
                new Rectangle((int)screenPosition.X, (int)screenPosition.Y, (int)(16 * 3), (int)(16 * 3)),
                new Rectangle(value * 16, 0, 16, 16),
                Color.White,
                0.0f,
                new Vector2(16 / 2, 16 / 2),
                SpriteEffects.None,
                0
            );
            return;
        }
        if (IsRainbow)
        {
            Debug.Assert(animState is not null, "Rainbow ball animation state is not loaded.");
            if (animState is AnimState animState2)
            {
                animState2.anim.Draw(
                spriteBatch,
                gameTime,
                new Rectangle((int)screenPosition.X, (int)screenPosition.Y, (int)(16 * 3), (int)(16 * 3)),
                Color.White,
                0.0f,
                new Vector2(16 / 2, 16 / 2)
            );
            }
            return;
        }
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
        return new AnimatedTexture2D(explosionSheet, new Rectangle(0, value * (explosionSheet.Height / 12), explosionSheet.Width, explosionSheet.Height / 12), 7, 1, 0.02f, false);
    }

    public class BallStats
    {
        public readonly Dictionary<int, int> ColorCounts = [];

        public void Add(IEnumerator<BallData> balls)
        {
            while (balls.MoveNext())
            {
                if (ColorCounts.TryGetValue(balls.Current.value, out int value))
                {
                    ColorCounts[balls.Current.value] = ++value;
                }
                else
                {
                    ColorCounts[balls.Current.value] = 1;
                }
            }
        }
    }
}