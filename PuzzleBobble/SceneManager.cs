using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using PuzzleBobble.Scene;

namespace PuzzleBobble;

public class SceneManager
{
    private readonly Game _game;
    private readonly SaveData _saveData;
    public AbstractScene CurrentScene { get; private set; }

    public SceneManager(Game game, SaveData saveData, AbstractScene scene)
    {
        _game = game;
        _saveData = saveData;
        CurrentScene = scene;
        CurrentScene.SceneChanged += ChangeScene;
    }

    private void ChangeScene(AbstractScene oldScene, AbstractScene newScene)
    {
        // The scene invoking the event must be the current scene
        Debug.Assert(CurrentScene == oldScene);

        CurrentScene.SceneChanged -= ChangeScene;
        CurrentScene = newScene;
        CurrentScene.SceneChanged += ChangeScene;

        CurrentScene.Initialize(_game, _saveData);
        CurrentScene.LoadContent(_game.Content);
    }

    public void Initialize()
    {
        CurrentScene.Initialize(_game, _saveData);
    }

    public void LoadContent(ContentManager content)
    {
        CurrentScene.LoadContent(content);
    }

    public void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        CurrentScene.Update(gameTime, parentTranslate);
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        CurrentScene.Draw(spriteBatch, gameTime);
    }

    public Desktop? DrawMyra()
    {
        return CurrentScene.DrawMyra();
    }

    public Color GetBackgroundColor()
    {
        return CurrentScene.BackgroundColor;
    }
}
