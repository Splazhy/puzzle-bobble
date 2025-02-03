using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;

namespace PuzzleBobble.Scene;
public delegate void SceneChangedHandler(AbstractScene newScene);

public abstract class AbstractScene : GameObject
{
    protected AbstractScene(string name) : base(name)
    {
    }

    public event SceneChangedHandler? UpperSceneChanged;
    public event SceneChangedHandler? SceneChanged;

    protected virtual void ChangeScene(AbstractScene newScene)
    {
        SceneChanged?.Invoke(newScene);
    }
    protected virtual void ChangeUpperScene(AbstractScene newScene)
    {
        UpperSceneChanged?.Invoke(newScene);
    }

    public abstract void Initialize(Game game, SaveData sd);

    public virtual Desktop? DrawMyra()
    {
        return null;
    }

    public virtual Color BackgroundColor => Color.RosyBrown;

    private bool _backBtnDown = true;
    /// <summary>
    /// Becomes true for one frame when the back button is pressed.
    /// </summary>
    protected bool GoBackTriggered { get; private set; }
    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        var backBtn = Keyboard.GetState().IsKeyDown(Keys.Escape)
            || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
        GoBackTriggered = backBtn && !_backBtnDown;
        _backBtnDown = backBtn;
    }
}
