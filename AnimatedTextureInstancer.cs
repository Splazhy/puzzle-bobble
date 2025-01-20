using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTextureInstancer
{
    public struct AnimationInstance
    {
        public AnimatedTexture2D animatedTexture;
        public Vector2 position;
        public float rotation;
        public Vector2 origin;
        public float scale;
        public Color color;
    }

    private readonly AnimatedTexture2D animatedTexture;
    private readonly List<AnimationInstance> instances = [];

    public AnimatedTextureInstancer(AnimatedTexture2D animatedTexture)
    {
        this.animatedTexture = animatedTexture;
    }

    public void PlayAt(Vector2 position, float rotation, Vector2 origin, float scale, Color color)
    {
        var newAnimatedTexture = new AnimatedTexture2D(animatedTexture);
        instances.Add(new AnimationInstance
        {
            animatedTexture = newAnimatedTexture,
            position = position,
            rotation = rotation,
            origin = origin,
            scale = scale,
            color = color,
        });
        newAnimatedTexture.Play();
    }

    public void Update(GameTime gameTime)
    {
        var instancesToRemove = instances.FindAll(instance => instance.animatedTexture.IsFinished);
        instances.RemoveAll(instance => instancesToRemove.Contains(instance));

        foreach (var instance in instances)
        {
            instance.animatedTexture.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var instance in instances)
        {
            instance.animatedTexture.Draw(
                spriteBatch,
                instance.position,
                instance.rotation,
                instance.origin,
                instance.scale,
                instance.color
            );
        }
    }
}