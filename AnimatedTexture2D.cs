using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTexture2D
{
    protected Texture2D spriteSheet;
    protected Rectangle spriteSheetClip;
    public readonly int hFrames;
    public readonly int vFrames;
    public readonly float frameDuration;
    public readonly int frameWidth;
    public readonly int frameHeight;
    private bool isLooping;
    private bool triggerOnNextDraw;
    private float triggerOnNextDrawDelay;

    private TimeSpan startTime;

    public AnimatedTexture2D(Texture2D spriteSheet, Rectangle spriteSheetClip, int hFrames, int vFrames, float frameDuration, bool isLooping = false)
    {
        this.spriteSheet = spriteSheet;
        this.spriteSheetClip = spriteSheetClip;

        this.hFrames = hFrames;
        this.vFrames = vFrames;
        this.frameDuration = frameDuration;
        this.isLooping = isLooping;
        frameWidth = spriteSheetClip.Width / hFrames;
        frameHeight = spriteSheetClip.Height / vFrames;
        Debug.Assert(spriteSheetClip.Width % hFrames == 0, "clippped spritesheet width should be divisible by the number of horizontal frames.");
        Debug.Assert(spriteSheetClip.Height % vFrames == 0, "clippped spritesheet height should be divisible by the number of vertical frames.");
    }

    public AnimatedTexture2D(AnimatedTexture2D template)
        : this(template.spriteSheet, template.spriteSheetClip, template.hFrames, template.vFrames, template.frameDuration, template.isLooping)
    {
    }

    public AnimatedTexture2D(Texture2D spriteSheet, int hFrames, int vFrames, float frameDuration, bool isLooping = false) :
        this(spriteSheet, new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height), hFrames, vFrames, frameDuration, isLooping)
    {
    }

    public void Play(GameTime gameTime, float delay = 0.0f)
    {
        startTime = gameTime.TotalGameTime + TimeSpan.FromSeconds(delay);
    }

    public void TriggerPlayOnNextDraw(float delay = 0.0f)
    {
        triggerOnNextDraw = true;
        triggerOnNextDrawDelay = delay;
    }

    public bool IsFinished(GameTime gameTime)
    {
        if (triggerOnNextDraw)
        {
            return false;
        }
        var elapsed = gameTime.TotalGameTime - startTime;
        var frameIndex = (int)(elapsed.TotalSeconds / frameDuration);
        return !isLooping && vFrames <= frameIndex / hFrames;
    }

    public void SetLooping(bool isLooping)
    {
        this.isLooping = isLooping;
    }

    private Rectangle ComputeSourceRectangle(GameTime gameTime)
    {
        if (triggerOnNextDraw)
        {
            Play(gameTime, triggerOnNextDrawDelay);
            triggerOnNextDraw = false;
        }

        var elapsed = Math.Max(0, (gameTime.TotalGameTime - startTime).TotalSeconds);
        var frameIndex = (int)(elapsed / frameDuration);
        var hFrameIndex = frameIndex % hFrames;
        var vFrameIndex = frameIndex / hFrames;
        if (vFrames <= vFrameIndex)
        {
            if (isLooping)
            {
                vFrameIndex %= vFrames;
            }
            else
            {
                vFrameIndex = vFrames - 1;
                hFrameIndex = hFrames - 1;
            }
        }
        return new Rectangle(spriteSheetClip.X + hFrameIndex * frameWidth, spriteSheetClip.Y + vFrameIndex * frameHeight, frameWidth, frameHeight);
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 position, Color color, float rotation, Vector2 origin, float scale)
    {
        var sourceRectangle = ComputeSourceRectangle(gameTime);

        spriteBatch.Draw(
            spriteSheet,
            position,
            sourceRectangle,
            color,
            rotation,
            origin,
            scale,
            SpriteEffects.None,
            0.0f
        );
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Rectangle destinationRectangle, Color color, float rotation, Vector2 origin)
    {
        var sourceRectangle = ComputeSourceRectangle(gameTime);

        spriteBatch.Draw(
            spriteSheet,
            destinationRectangle,
            sourceRectangle,
            color,
            rotation,
            origin,
            SpriteEffects.None,
            0.0f
        );
    }
}
