using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace PuzzleBobble;

public class SlingshotBall : GameObject
{
    private static readonly Random _random = new();
    private readonly double _timeDelay = _random.NextDouble();
    public Vector2 TargetPosition;
    public BallData Data;
    private readonly BallData.Assets _ballAssets;
    private TimeSpan _time;
    private bool _fadeIn = true;

    private static readonly TimeSpan FADE_TIME = TimeSpan.FromSeconds(0.5);

    public SlingshotBall(GameTime gameTime, BallData.Assets assets, BallData data, Vector2 position) : base("SlingshotBall")
    {
        Data = data;
        _ballAssets = assets;
        Position = position;
        TargetPosition = position;
        _time = gameTime.TotalGameTime;
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        var target = TargetPosition + new Vector2(0, (float)Math.Sin((gameTime.TotalGameTime.TotalSeconds + _timeDelay) * Math.PI) * 3);
        var posDiff = target - Position;
        Position += posDiff * 8f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!_fadeIn && _time < gameTime.TotalGameTime)
        {
            Destroy();
        }
    }

    public void FadeAway(GameTime gameTime)
    {
        _fadeIn = false;
        _time = gameTime.TotalGameTime + FADE_TIME;
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        double fadeValue;
        if (_fadeIn)
        {
            var rawFadeValue = (gameTime.TotalGameTime - _time) / FADE_TIME;
            fadeValue = Math.Min(rawFadeValue, 1);
        }
        else
        {
            var rawFadeValue = (_time - gameTime.TotalGameTime) / FADE_TIME;
            fadeValue = Math.Max(0, rawFadeValue);
        }

        Data.Draw(spriteBatch, gameTime, _ballAssets, ScreenPosition, (float)fadeValue);

    }
}