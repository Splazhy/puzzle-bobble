using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PuzzleBobble;

public class SceneManager
{
    private Game _game;
    public AbstractScene currentScene { get; private set; }

    public SceneManager(Game game)
    {
        _game = game;
        currentScene = Scenes.MENU;
        currentScene.SceneChanged += ChangeScene;
    }

    private void ChangeScene(AbstractScene oldScene, AbstractScene newScene)
    {
        // The scene invoking the event must be the current scene
        Debug.Assert(currentScene == oldScene);

        currentScene.Deinitialize();
        currentScene = newScene;

        // https://stackoverflow.com/questions/19314983/attaching-an-event-handler-multiple-times
        //
        // Unsubscribe if the scene was already subscribed to
        // prevent duplicate subscriptions to the same event.
        // Unsubscribe (-=) is ignored when already unsubscribed.
        currentScene.SceneChanged -= ChangeScene;
        currentScene.SceneChanged += ChangeScene;

        currentScene.Initialize(_game);
        currentScene.LoadContent(_game.Content);
    }

    public void Initialize(Game game)
    {
        currentScene.Initialize(game);
    }

    public void LoadContent(ContentManager content)
    {
        currentScene.LoadContent(content);
    }

    public void Update(GameTime gameTime)
    {
        currentScene.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        currentScene.Draw(spriteBatch, gameTime);
    }
}
