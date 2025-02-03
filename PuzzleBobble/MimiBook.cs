using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
namespace PuzzleBobble;

public class MimiBook : GameObject
{

    private Texture2D? _texture;
    private readonly Random _rand = new();
    private int _frame = 0;
    private TimeSpan _timeAccumulator = TimeSpan.Zero;
    private int _frameWidth;
    private TimeSpan _nextTimePoint = TimeSpan.Zero;
    public MimiBook() : base("MimiBook")
    {
    }

    public override void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("Graphics/mimi_book");
        _frameWidth = _texture.Width / 11;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        _timeAccumulator += gameTime.ElapsedGameTime;
        if (_timeAccumulator < TimeSpan.FromSeconds(0.08)) return;
        _timeAccumulator -= TimeSpan.FromSeconds(0.08);

        switch (_frame)
        {
            case 0:
                {
                    if (gameTime.TotalGameTime < _nextTimePoint) { break; }
                    else
                    {
                        var v2 = _rand.NextSingle();
                        if (v2 < 0.5f)
                        {
                            _frame = 1;
                            _nextTimePoint = gameTime.TotalGameTime + TimeSpan.FromSeconds(_rand.NextSingle() * 1);
                        }
                        else { _frame = 6; }
                    }
                    break;
                }
            case 3:
                _frame++;
                break;
            case 4:
                if (gameTime.TotalGameTime < _nextTimePoint) { _frame = 3; }
                else { _frame = 5; }
                break;
            case 5:
            case 10:
                _frame = 0;
                _nextTimePoint = gameTime.TotalGameTime + TimeSpan.FromSeconds(_rand.NextSingle() * 5 + 2);
                break;
            default:
                _frame++;
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null);

        var wave = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * Math.PI / 2.7) * 5;

        spriteBatch.Draw(
            _texture,
            ScreenPositionO(new Vector2(0, wave)),
            new Rectangle(_frameWidth * _frame, 0, _frameWidth, _texture.Height),
            Color.White,
            0f,
            new Vector2(_frameWidth / 2, _texture.Height / 2),
            PIXEL_SIZE,
            SpriteEffects.None,
            0f
        );
    }
}