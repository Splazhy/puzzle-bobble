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
    private float HalfBoardWidth { get { return +192 - 24; } }
    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null, "Guideline texture is not loaded");

        var _progress = (float)(gameTime.TotalGameTime.TotalSeconds % _duration / _duration);
        var direction = Vector2.Normalize(new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation)));
        for (int i = 0; i < _count; i++)
        {
            var subProgress = (_progress + (float)i / _count) % 1.0f;
            var actualScale = new Vector2(3, 3) * (1.0f - subProgress);
            var lengthFromSlingshot = _length * subProgress;
            var vecToEndPos = direction * lengthFromSlingshot;

            // exploiting modulo and absolute to do reflections
            // general idea is to have the line "loop" over 2 board lengths, (modulo)
            // then "fold" half of the board into itself (abs)
            // most code is just moving the 0 point around
            // so modulo and abs can do its thing
            // see https://youtu.be/YZBg4M-MO8A

            // shift coordinates so
            // left edge of "negative" board is 0,
            // and right edge of "positive" board is 2 * Width
            var x1 = vecToEndPos.X + 3 * HalfBoardWidth;

            // loop coordinates so everything falls between 0 and 2 * Width
            // compute modulo using remainder operator, handling case where x1 is negative
            var x2 = ((x1 % (4 * HalfBoardWidth)) + 4 * HalfBoardWidth) % (4 * HalfBoardWidth);

            // shift coordinates so
            // left edge of "negative" board is -Width,
            // center edge is 0,
            // and right edge of "positive" board is Width
            var x3 = x2 - 2 * HalfBoardWidth;

            // reflect coordinates
            // from the "negative" board onto the "positive" board
            var x4 = Math.Abs(x3);

            // shift coordinates so
            // left and right edge of board is on ±0.5 * Width,
            // and 0 in the center of the board
            var x5 = x4 - HalfBoardWidth;

            Vector2 calculatedPos = new(x5, vecToEndPos.Y);
            spriteBatch.Draw(
                _texture,
                calculatedPos + ScreenPosition,
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
