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
    protected Rectangle sourceRectangle;
    private bool isLooping;
    private bool isPlaying;
    public bool IsFinished { get; private set; }
    private float timeSinceStart;

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
        sourceRectangle = new Rectangle(spriteSheetClip.X, spriteSheetClip.Y, frameWidth, frameHeight);
    }

    public AnimatedTexture2D(AnimatedTexture2D template)
        : this(template.spriteSheet, template.spriteSheetClip, template.hFrames, template.vFrames, template.frameDuration, template.isLooping)
    {
    }

    public AnimatedTexture2D(Texture2D spriteSheet, int hFrames, int vFrames, float frameDuration, bool isLooping = false) :
        this(spriteSheet, new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height), hFrames, vFrames, frameDuration, isLooping)
    {
    }

    public void Play(float delay = 0.0f)
    {
        if (isPlaying) return;

        isPlaying = true;
        sourceRectangle.X = spriteSheetClip.X;
        sourceRectangle.Y = spriteSheetClip.Y;
        timeSinceStart = -delay;
    }

    public void Stop()
    {
        isPlaying = false;
        timeSinceStart = 0;
    }

    public void SetLooping(bool isLooping)
    {
        this.isLooping = isLooping;
    }

    public void Update(GameTime gameTime)
    {
        if (!isPlaying) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        timeSinceStart += deltaTime;
        if (timeSinceStart > frameDuration)
        {
            timeSinceStart = 0;
            sourceRectangle.X += frameWidth;
            if (sourceRectangle.X >= spriteSheetClip.X + spriteSheetClip.Width)
            {
                sourceRectangle.X = spriteSheetClip.X;
                sourceRectangle.Y += frameHeight;
                if (sourceRectangle.Y >= spriteSheetClip.Y + spriteSheetClip.Height)
                {
                    sourceRectangle.Y = spriteSheetClip.Y;
                    if (!isLooping)
                    {
                        Stop();
                        IsFinished = true;
                    }
                }
            }

        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, Vector2 origin, float scale)
    {
        if (!isPlaying) return;

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

    public void Draw(SpriteBatch spriteBatch, Rectangle destinationRectangle, Color color, float rotation, Vector2 origin)
    {
        if (!isPlaying) return;

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
