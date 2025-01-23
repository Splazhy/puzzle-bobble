using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTexturePlayer : GameObject
{
    public struct AnimationInstance
    {
        public AnimatedTexture2D animatedTexture;
        public Rectangle destinationRectangle;
        public float rotation;
        public Vector2 origin;
        public Color color;
    }

    private readonly AnimatedTexture2D animatedTexture;
    private readonly List<AnimationInstance> instances = [];

    public AnimatedTexturePlayer(AnimatedTexture2D animatedTexture) : base("animatedTexturePlayer")
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

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        var instancesToRemove = instances.FindAll(instance => instance.animatedTexture.IsFinished);
        instances.RemoveAll(instance => instancesToRemove.Contains(instance));

        foreach (var instance in instances)
        {
            instance.animatedTexture.Update(gameTime);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        foreach (var instance in instances)
        {
            instance.animatedTexture.Draw(
                spriteBatch,
                new Rectangle(
                    instance.destinationRectangle.X + (int)ParentTranslate.X,
                    instance.destinationRectangle.Y + (int)ParentTranslate.Y,
                    instance.destinationRectangle.Width,
                    instance.destinationRectangle.Height
                ),
                instance.color,
                instance.rotation,
                instance.origin
            );
        }
    }

    internal void PlayAt(Rectangle rectangle, int v1, Vector2 vector2, int v2, Color white)
    {
        throw new NotImplementedException();
    }

}
