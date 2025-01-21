using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PuzzleBobble.Scene;

namespace PuzzleBobble;

public class SceneManager
{
    private readonly Game _game;
    public AbstractScene CurrentScene { get; private set; }

    public SceneManager(Game game)
    {
        _game = game;
        CurrentScene = Scenes.GAME;
        CurrentScene.SceneChanged += ChangeScene;
    }

    private void ChangeScene(AbstractScene oldScene, AbstractScene newScene)
    {
        // The scene invoking the event must be the current scene
        Debug.Assert(CurrentScene == oldScene);

        CurrentScene.SceneChanged -= ChangeScene;
        CurrentScene.Deinitialize();
        CurrentScene = newScene;
        CurrentScene.SceneChanged += ChangeScene;

        CurrentScene.Initialize(_game);
        CurrentScene.LoadContent(_game.Content);
    }

    public void Initialize(Game game)
    {
        CurrentScene.Initialize(game);
    }

    public void LoadContent(ContentManager content)
    {
        CurrentScene.LoadContent(content);
    }

    public void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        CurrentScene.Update(gameTime, parentTranslate);
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        CurrentScene.Draw(spriteBatch, gameTime, parentTranslate);
    }
}
