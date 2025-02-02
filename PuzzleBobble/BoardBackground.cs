using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class BoardBackground : GameObject
{
    private readonly GameBoard _gameBoard;
    private Texture2D? background;
    private Texture2D? leftBorder;
    private Texture2D? rightBorder;

    public BoardBackground(GameBoard gameBoard) : base("board_background")
    {
        _gameBoard = gameBoard;
    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);

        background = content.Load<Texture2D>("Graphics/board_bg");
        leftBorder = content.Load<Texture2D>("Graphics/border_left");
        rightBorder = content.Load<Texture2D>("Graphics/border_right");
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(background is not null, "Background is not loaded.");
        Debug.Assert(leftBorder is not null, "Left border is not loaded.");
        Debug.Assert(rightBorder is not null, "Right border is not loaded.");

        var BOARD_HALF_WIDTH_PX = GameBoard.BOARD_HALF_WIDTH_PX;
        var sourceY = -(_gameBoard.Position.Y / 3);
        var subPixelY = sourceY - (int)sourceY;
        spriteBatch.Draw(
            background,
            ParentTranslate + new Vector2(-BOARD_HALF_WIDTH_PX, -background.Height / 2 - subPixelY) * PIXEL_SIZE,
            new Rectangle(0, (int)sourceY, background.Width, background.Height + 1), Color.White, 0, Vector2.Zero, PIXEL_SIZE, SpriteEffects.None, 0
        );
        spriteBatch.Draw(
            leftBorder,
            ParentTranslate + new Vector2(-BOARD_HALF_WIDTH_PX - leftBorder.Width, -leftBorder.Height / 2) * PIXEL_SIZE,
            null, Color.White, 0, Vector2.Zero, PIXEL_SIZE, SpriteEffects.None, 0
        );
        spriteBatch.Draw(
            rightBorder,
            ParentTranslate + new Vector2(+BOARD_HALF_WIDTH_PX, -rightBorder.Height / 2) * PIXEL_SIZE,
            null, Color.White, 0, Vector2.Zero, PIXEL_SIZE, SpriteEffects.None, 0
        );
    }
}