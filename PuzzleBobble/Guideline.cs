using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.HexGrid;

namespace PuzzleBobble;


public class Guideline : GameObject
{
    private readonly GameBoard _gameBoard;
    private readonly Slingshot _slingshot;

    private const int STEP_COUNT = 200;
    private static readonly float STEP_SIZE = BallData.BALL_SIZE / 2;
    private const float MAX_LENGTH = 4800.0f;

    private Texture2D? _texture;
    private Texture2D? _textureHollow;
    private AnimatedTexture2D? _previewBallSpriteSheet;
    private Vector2 _origin;
    private readonly int _drawCount;
    private readonly float _cutoffLength;
    private readonly float _duration;

    private readonly float HalfBoardWidth = GameBoard.BOARD_HALF_WIDTH_PX - BallData.BALL_SIZE / 2;

    private Vector2? _lastCollidePosition;

    public Guideline(GameBoard gameBoard, Slingshot slingshot) : base("guideline")
    {
        _gameBoard = gameBoard;
        _slingshot = slingshot;
        _drawCount = 96;
        _cutoffLength = MAX_LENGTH;
        _duration = 45f / _drawCount;
        Position = _slingshot.Position;
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
        _lastCollidePosition = GetEndHexPosition(direction);
    }


    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null, "Guideline texture is not loaded");
        Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball spritesheet is not loaded");

        var direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

        var _progress = (float)(gameTime.TotalGameTime.TotalSeconds / _duration % 1.0);

        var selectedTexture = _lastCollidePosition is null ? _textureHollow : _texture;
        for (int i = 0; i < _drawCount; i++)
        {
            float subProgress = (_progress % 1.0f) + i;
            var pos = GetCalculatedPosition(direction * subProgress * BallData.BALL_SIZE);
            // early break (not sure if this optimization is necessary)
            if (_lastCollidePosition is not null && pos.Y + BallData.BALL_SIZE / 2 < _lastCollidePosition?.Y)
            {
                break;
            }
            spriteBatch.Draw(
                selectedTexture,
                ScreenPositionO(pos),
                null,
                Color.White * 0.5f * (1.0f - ((pos.Y / _lastCollidePosition?.Y) ?? 0.0f)),
                0.0f,
                _origin,
                PixelScale,
                SpriteEffects.None,
                0
            );
        }

        if (_lastCollidePosition is null) return;

        BallData.DrawPreviewBall(
            spriteBatch,
            gameTime,
            _previewBallSpriteSheet,
            ScreenPositionO(_lastCollidePosition.Value)
        );
    }

    private Vector2? GetEndHexPosition(Vector2 direction)
    {
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
