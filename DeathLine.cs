using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class DeathLine : GameObject
{
    private AnimatedTexture2D? _spriteSheet;

    public DeathLine(Game game) : base("deathline")
    {
        Position = new Vector2(0, 208); // value hand-picked with my eye (pixel-perfect btw)
        Scale = new Vector2(3.0f, 3.0f);
    }

    public override void LoadContent(ContentManager content)
    {
        _spriteSheet = new AnimatedTexture2D(
            content.Load<Texture2D>("Graphics/deathline"),
            1, 12, 0.025f, true, true);
        _spriteSheet.Play();
        base.LoadContent(content);
    }

    public override void Update(GameTime gameTime)
    {
        _spriteSheet?.Update(gameTime);
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        _spriteSheet?.Draw(
            spriteBatch,
            ScreenPosition,
            Color.White,
            0.0f,
            new Vector2(_spriteSheet.frameWidth / 2, 0),
            Scale.X
        );
        base.Draw(spriteBatch, gameTime);
    }
}