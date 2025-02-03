using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class SlingshotStaff : GameObject
{
    public Vector2 TargetPosition;
    public float TargetRotation;
    public Vector2 TargetPosition2;
    public float TargetRotation2;
    public TimeSpan ChangeUntil;
    private Texture2D? _slingshotTexture;
    private SpriteFont? _debugfont;
    public SlingshotStaff() : base("SlingshotStaff")
    {
    }

    public override void LoadContent(ContentManager content)
    {
        _slingshotTexture = content.Load<Texture2D>("Graphics/slingshot");
        _debugfont = content.Load<SpriteFont>("Fonts/Arial24");
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);

        // Decay Velocity by 0.6x per second
        // Velocity = Velocity * MathF.Pow(0.001f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        // Velocity += (TargetPosition - Position);

        var chosenTargetRot = (gameTime.TotalGameTime < ChangeUntil) ? TargetRotation2 : TargetRotation;
        var chosenTargetPos = (gameTime.TotalGameTime < ChangeUntil) ? TargetPosition2 : TargetPosition;
        var rotDiff = chosenTargetRot - Rotation;
        Rotation += rotDiff * 8f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        var posDiff = chosenTargetPos - Position;
        Position += posDiff * 8f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        // Position += posDiff / 2f;
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_slingshotTexture is not null, "Slingshot texture is not loaded.");

        spriteBatch.Draw(
            _slingshotTexture,
            ScreenPosition,
            null,
            Color.White,
            Rotation,
            // anchors the texture from the top by 10 pixels no matter the height
            // so that the ball positioned in the center nicely.
            new Vector2(_slingshotTexture.Width / 2, 10),
            PixelScale,
            SpriteEffects.None,
            0
        );

        if (DebugOptions.SLINGSHOTSTAFF_SHOW_POSITIONS)
        {
            spriteBatch.DrawString(
                _debugfont,
                $"pos {Position}\ntpos {TargetPosition}\nvel {Velocity}",
                ScreenPositionO(new Vector2(15f / 3, 15f / 3)),
                Color.White
            );
        }

    }
}