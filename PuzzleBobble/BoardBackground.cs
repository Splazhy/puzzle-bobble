using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.Easer;

namespace PuzzleBobble;

public class BoardBackground : GameObject
{
    private readonly GameBoard _gameBoard;
    private Texture2D? backgrounds;
    private Texture2D? backgroundsGray;
    private Texture2D? leftBorder;
    private Texture2D? rightBorder;
    private readonly FloatEaser _failEaser = new(TimeSpan.FromSeconds(-1));

    public BoardBackground(GameBoard gameBoard) : base("board_background")
    {
        _gameBoard = gameBoard;

        _failEaser.SetValueA(0.0f);
        _failEaser.SetEaseFunction(EasingFunctions.PowerInOut(2));
        _failEaser.SetTimeLength(TimeSpan.FromSeconds(2), TimeSpan.Zero);
        _failEaser.SetValueB(1.0f);
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);

        backgrounds = content.Load<Texture2D>("Graphics/board_bg_parallax");
        backgroundsGray = content.Load<Texture2D>("Graphics/board_bg_parallax_gray");
        leftBorder = content.Load<Texture2D>("Graphics/border_left");
        rightBorder = content.Load<Texture2D>("Graphics/border_right");
    }

    public void Fail(GameTime gameTime)
    {
        _failEaser.StartEase(gameTime.TotalGameTime, true);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(backgrounds is not null, "Backgrounds are not loaded.");
        Debug.Assert(backgroundsGray is not null, "Backgrounds gray are not loaded.");
        Debug.Assert(leftBorder is not null, "Left border is not loaded.");
        Debug.Assert(rightBorder is not null, "Right border is not loaded.");

        var grayV = _failEaser.GetValue(gameTime.TotalGameTime);
        var tileWidth = backgrounds.Width / 3;

        if (grayV < 1)
        {
            ParallaxDraw(spriteBatch, backgrounds,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgrounds.Height / 2) * PIXEL_SIZE,
                new Rectangle(0, 0, tileWidth, backgrounds.Height),
                _gameBoard.Position.Y, 4);
            ParallaxDraw(spriteBatch, backgrounds,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgrounds.Height / 2) * PIXEL_SIZE,
                new Rectangle(tileWidth * 1, 0, tileWidth, backgrounds.Height),
                _gameBoard.Position.Y, 3);
            ParallaxDraw(spriteBatch, backgrounds,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgrounds.Height / 2) * PIXEL_SIZE,
                new Rectangle(tileWidth * 2, 0, tileWidth, backgrounds.Height),
                _gameBoard.Position.Y, 2);
        }

        if (0 < grayV)
        {
            ParallaxDraw(spriteBatch, backgroundsGray,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgroundsGray.Height / 2) * PIXEL_SIZE,
                new Rectangle(0, 0, tileWidth, backgroundsGray.Height),
                _gameBoard.Position.Y, 4, grayV);
            ParallaxDraw(spriteBatch, backgroundsGray,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgroundsGray.Height / 2) * PIXEL_SIZE,
                new Rectangle(tileWidth * 1, 0, tileWidth, backgroundsGray.Height),
                _gameBoard.Position.Y, 3, grayV);
            ParallaxDraw(spriteBatch, backgroundsGray,
                new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX, -backgroundsGray.Height / 2) * PIXEL_SIZE,
                new Rectangle(tileWidth * 2, 0, tileWidth, backgroundsGray.Height),
                _gameBoard.Position.Y, 2, grayV);
        }

        ParallaxDraw(spriteBatch, leftBorder,
            new Vector2(-GameBoard.BOARD_HALF_WIDTH_PX - leftBorder.Width, -leftBorder.Height / 2) * PIXEL_SIZE,
            new Rectangle(0, 0, leftBorder.Width, leftBorder.Height),
            _gameBoard.Position.Y, 1);
        ParallaxDraw(spriteBatch, rightBorder,
            new Vector2(GameBoard.BOARD_HALF_WIDTH_PX, -rightBorder.Height / 2) * PIXEL_SIZE,
            new Rectangle(0, 0, rightBorder.Width, rightBorder.Height),
            _gameBoard.Position.Y, 1);
    }

    private void ParallaxDraw(SpriteBatch spriteBatch, Texture2D sheet, Vector2 pos, Rectangle sourceRect, float gameBoardY, float divFactor, float alpha = 1)
    {
        var sourceY = divFactor == 0 ? 0 : -(gameBoardY / divFactor);
        var subPixelY = sourceY - (int)sourceY;
        var finalSourceRect = sourceRect;
        finalSourceRect.Offset(0, (int)sourceY);
        finalSourceRect.Inflate(0, 1);
        spriteBatch.Draw(
            sheet,
            ParentTranslate + pos - new Vector2(0, subPixelY) * PIXEL_SIZE,
            finalSourceRect,
            Color.White * alpha, 0, Vector2.Zero, PIXEL_SIZE, SpriteEffects.None, 0
        );

    }
}