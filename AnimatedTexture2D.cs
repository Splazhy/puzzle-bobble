using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTexture2D
{
    protected Texture2D spriteSheet;
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

    public AnimatedTexture2D(Texture2D spriteSheet, int hFrames, int vFrames, float frameDuration, bool isLooping = false)
    {
        this.spriteSheet = spriteSheet;
        this.hFrames = hFrames;
        this.vFrames = vFrames;
        this.frameDuration = frameDuration;
        this.isLooping = isLooping;
        frameWidth = spriteSheet.Width / hFrames;
        frameHeight = spriteSheet.Height / vFrames;
        sourceRectangle = new Rectangle(0, 0, frameWidth, frameHeight);
    }

    public AnimatedTexture2D(AnimatedTexture2D template)
        : this(template.spriteSheet, template.hFrames, template.vFrames, template.frameDuration, template.isLooping)
    {
    }

    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        sourceRectangle.X = 0;
        timeSinceStart = 0;
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

    public void SetVFrame(int index)
    {
        Debug.Assert(index >= 0 && index < vFrames, "vframe index out of range");
        sourceRectangle.Y = index * frameHeight;
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
            if (sourceRectangle.X >= spriteSheet.Width)
            {
                sourceRectangle.X = 0;
                if (!isLooping)
                {
                    Stop();
                    IsFinished = true;
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 origin, float scale, Color color)
    {
        if (!isPlaying) return;

        spriteBatch.Draw(spriteSheet, position, sourceRectangle, color, rotation, origin, scale, SpriteEffects.None, 0);
    }
}
