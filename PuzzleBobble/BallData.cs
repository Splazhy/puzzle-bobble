using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;
public readonly struct BallData
{
    public static readonly int BALL_TEXTURE_SIZE = 16;
    public static readonly int EXPLOSION_TEXTURE_SIZE = 32;
    public static readonly int BALL_SIZE = BALL_TEXTURE_SIZE * GameObject.PIXEL_SIZE;

    private static readonly Random _rand = new();
    public readonly int value;

    private readonly AnimState animState = new();

    public static readonly int COLOR_COUNT = 12;

    public enum SpecialType
    {
        Rainbow = -1,
        Bomb = -2,
        Stone = -3
    }

    public BallData(int value)
    {
        this.value = value;
    }

    public static BallData FromCode(string code)
    {
        string firstChar = code.Substring(0, 1);
        switch (firstChar)
        {
            default:
                var color = firstChar[0] - 'a';
                if (!(0 <= color && color < COLOR_COUNT))
                {
                    throw new ArgumentException($"Invalid color code: {firstChar}");
                }
                return new BallData(color);
            case "R":
                return new BallData((int)SpecialType.Rainbow);
            case "B":
                return new BallData((int)SpecialType.Bomb);
            case "S":
                return new BallData((int)SpecialType.Stone);
        }
    }

    public class Assets
    {
        public readonly Texture2D BallSpritesheet;
        public readonly Texture2D ExplosionSpritesheet;
        public readonly Texture2D ShineSpritesheet;
        public readonly Texture2D PreviewBallSpriteSheet;

        public Assets(ContentManager content)
        {
            BallSpritesheet = content.Load<Texture2D>("Graphics/balls");
            ExplosionSpritesheet = content.Load<Texture2D>("Graphics/balls_explode3");
            ShineSpritesheet = content.Load<Texture2D>("Graphics/ball_shine");
            PreviewBallSpriteSheet = content.Load<Texture2D>("Graphics/ball_preview");
        }

        public AnimatedTexture2D CreatePreviewBallAnimation()
        {
            var at2d = new AnimatedTexture2D(
                PreviewBallSpriteSheet,
                4, 1, 0.1f, true);
            at2d.TriggerPlayOnNextDraw();
            return at2d;
        }
    }

    private class AnimState
    {
        public AnimatedTexture2D? anim;
        public AnimatedTexture2D? shineAnim;
    }

    public void LoadAnimation(Assets assets)
    {
        animState.shineAnim =
            new AnimatedTexture2D(
                assets.ShineSpritesheet,
                9, 1, 0.01f, false
        )
            {
                KeepDrawingAfterFinish = false
            };
        switch (value)
        {
            case (int)SpecialType.Rainbow:
                {
                    var anim = new AnimatedTexture2D(assets.BallSpritesheet, new Rectangle(0, 0, BALL_TEXTURE_SIZE * 10, BALL_TEXTURE_SIZE), 10, 1, 0.15f, true);
                    anim.Delay(_rand.NextSingle() * 0.15f * 10);
                    animState.anim = anim;
                    return;
                }
            case (int)SpecialType.Bomb:
                {
                    var anim = new AnimatedTexture2D(assets.BallSpritesheet, new Rectangle(0, BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE * 4, BALL_TEXTURE_SIZE), 4, 1, 0.1f, true);
                    anim.Delay(_rand.NextSingle() * 0.1f * 4);
                    animState.anim = anim;
                    return;
                }
            case (int)SpecialType.Stone:
                return;
            default: // Color balls
                animState.anim = null;
                return;
        }
    }

    public static bool operator ==(BallData a, BallData b) => a.value == b.value;
    public static bool operator !=(BallData a, BallData b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is BallData data && this == data;
    public override readonly int GetHashCode() => value.GetHashCode();

    public void PlayShineAnimation(GameTime gameTime)
    {
        Debug.Assert(animState.shineAnim is not null, "Shine animation is not loaded.");
        animState.shineAnim.Play(gameTime);
    }

    public static void DrawPreviewBall(SpriteBatch spriteBatch, GameTime gameTime, AnimatedTexture2D previewBallAnim, Vector2 screenPosition, float alpha = 1.0f)
    {
        previewBallAnim.Draw(
            spriteBatch,
            gameTime,
            new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
            Color.White * alpha,
            0.0f,
            new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2)
        );
    }

    /// <summary>
    /// Draw the ball at the given screen position, with the ball centered at the position.
    /// </summary>
    public readonly void Draw(SpriteBatch spriteBatch, GameTime gameTime, Assets assets, Vector2 screenPosition, float alpha = 1.0f)
    {
        switch (value)
        {
            default: // Color balls
                spriteBatch.Draw(
                    assets.BallSpritesheet,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
                    new Rectangle(value * BALL_TEXTURE_SIZE, 0, BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2),
                    SpriteEffects.None,
                    0
                );
                break;
            case (int)SpecialType.Rainbow:
                {
                    Debug.Assert(animState.anim is not null, "Rainbow ball animation state is not loaded.");
                    if (animState.anim is AnimatedTexture2D atex)
                    {
                        atex.Draw(
                        spriteBatch,
                        gameTime,
                        new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
                        Color.White * alpha,
                        0.0f,
                        new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2)
                    );
                    }
                    break;
                }
            case (int)SpecialType.Bomb:
                {
                    Debug.Assert(animState.anim is not null, "Bomb ball animation state is not loaded.");
                    if (animState.anim is AnimatedTexture2D atex)
                    {
                        atex.Draw(
                        spriteBatch,
                        gameTime,
                        new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
                        Color.White * alpha,
                        0.0f,
                        new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2)
                    );
                    }
                    break;
                }
            case (int)SpecialType.Stone:
                spriteBatch.Draw(
                    assets.BallSpritesheet,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
                    new Rectangle(4 * BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE, BALL_TEXTURE_SIZE),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2),
                    SpriteEffects.None,
                    0
                );
                break;
        }

        if (animState.shineAnim is AnimatedTexture2D at2d)
        {
            at2d.Draw(
                spriteBatch,
                gameTime,
                new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_SIZE, BALL_SIZE),
                Color.White * alpha,
                0.0f,
                new Vector2(BALL_TEXTURE_SIZE / 2, BALL_TEXTURE_SIZE / 2)
            );
        }
    }


    public readonly AnimatedTexture2D CreateExplosionAnimation(Assets assets)
    {
        var explosionSheet = assets.ExplosionSpritesheet;
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
