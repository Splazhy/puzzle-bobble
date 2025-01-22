using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble.Scene;

public abstract class AbstractScene : GameObject
{
    protected AbstractScene(string name) : base(name)
    {
    }

    public event SceneChangedHandler? SceneChanged;

    public delegate void SceneChangedHandler(AbstractScene oldScene, AbstractScene newScene);

    protected virtual void ChangeScene(AbstractScene newScene)
    {
        // ?. operator is used to check if the event is null
        // before invoking it for thread safety.
        SceneChanged?.Invoke(this, newScene);
    }

    public abstract void Initialize(Game game);
    public virtual void Deinitialize()
    {
        children.Clear();
        pendingChildren.Clear();
    }
}
