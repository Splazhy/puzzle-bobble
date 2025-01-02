using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.Scene;

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

        currentScene.SceneChanged -= ChangeScene;
        currentScene.Deinitialize();
        currentScene = newScene;
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
