using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.Easer;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;


public class Guideline : GameObject
{
    private readonly GameBoard _gameBoard;
    private readonly Slingshot _slingshot;

    private const int STEP_COUNT = 200;
    private static readonly float STEP_SIZE = BallData.BALL_SIZE / 2;
    private const float MAX_LENGTH = 100f;

    private Texture2D? _texture;
    private Texture2D? _textureHollow;
    private AnimatedTexture2D? _previewBallSpriteSheet;
    private Vector2 _origin;
    private readonly int _drawCount;
    private readonly float _duration;

    private readonly float HalfBoardWidth = GameBoard.BOARD_HALF_WIDTH_PX - BallData.BALL_SIZE / 2;

    private Vector2? _lastCollidePosition;
    private Vector2? _lastCollideRawPosition;

    private readonly FloatEaser _powerUpEaser = new(TimeSpan.FromSeconds(-1));
    public bool PoweredUp { get; private set; }

    public Guideline(GameBoard gameBoard, Slingshot slingshot) : base("guideline")
    {
        _gameBoard = gameBoard;
        _slingshot = slingshot;
        _drawCount = 96;
        _duration = 45f / _drawCount;
        Position = _slingshot.Position;

        _powerUpEaser.SetValueA(0.0f);
        _powerUpEaser.SetEaseFunction(EasingFunctions.ExpoInOut);
        _powerUpEaser.SetTimeLength(TimeSpan.FromSeconds(2), TimeSpan.Zero);

        _powerUpEaser.SetEaseBToAFunction(EasingFunctions.ExpoOut);
        _powerUpEaser.SetTimeLengthBToA(TimeSpan.FromSeconds(3), TimeSpan.Zero);
        _powerUpEaser.SetValueB(1.0f);
    }

    public Vector2? LastCollidePosition
    {
        get
        {
            if (_lastCollidePosition is Vector2 pos)
            {
                return SelfToParentRelPos(pos);
            }

            return null;
        }
    }

    public void SetPowerUp(GameTime gameTime, bool poweredUp)
    {
        if (PoweredUp == poweredUp) return;
        PoweredUp = poweredUp;

        _powerUpEaser.StartEase(gameTime.TotalGameTime, poweredUp);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);

        _texture = content.Load<Texture2D>("Graphics/guideline_full");
        _textureHollow = content.Load<Texture2D>("Graphics/guideline_hollow");


        _previewBallSpriteSheet = new BallData.Assets(content).CreatePreviewBallAnimation();
        _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball spritesheet is not loaded");

        Rotation = _slingshot.Rotation - MathF.PI / 2;

        Recalculate();
    }

    public void Recalculate()
    {
        var direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        _lastCollidePosition = GetEndHexPosition(direction, out Vector2? rawPosOutput);
        _lastCollideRawPosition = rawPosOutput;
    }


    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null, "Guideline texture is not loaded");
        Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball spritesheet is not loaded");

        var direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

        var _progress = (float)(gameTime.TotalGameTime.TotalSeconds / _duration % 1.0);
        var powerUpV = _powerUpEaser.GetValue(gameTime.TotalGameTime);
        var stepSize = 0.3f + (powerUpV * 0.7f);
        var taper = 0.5f + (powerUpV * -0.5f);
        var cutOff = 2f + (powerUpV * (MAX_LENGTH - 2f));
        cutOff *= BallData.BALL_SIZE;
        var alpha = 0.6f + (powerUpV * -0.2f);
        var previewAlpha = powerUpV;

        var selectedTexture = _lastCollidePosition is null ? _textureHollow : _texture;
        for (int i = 0; i < _drawCount; i++)
        {
            float distance = (_progress + i) * BallData.BALL_SIZE * stepSize;
            if (cutOff < distance) break;
            float cutOffFrac = distance / cutOff;
            float scale = 1.0f - (cutOffFrac * taper);

            var collisionFrac = (distance / _lastCollideRawPosition?.Length()) ?? 0;
            var frac = Math.Max(cutOffFrac, collisionFrac);
            var pos = GetCalculatedPosition(direction * distance);
            if (1 < frac) break;

            float firstBallFade = i == 0 ?
            (float)EasingFunctions.PowerInOut(2)(_progress)
             : 1;

            if (powerUpV < 1)
            {
                spriteBatch.Draw(
                    _textureHollow,
                    ScreenPositionO(pos),
                    null,
                    Color.White * alpha * (1.0f - frac) * (1f - powerUpV) * firstBallFade,
                    0.0f,
                    _origin,
                    PixelScale * scale,
                    SpriteEffects.None,
                    0
                );
            }

            if (0 < powerUpV)
            {
                spriteBatch.Draw(
                selectedTexture,
                ScreenPositionO(pos),
                null,
                Color.White * alpha * (1.0f - frac) * powerUpV * firstBallFade,
                0.0f,
                _origin,
                PixelScale * scale,
                SpriteEffects.None,
                0
            );
            }
        }

        if (_lastCollidePosition is null) return;

        if (previewAlpha == 0) return;
        BallData.DrawPreviewBall(
            spriteBatch,
            gameTime,
            _previewBallSpriteSheet,
            ScreenPositionO(_lastCollidePosition.Value),
            previewAlpha
        );
    }

    private Vector2? GetEndHexPosition(Vector2 direction, out Vector2? rawPosOutput)
    {
        rawPosOutput = null;
        var lastNonCollidePos = new Vector2(0, 0);
        var lastCollidePos = new Vector2(0, 0);
        for (int i = 0; i < STEP_COUNT; i++)
        {
            // var calculatedPos = GetCalculatedPosition(direction, i * STEP_SIZE);
            var rawPos = direction * i * STEP_SIZE;
            var calculatedPos = GetCalculatedPosition(rawPos);

            // check collision
            if (_gameBoard.CheckBallCollision(SelfToParentRelPos(calculatedPos), out Hex closestHex))
            {
                lastCollidePos = rawPos;
                break;
            }

            lastNonCollidePos = rawPos;
        }

        if (lastCollidePos == Vector2.Zero) return null;

        for (int subdivs = 0; subdivs < 3; subdivs++)
        {
            var averagePos = (lastNonCollidePos + lastCollidePos) / 2;
            var calculatedPos = GetCalculatedPosition(averagePos);

            if (_gameBoard.CheckBallCollision(SelfToParentRelPos(calculatedPos), out _))
            {
                lastCollidePos = averagePos;
            }
            else
            {
                lastNonCollidePos = averagePos;
            }
        }

        var lastAveragePos = (lastNonCollidePos + lastCollidePos) / 2;
        rawPosOutput = lastAveragePos;
        var lastCalculatedPos = GetCalculatedPosition(lastAveragePos);
        var hex = _gameBoard.ComputeClosestHex(SelfToParentRelPos(lastCalculatedPos));
        return ParentToSelfRelPos(_gameBoard.ConvertHexToCenter(hex));
    }

    private Vector2 GetCalculatedPosition(Vector2 vec)
    {
        // Debug.Assert(progress >= 0.0f && progress <= 1.0f, "Progress must be between 0 and 1");

        // var actualScale = PixelScale * (1.0f - progress);
        // var lengthFromSlingshot = _cutoffLength * progress;
        // var vecToEndPos = direction * lengthFromSlingshot;
        // var vecToEndPos = vec;

        // exploiting modulo and absolute to do reflections

        // shift coordinates so
        // left edge of "negative" board is 0,
        // and right edge of "positive" board is 2 * Width
        var x1 = vec.X + 3 * HalfBoardWidth;

        // loop coordinates so everything falls between 0 and 2 * Width
        // compute modulo using remainder operator
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

        Vector2 calculatedPos = new(x5, vec.Y);
        return calculatedPos;
    }
}
