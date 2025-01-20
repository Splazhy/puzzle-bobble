using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTextureInstancer
{
    public struct AnimationInstance
    {
        public AnimatedTexture2D animatedTexture;
        public Rectangle destinationRectangle;
        public float rotation;
        public Vector2 origin;
        public Color color;
    }

    private AnimatedTexture2D animatedTexture;
    private List<AnimationInstance> instances = [];

    public AnimatedTextureInstancer(AnimatedTexture2D animatedTexture)
    {
        this.animatedTexture = animatedTexture;
    }

    public void PlayAt(Rectangle destinationRectangle, Color color, float rotation, Vector2 origin)
    {
        var newAnimatedTexture = new AnimatedTexture2D(animatedTexture);
        instances.Add(new AnimationInstance
        {
            animatedTexture = newAnimatedTexture,
            destinationRectangle = destinationRectangle,
            color = color,
            rotation = rotation,
            origin = origin,
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
                instance.destinationRectangle,
                instance.color,
                instance.rotation,
                instance.origin
            );
        }
    }
}