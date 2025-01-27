using System;
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
    private const float STEP_SIZE = 1.0f / STEP_COUNT;
    private const float MAX_LENGTH = 4800.0f;

    private Texture2D? _texture;
    private Texture2D? _textureHollow;
    private AnimatedTexture2D? _previewBallSpriteSheet;
    private Vector2 _origin;
    private readonly int _drawCount;
    private readonly float _cutoffLength;
    private readonly float _duration;

    private readonly float HalfBoardWidth = GameBoard.BOARD_HALF_WIDTH_PX - BallData.BALL_SIZE / 2;

    public Guideline(GameBoard gameBoard, Slingshot slingshot, int drawCount, float loopDuration, float lineLength = MAX_LENGTH) : base("guideline")
    {
        _gameBoard = gameBoard;
        _slingshot = slingshot;
        _drawCount = drawCount;
        _cutoffLength = lineLength;
        _duration = loopDuration;
        Position = _slingshot.Position;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);

        _texture = content.Load<Texture2D>("Graphics/guideline_full");
        _textureHollow = content.Load<Texture2D>("Graphics/guideline_hollow");
        _previewBallSpriteSheet = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/ball_preview"),
            4, 1, 0.1f, true);
        _previewBallSpriteSheet.TriggerPlayOnNextDraw();
        _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball spritesheet is not loaded");

        Rotation = _slingshot.Rotation - MathF.PI / 2;
    }


    // 192 -> gameboard border
    // 24 -> ball radius
    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null, "Guideline texture is not loaded");
        Debug.Assert(_previewBallSpriteSheet is not null, "Preview ball spritesheet is not loaded");

        var direction = Vector2.Normalize(new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation)));
        Vector2? endHexPos = GetEndHexPosition(direction);

        var _progress = (float)(gameTime.TotalGameTime.TotalSeconds % _duration / _duration);
        int minProgressI = 0;
        float minProgress = 1.0f;
        for (int i = 0; i < _drawCount; i++)
        {
            float progress = (_progress + (float)i / _drawCount) % 1.0f;
            if (progress < minProgress)
            {
                minProgressI = i;
                minProgress = progress;
            }
        }

        var selectedTexture = endHexPos is null ? _textureHollow : _texture;
        for (int i = minProgressI, j = 0; j < _drawCount; i = (i + 1) % _drawCount, j++)
        {
            float subProgress = (_progress + (float)i / _drawCount) % 1.0f;
            var pos = GetCalculatedPosition(direction, subProgress);
            // early break (not sure if this optimization is necessary)
            if (endHexPos is not null && pos.Y + 24.0f < endHexPos?.Y)
            {
                break;
            }
            spriteBatch.Draw(
                selectedTexture,
                pos + ScreenPosition,
                null,
                Color.White * 0.5f * (1.0f - ((pos.Y / endHexPos?.Y) ?? 0.0f)),
                0.0f,
                _origin,
                PixelScale,
                SpriteEffects.None,
                0
            );
        }

        if (endHexPos is null) return;

        _previewBallSpriteSheet.Draw(
            spriteBatch,
            gameTime,
            new Rectangle((int)(endHexPos.Value.X + ScreenPosition.X), (int)(endHexPos.Value.Y + ScreenPosition.Y), BallData.BALL_SIZE, BallData.BALL_SIZE),
            Color.White,
            0.0f,
            new Vector2(BallData.BALL_TEXTURE_SIZE / 2, BallData.BALL_TEXTURE_SIZE / 2)
        );
    }

    private Vector2? GetEndHexPosition(Vector2 direction)
    {
        for (int i = 0; i < STEP_COUNT; i++)
        {
            var calculatedPos = GetCalculatedPosition(direction, i * STEP_SIZE);

            // check collision
            var translatedPos = SelfToParentRelPos(calculatedPos);
            var closestHex = _gameBoard.ComputeClosestHex(translatedPos);

            {
                float rightProgress = i * STEP_SIZE;
                float leftProgress = (i - 1) * STEP_SIZE;
                while (_gameBoard.IsBallAt(closestHex))
                {
                    float midProgress = (rightProgress + leftProgress) / 2;
                    calculatedPos = GetCalculatedPosition(direction, midProgress);
                    translatedPos = SelfToParentRelPos(calculatedPos);
                    closestHex = _gameBoard.ComputeClosestHex(translatedPos);
                    rightProgress = midProgress;
                }
            }

            if (_gameBoard.CheckBallCollision(translatedPos, out _))
            {
                var translatedHexPos = ParentToSelfRelPos(_gameBoard.ConvertHexToCenter(closestHex));
                return translatedHexPos;
            }
        }
        return null;
    }

    private Vector2 GetCalculatedPosition(Vector2 direction, float progress)
    {
        Debug.Assert(progress >= 0.0f && progress <= 1.0f, "Progress must be between 0 and 1");

        var actualScale = PixelScale * (1.0f - progress);
        var lengthFromSlingshot = _cutoffLength * progress;
        var vecToEndPos = direction * lengthFromSlingshot;

        // exploiting modulo and absolute to do reflections

        // shift coordinates so
        // left edge of "negative" board is 0,
        // and right edge of "positive" board is 2 * Width
        var x1 = vecToEndPos.X + 3 * HalfBoardWidth;

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
        // left and right edge of board is on Â±0.5 * Width,
        // and 0 in the center of the board
        var x5 = x4 - HalfBoardWidth;

        Vector2 calculatedPos = new(x5, vecToEndPos.Y);
        return calculatedPos;
    }
}
