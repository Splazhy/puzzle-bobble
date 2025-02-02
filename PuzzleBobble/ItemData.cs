using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
namespace PuzzleBobble;

public class ItemData
{
    public static readonly int ITEM_SIZE = 16;
    public static int ITEM_DRAW_SIZE => ITEM_SIZE * GameObject.PIXEL_SIZE;

    public class Assets
    {
        public readonly Texture2D ItemSpritesheet;

        public Assets(ContentManager content)
        {
            ItemSpritesheet = content.Load<Texture2D>("Graphics/item");
        }
    }

    public readonly string ItemId;
    private readonly Rectangle _sourceRect;

    public ItemData(string itemId)
    {
        ItemId = itemId;

        if (itemId.StartsWith("ingredient-"))
        {
            var name = itemId[11..];
            switch (name)
            {
                case "rainbow":
                    _sourceRect = new Rectangle(0, ITEM_SIZE, ITEM_SIZE, ITEM_SIZE);
                    break;
                case "bomb":
                    _sourceRect = new Rectangle(ITEM_SIZE, ITEM_SIZE, ITEM_SIZE, ITEM_SIZE);
                    break;
                case "stone":
                    _sourceRect = new Rectangle(ITEM_SIZE * 2, ITEM_SIZE, ITEM_SIZE, ITEM_SIZE);
                    break;
                default:
                    var index = int.Parse(name);
                    _sourceRect = new Rectangle(index * ITEM_SIZE, 0, ITEM_SIZE, ITEM_SIZE);
                    break;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Assets assets, Vector2 screenPosition, Vector2 fracOrigin)
    {
        spriteBatch.Draw(
            assets.ItemSpritesheet,
            screenPosition,
            _sourceRect,
            Color.White,
            0,
            fracOrigin * ITEM_DRAW_SIZE,
            GameObject.PIXEL_SIZE,
            SpriteEffects.None,
            0
        );
    }
}