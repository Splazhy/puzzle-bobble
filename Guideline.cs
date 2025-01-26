using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;


public class Guideline : GameObject
{
    private readonly Slingshot _slingshot;
    private Texture2D? _texture;
    private readonly float _length;
    private readonly float _duration; // in seconds
    private readonly int _count;
    private Vector2 _origin;

    public Guideline(Slingshot slingshot, int drawCount, float lineLength, float loopDuration) : base("guideline")
    {
        _slingshot = slingshot;
        _length = lineLength;
        _duration = loopDuration;
        _count = drawCount;
        Position = _slingshot.Position;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);

        _texture = content.Load<Texture2D>("Graphics/guideline_full");
        _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);

    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        Rotation = _slingshot.Rotation - MathF.PI / 2;
    }


    // 192 -> gameboard border
    // 24 -> ball radius
    private float LeftBorder { get { return -192 + 24; } }
    private float RightBorder { get { return +192 - 24; } }
    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null, "Guideline texture is not loaded");

        var _progress = (float)(gameTime.TotalGameTime.TotalSeconds % _duration / _duration);
        var direction = Vector2.Normalize(new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation)));
        for (int i = 0; i < _count; i++)
        {
            var subProgress = (_progress + (float)i / _count) % 1.0f;
            var actualScale = new Vector2(3, 3) * (1.0f - subProgress);
            var lengthLeft = _length * subProgress;
            var tmpDirection = direction;
            var s = new Vector2(0, 0);
            Vector2 subPosition;
            while (true)
            {
                var e = s + (tmpDirection * lengthLeft);
                var slope = (e.Y - s.Y) / (e.X - s.X);
                Vector2? bouncePoint = null;

                if (e.X > s.X && e.X > RightBorder)
                {
                    bouncePoint = new Vector2(RightBorder, slope * (RightBorder - s.X) + s.Y);
                }
                else if (e.X < s.X && e.X < LeftBorder)
                {
                    bouncePoint = new Vector2(LeftBorder, slope * (LeftBorder - s.X) + s.Y);
                }

                lengthLeft -= Vector2.Distance(s, bouncePoint ?? e);
                s = bouncePoint ?? e;
                tmpDirection = new Vector2(-tmpDirection.X, tmpDirection.Y);
                if (bouncePoint is null || lengthLeft <= 0)
                {
                    subPosition = e;
                    break;
                }
            }
            spriteBatch.Draw(
                _texture,
                subPosition + ScreenPosition,
                null,
                Color.White * (0.25f * (1.0f - subProgress)),
                0,
                _origin,
                new Vector2(3, 3),
                SpriteEffects.None,
                0
            );
        }
    }
}
