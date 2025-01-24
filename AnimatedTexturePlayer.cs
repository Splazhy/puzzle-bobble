using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class AnimatedTexturePlayer
{
    public class AnimationInstance : GameObject
    {
        private readonly AnimatedTexture2D _animatedTexture;
        public Vector2 Size;
        public Vector2 Origin;
        public Color Color;

        public AnimationInstance(AnimatedTexture2D animatedTexture) : base("animationInstance")
        {
            _animatedTexture = animatedTexture;
        }

        public override void Update(GameTime gameTime, Vector2 parentTranslate)
        {
            base.Update(gameTime, parentTranslate);
            if (_animatedTexture.IsFinished)
            {
                Destroy();
            }
            _animatedTexture.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _animatedTexture.Draw(
                spriteBatch,
                new Rectangle(
                    (int)(Position.X + ParentTranslate.X),
                    (int)(Position.Y + ParentTranslate.Y),
                    (int)Size.X,
                    (int)Size.Y
                ),
                Color,
                Rotation,
                Origin
            );
        }
    }

    private readonly AnimatedTexture2D animatedTexture;

    public AnimatedTexturePlayer(AnimatedTexture2D animatedTexture)
    {
        this.animatedTexture = animatedTexture;
    }

    public AnimationInstance PlayAt(Vector2 position, Vector2 size, Color color, float rotation, Vector2 origin)
    {
        var newAnimatedTexture = new AnimatedTexture2D(animatedTexture);
        newAnimatedTexture.Play();
        return new AnimationInstance(newAnimatedTexture)
        {
            Position = position,
            Size = size,
            Color = color,
            Rotation = rotation,
            Origin = origin,
        };
    }
}
