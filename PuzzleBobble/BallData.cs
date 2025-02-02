using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;
public readonly struct BallData
{
    public static readonly int BALL_SIZE = 16;
    public static readonly int EXPLOSION_SIZE = 32;
    public static readonly int BOMB_EXPLOSION_WIDTH = 80;
    public static readonly int BOMB_EXPLOSION_HEIGHT = 72;
    public static int BALL_DRAW_SIZE => BALL_SIZE * GameObject.PIXEL_SIZE;
    public static int EXPLOSION_DRAW_SIZE => EXPLOSION_SIZE * GameObject.PIXEL_SIZE;
    public static int BOMB_EXPLOSION_DRAW_WIDTH => BOMB_EXPLOSION_WIDTH * GameObject.PIXEL_SIZE;
    public static int BOMB_EXPLOSION_DRAW_HEIGHT => BOMB_EXPLOSION_HEIGHT * GameObject.PIXEL_SIZE;

    public const float MAX_EXPLODE_DELAY = 0.2f;
    public const float MAX_PETRIFY_DELAY = 1f;


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

    public bool IsColor => 0 <= value && value < COLOR_COUNT;
    public bool IsSpecial => value < 0;
    public bool IsRainbow => value == (int)SpecialType.Rainbow;
    public bool IsBomb => value == (int)SpecialType.Bomb;
    public bool IsStone => value == (int)SpecialType.Stone;

    public BallData(int value)
    {
        this.value = value;
    }

    public BallData(BallData other)
    {
        value = other.value;
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

    public static string ToCode(BallData ball)
    {
        if (ball.IsColor)
        {
            return ((char)('a' + ball.value)).ToString();
        }
        return ball.value switch
        {
            (int)SpecialType.Rainbow => "R",
            (int)SpecialType.Bomb => "B",
            (int)SpecialType.Stone => "S",
            _ => throw new ArgumentException($"Invalid special ball type: {ball.value}")
        };
    }

    public string ToSQLValue()
    {
        if (IsColor)
        {
            return value.ToString();
        }
        return value switch
        {
            (int)SpecialType.Rainbow => "rainbow",
            (int)SpecialType.Bomb => "bomb",
            (int)SpecialType.Stone => "stone",
            _ => throw new ArgumentException($"Invalid special ball type: {value}")
        };
    }

    public static int RandomColor(Random rand)
    {
        return rand.Next(COLOR_COUNT);
    }

    public static int RandomUsefulSpecial(Random rand)
    {
        return rand.Next((int)SpecialType.Bomb, (int)SpecialType.Rainbow + 1);
    }

    public class Assets
    {
        public readonly Texture2D BallSpritesheet;
        public readonly Texture2D SpecialBallSpritesheet;
        public readonly Texture2D ExplosionSpritesheet;
        public readonly Texture2D BombExplosionSpritesheet;
        public readonly Texture2D StoneExplosionSpritesheet;
        public readonly Texture2D ShineSpritesheet;
        public readonly Texture2D PreviewBallSpriteSheet;

        public Assets(ContentManager content)
        {
            BallSpritesheet = content.Load<Texture2D>("Graphics/balls");
            SpecialBallSpritesheet = content.Load<Texture2D>("Graphics/special_balls");
            ExplosionSpritesheet = content.Load<Texture2D>("Graphics/balls_explode4");
            BombExplosionSpritesheet = content.Load<Texture2D>("Graphics/bomb_explode");
            StoneExplosionSpritesheet = content.Load<Texture2D>("Graphics/rock_explode");
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
        public AnimatedTexture2D? explosionAnim;
        public AnimatedTexture2D? petrifyAnim;
        public float explodeDelay = 0.0f;
        public float petrifyDelay = 0.0f;
        public bool isExploding = false;
        public bool isPetrifying = false;
        public bool isAlt = false;
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
        animState.petrifyAnim =
            new AnimatedTexture2D(
                assets.SpecialBallSpritesheet,
                new Rectangle(BALL_SIZE * 2, BALL_SIZE, BALL_SIZE * 6, BALL_SIZE),
                6, 1, 0.1f, false
        )
            {
                KeepDrawingAfterFinish = true
            };
        switch (value)
        {
            case (int)SpecialType.Rainbow:
                {
                    var anim = new AnimatedTexture2D(assets.SpecialBallSpritesheet, new Rectangle(0, BALL_SIZE * 2, BALL_SIZE * 8, BALL_SIZE), 8, 1, 0.15f, true);
                    anim.Delay(_rand.NextSingle() * 0.15f * 10);
                    animState.anim = anim;

                    animState.explosionAnim = new AnimatedTexture2D(
                        assets.ExplosionSpritesheet,
                        new Rectangle(
                            0,
                            11 * (assets.ExplosionSpritesheet.Height / 12),
                            assets.ExplosionSpritesheet.Width,
                            assets.ExplosionSpritesheet.Height / 12
                        ),
                        7, 1, 0.02f, false
                    );
                    break;
                }
            case (int)SpecialType.Bomb:
                {
                    var anim = new AnimatedTexture2D(assets.SpecialBallSpritesheet, new Rectangle(BALL_SIZE, 0, BALL_SIZE * 4, BALL_SIZE), 4, 1, 0.15f, true);
                    anim.Delay(_rand.NextSingle() * 0.1f * 4);
                    animState.anim = anim;

                    animState.explosionAnim = new AnimatedTexture2D(
                        assets.BombExplosionSpritesheet,
                        new Rectangle(
                            0,
                            0,
                            assets.BombExplosionSpritesheet.Width,
                            assets.BombExplosionSpritesheet.Height
                        ),
                        6, 1, 0.075f, false
                    );
                    break;
                }
            case (int)SpecialType.Stone:
                animState.explosionAnim = new AnimatedTexture2D(
                    assets.StoneExplosionSpritesheet,
                    new Rectangle(
                        0,
                        0,
                        assets.StoneExplosionSpritesheet.Width,
                        assets.StoneExplosionSpritesheet.Height
                    ),
                    7, 1, 0.02f, false
                );
                break;
            default:
                {// Color balls
                    animState.anim = null;

                    animState.explosionAnim = new AnimatedTexture2D(
                        assets.ExplosionSpritesheet,
                        new Rectangle(
                            0,
                            value * (assets.ExplosionSpritesheet.Height / 12),
                            assets.ExplosionSpritesheet.Width,
                            assets.ExplosionSpritesheet.Height / 12
                        ),
                        7, 1, 0.02f, false
                    );
                    break;
                }
        }

        animState.explosionAnim.KeepDrawingAfterFinish = false;
        if (IsBomb) { animState.explodeDelay = 0; }
        else { animState.explodeDelay = _rand.NextSingle() * MAX_EXPLODE_DELAY; }
        animState.petrifyDelay = _rand.NextSingle() * MAX_PETRIFY_DELAY;
    }

    public static bool operator ==(BallData a, BallData b) => a.value == b.value;
    public static bool operator !=(BallData a, BallData b) => !(a == b);
    public override readonly bool Equals(object? obj) => obj is BallData data && this == data;
    public override readonly int GetHashCode() => value.GetHashCode();

    public void PlayShineAnimation(GameTime gameTime)
    {
        if (value == (int)SpecialType.Stone)
        {
            return;
        }
        Debug.Assert(animState.shineAnim is not null, "Shine animation is not loaded.");
        animState.shineAnim.Play(gameTime);
    }

    public void PlayAltAnimation(GameTime gameTime)
    {
        switch (value)
        {
            case (int)SpecialType.Bomb:
                Debug.Assert(animState.anim is not null, "Bomb ball animation state is not loaded.");
                animState.anim.Play(gameTime);
                animState.isAlt = true;
                break;
            default:
                break;
        }
    }

    public void PlayPetrifyAnimation(GameTime gameTime)
    {
        if (value == (int)SpecialType.Stone)
        {
            return;
        }
        Debug.Assert(animState.petrifyAnim is not null, "Petrify animation is not loaded.");
        animState.petrifyAnim.Play(gameTime, animState.petrifyDelay);
        animState.isPetrifying = true;
    }

    public bool IsPlayingExplosionAnimation => animState.isExploding;
    public bool IsPlayingShineAnimation(GameTime gameTime)
    {
        Debug.Assert(animState.shineAnim is not null, "Shine animation is not loaded.");
        return !animState.shineAnim.IsFinished(gameTime);
    }

    public void PlayExplosionAnimation(GameTime gameTime)
    {
        Debug.Assert(animState.explosionAnim is not null, "Explosion animation is not loaded.");
        animState.isExploding = true;
        animState.explosionAnim.Play(gameTime, animState.explodeDelay);
    }

    public void PlayExplosionAnimation()
    {
        Debug.Assert(animState.explosionAnim is not null, "Explosion animation is not loaded.");
        animState.explosionAnim.TriggerPlayOnNextDraw(animState.explodeDelay);
        animState.isExploding = true;
    }

    public void StopExplosionAnimation()
    {
        Debug.Assert(animState.explosionAnim is not null, "Explosion animation is not loaded.");
        animState.isExploding = false;
    }

    public bool ExplosionFinished(GameTime gameTime)
    {
        Debug.Assert(animState.explosionAnim is not null, "Explosion animation is not loaded.");
        return animState.explosionAnim.IsFinished(gameTime);
    }

    public float ExplosionDelay => animState.explodeDelay;

    public static void DrawPreviewBall(SpriteBatch spriteBatch, GameTime gameTime, AnimatedTexture2D previewBallAnim, Vector2 screenPosition, float alpha = 1.0f)
    {
        previewBallAnim.Draw(
            spriteBatch,
            gameTime,
            new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
            Color.White * alpha,
            0.0f,
            new Vector2(BALL_SIZE / 2, BALL_SIZE / 2)
        );
    }

    /// <summary>
    /// Draw the ball at the given screen position, with the ball centered at the position.
    /// </summary>
    public readonly void Draw(SpriteBatch spriteBatch, GameTime gameTime, Assets assets, Vector2 screenPosition, float alpha = 1.0f)
    {
        if (animState.isExploding)
        {
            DrawExplode(spriteBatch, gameTime, screenPosition, alpha);
            return;
        }

        if (!animState.isAlt)
        {

            DrawMain(spriteBatch, gameTime, assets, screenPosition, alpha);
        }
        else
        {
            DrawAlt(spriteBatch, gameTime, assets, screenPosition, alpha);
        }

        DrawShine(spriteBatch, gameTime, screenPosition, alpha);
        if (animState.isPetrifying)
        {
            DrawPetrify(spriteBatch, gameTime, screenPosition, alpha);
        }
    }

    private void DrawExplode(SpriteBatch spriteBatch, GameTime gameTime, Vector2 screenPosition, float alpha)
    {
        Debug.Assert(animState.explosionAnim is not null, "Explosion animation is not loaded.");
        switch (value)
        {
            case (int)SpecialType.Bomb:
                animState.explosionAnim.Draw(
                    spriteBatch,
                    gameTime,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BOMB_EXPLOSION_DRAW_WIDTH, BOMB_EXPLOSION_DRAW_HEIGHT),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(BOMB_EXPLOSION_WIDTH / 2, BOMB_EXPLOSION_HEIGHT / 2)
                );
                break;

            default:
                animState.explosionAnim.Draw(
                    spriteBatch,
                    gameTime,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, EXPLOSION_DRAW_SIZE, EXPLOSION_DRAW_SIZE),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(EXPLOSION_SIZE / 2, EXPLOSION_SIZE / 2)
                );
                break;
        }
    }


    private void DrawMain(SpriteBatch spriteBatch, GameTime gameTime, Assets assets, Vector2 screenPosition, float alpha)
    {
        switch (value)
        {
            default: // Color balls
                spriteBatch.Draw(
                    assets.BallSpritesheet,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                    new Rectangle(value * BALL_SIZE, 0, BALL_SIZE, BALL_SIZE),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(BALL_SIZE / 2, BALL_SIZE / 2),
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
                        new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                        Color.White * alpha,
                        0.0f,
                        new Vector2(BALL_SIZE / 2, BALL_SIZE / 2)
                    );
                    }
                    break;
                }
            case (int)SpecialType.Bomb:
                spriteBatch.Draw(
                        assets.SpecialBallSpritesheet,
                        new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                        new Rectangle(0, 0, BALL_SIZE, BALL_SIZE),
                        Color.White * alpha,
                        0.0f,
                        new Vector2(BALL_SIZE / 2, BALL_SIZE / 2),
                        SpriteEffects.None,
                        0
                    );
                break;
            case (int)SpecialType.Stone:
                spriteBatch.Draw(
                    assets.SpecialBallSpritesheet,
                    new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                    new Rectangle(BALL_SIZE * 7, BALL_SIZE, BALL_SIZE, BALL_SIZE),
                    Color.White * alpha,
                    0.0f,
                    new Vector2(BALL_SIZE / 2, BALL_SIZE / 2),
                    SpriteEffects.None,
                    0
                );
                break;
        }
    }

    private void DrawAlt(SpriteBatch spriteBatch, GameTime gameTime, Assets assets, Vector2 screenPosition, float alpha)
    {
        switch (value)
        {
            case (int)SpecialType.Bomb:
                {
                    Debug.Assert(animState.anim is not null, "Bomb ball animation state is not loaded.");
                    if (animState.anim is AnimatedTexture2D atex)
                    {
                        atex.Draw(
                        spriteBatch,
                        gameTime,
                        new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                        Color.White * alpha,
                        0.0f,
                        new Vector2(BALL_SIZE / 2, BALL_SIZE / 2)
                    );
                    }
                    break;
                }
            default:
                break;
        }
    }



    private void DrawShine(SpriteBatch spriteBatch, GameTime gameTime, Vector2 screenPosition, float alpha)
    {
        if (animState.shineAnim is AnimatedTexture2D at2d)
        {
            at2d.Draw(
                spriteBatch,
                gameTime,
                new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                Color.White * alpha,
                0.0f,
                new Vector2(BALL_SIZE / 2, BALL_SIZE / 2)
            );
        }
    }

    private void DrawPetrify(SpriteBatch spriteBatch, GameTime gameTime, Vector2 screenPosition, float alpha)
    {
        if (animState.petrifyAnim is AnimatedTexture2D at2d)
        {
            at2d.Draw(
                spriteBatch,
                gameTime,
                new Rectangle((int)screenPosition.X, (int)screenPosition.Y, BALL_DRAW_SIZE, BALL_DRAW_SIZE),
                Color.White * alpha,
                0.0f,
                new Vector2(BALL_SIZE / 2, BALL_SIZE / 2)
            );
        }
    }

    public static string HexMapToString(HexMap<BallData> map)
    {
        List<string> lines = [];
        for (int row = map.MinR; row <= map.MaxR; row++)
        {
            List<string> cells = [];
            bool stagger = (row & 1) == 1;
            for (int col = 0; col < (stagger ? 7 : 8); col++)
            {
                var data = map[new OffsetCoord(col, row)];
                if (data is BallData bd)
                {
                    cells.Add(ToCode(bd));
                }
                else
                {
                    cells.Add(".");
                }
            }
            lines.Add((stagger ? " " : "") + string.Join(" ", cells));
        }
        return string.Join("\n", lines);
    }

    public class BallStats
    {
        public readonly Dictionary<int, int> ColorCounts = [];
        public int Count = 0;
        public int ColorCount = 0;

        public void Add(BallData ball)
        {
            if (ball.IsColor)
            {
                if (ColorCounts.TryGetValue(ball.value, out int value))
                {
                    ColorCounts[ball.value] = ++value;
                }
                else
                {
                    ColorCounts[ball.value] = 1;
                }
                ColorCount++;
            }

            Count++;
        }

        public void Add(IEnumerator<BallData> balls)
        {
            while (balls.MoveNext())
            {
                if (balls.Current.IsColor)
                {
                    if (ColorCounts.TryGetValue(balls.Current.value, out int value))
                    {
                        ColorCounts[balls.Current.value] = ++value;
                    }
                    else
                    {
                        ColorCounts[balls.Current.value] = 1;
                    }
                    ColorCount++;
                }

                Count++;
            }
        }

        public BallData? GetNextBall(Random rand)
        {
            var colors = ColorCounts.Keys.Where(k => 0 <= k).ToList();
            if (colors.Count == 0)
            {
                return new BallData((int)SpecialType.Bomb);
            }
            var color = colors[rand.Next(colors.Count)];
            return new BallData(color);
        }

        public bool Check(BallData ball)
        {
            if (!ball.IsColor) return true;
            return ColorCounts.TryGetValue(ball.value, out int value)
                    && 0 < value;
        }
    }
}
