using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.Easer;

namespace PuzzleBobble;

public class DeathLine : GameObject
{
    private AnimatedTexture2D? _spriteSheet;

    private bool _show = false;
    private readonly FloatEaser _yPosEaser = new(TimeSpan.FromSeconds(-1));
    private readonly FloatEaser _alphaEaser = new(TimeSpan.FromSeconds(-1));

    private int originY;

    public DeathLine(int deathYPos) : base("deathline")
    {
        _yPosEaser.SetValueA(deathYPos + 20);
        _yPosEaser.SetEaseFunction(EasingFunctions.ExpoOut);
        _yPosEaser.SetTimeLength(TimeSpan.FromSeconds(0.6), TimeSpan.Zero);

        _yPosEaser.SetEaseBToAFunction(EasingFunctions.PowerIn(3));
        _yPosEaser.SetTimeLengthBToA(TimeSpan.FromSeconds(0.75), TimeSpan.Zero);
        _yPosEaser.SetValueB(deathYPos);


        _alphaEaser.SetValueA(0.0f);
        _alphaEaser.SetEaseFunction(EasingFunctions.ExpoOut);
        _alphaEaser.SetTimeLength(TimeSpan.FromSeconds(0.6), TimeSpan.Zero);

        _alphaEaser.SetEaseBToAFunction(EasingFunctions.PowerInOut(3));
        _alphaEaser.SetTimeLengthBToA(TimeSpan.FromSeconds(0.75), TimeSpan.Zero);
        _alphaEaser.SetValueB(1.0f);
    }

    public override void LoadContent(ContentManager content)
    {
        var tex = content.Load<Texture2D>("Graphics/deathline");
        _spriteSheet = new AnimatedTexture2D(
            tex,
            1, 12, 0.025f, true);
        originY = tex.Height / 12;
        base.LoadContent(content);
    }

    public void Show(GameTime gameTime)
    {
        if (!_show)
        {
            _show = true;
            _yPosEaser.StartEase(gameTime.TotalGameTime, true);
            _alphaEaser.StartEase(gameTime.TotalGameTime, true);
        }
    }

    public void Hide(GameTime gameTime)
    {
        if (_show)
        {
            _show = false;
            _yPosEaser.StartEase(gameTime.TotalGameTime, false);
            _alphaEaser.StartEase(gameTime.TotalGameTime, false);
        }
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        if (!IsActive) return;

        Position = new Vector2(Position.X, _yPosEaser.GetValue(gameTime.TotalGameTime));
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _spriteSheet?.Draw(
            spriteBatch,
            gameTime,
            ScreenPosition - new Vector2(0, BallData.BALL_SIZE),
            Color.White * _alphaEaser.GetValue(gameTime.TotalGameTime),
            0.0f,
            new Vector2(_spriteSheet.frameWidth / 2, originY),
            PixelScale.X
        );
    }
}