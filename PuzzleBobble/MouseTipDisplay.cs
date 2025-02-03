using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;
public class MouseTipDisplay : GameObject
{
    private Texture2D? _texture;
    private AnimatedTexture2D? _anim;
    public MouseTipDisplay() : base("MouseTipDisplay")
    {
    }

    public override void LoadContent(ContentManager content)
    {
        _texture = content.Load<Texture2D>("Graphics/mouse_tutorial");
        _anim = new AnimatedTexture2D(_texture, 2, 1, 0.5f, true);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_texture is not null && _anim is not null);
        // spriteBatch.Draw(_texture, ScreenPosition, null, Color.White, 0f, new Vector2(_texture.Width / 2, _texture.Height / 2), PixelScale, SpriteEffects.None, 0f);

        _anim.Draw(
            spriteBatch,
            gameTime,
            ScreenPosition,
            Color.White,
            0f,
            new Vector2(_texture.Width / 4, _texture.Height / 2),
            PIXEL_SIZE
        );
    }
}