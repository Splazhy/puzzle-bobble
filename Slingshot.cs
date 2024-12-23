using System;
using Microsoft.Xna.Framework;

namespace puzzle_bobble;

public class Slingshot : DrawableGameComponent
{
    // Rotations in this class are in radians
    public static readonly float MIN_ROTATION = (float)Math.PI / 3.0f;
    public static readonly float MAX_ROTATION = 5 * (float)Math.PI / 3.0f;
    protected float rotation = 0.0f;

    public Slingshot(Game game) : base(game)
    {
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {

    }

    public override void Draw(GameTime gameTime)
    {

    }

}
