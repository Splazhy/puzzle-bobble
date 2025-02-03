using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace PuzzleBobble.Scene;

public class MimiScene : AbstractScene
{
    private ItemData.Assets? _itemAssets;

    public override Color BackgroundColor => new(69, 41, 63);

    private Texture2D? _cauldronTexture;
    private Texture2D? _homeDecorTexture;
    private Texture2D? _mimiTexture;
    private AnimatedTexture2D? _mimiAnim;
    private Texture2D? _mimiEyesTexture;
    private AnimatedTexture2D? _mimiEyesAnim;
    private TimeSpan _nextMimiBlinkTime = TimeSpan.Zero;
    private readonly Random _rand = new();

    private SceneManager? _sceneManager;

    public MimiScene() : base("scene_craft")
    {
    }

    public override void Initialize(Game game, SaveData sd)
    {
        _sceneManager = new SceneManager(game, sd, new MenuScene());
        _sceneManager.UpperSceneChanged += ChangeUpperScene;

        _sceneManager.Initialize();

        children = [
            new MimiBook() {
                Position = new Vector2(-82, 31)
            },
        ];
    }

    public override void LoadContent(ContentManager content)
    {
        Debug.Assert(_sceneManager is not null);
        _sceneManager.LoadContent(content);

        _itemAssets = new ItemData.Assets(content);
        _cauldronTexture = content.Load<Texture2D>("Graphics/cauldron");
        _homeDecorTexture = content.Load<Texture2D>("Graphics/home_decor");
        _mimiTexture = content.Load<Texture2D>("Graphics/mimi");
        _mimiAnim = new AnimatedTexture2D(_mimiTexture, 3, 1, 0.5f, true);
        _mimiEyesTexture = content.Load<Texture2D>("Graphics/mimi_eyes");
        _mimiEyesAnim = new AnimatedTexture2D(_mimiEyesTexture,
            new Rectangle(_mimiEyesTexture.Width / 3, 0, _mimiEyesTexture.Width, _mimiEyesTexture.Height),
         3, 1, 0.05f, false)
        {
            KeepDrawingAfterFinish = true
        };

        base.LoadContent(content);
    }

    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        Debug.Assert(_sceneManager is not null);

        base.Update(gameTime, parentTranslate);
        UpdateChildren(gameTime);
        _sceneManager.Update(gameTime, parentTranslate);

        Debug.Assert(_mimiEyesAnim is not null);
        if (_nextMimiBlinkTime < gameTime.TotalGameTime)
        {
            _mimiEyesAnim.Play(gameTime);
            _nextMimiBlinkTime = gameTime.TotalGameTime + TimeSpan.FromSeconds(4.8 + 3 * _rand.NextDouble());
        }
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_itemAssets is not null);
        Debug.Assert(_cauldronTexture is not null);
        Debug.Assert(_homeDecorTexture is not null);
        Debug.Assert(_mimiTexture is not null);
        Debug.Assert(_mimiAnim is not null);
        Debug.Assert(_mimiEyesTexture is not null);
        Debug.Assert(_mimiEyesAnim is not null);
        Debug.Assert(_sceneManager is not null);


        spriteBatch.Draw(
            _homeDecorTexture,
            ScreenPositionO(new Vector2(90, 0)),
            null,
            Color.White,
            0,
            new Vector2(_homeDecorTexture.Width / 2, _homeDecorTexture.Height / 2),
            PIXEL_SIZE,
            SpriteEffects.None,
            0
        );

        _mimiAnim.Draw(
            spriteBatch,
            gameTime,
            ScreenPositionO(new Vector2(-171, 251)),
            Color.White,
            0,
            new Vector2(_mimiTexture.Width / 6, _mimiTexture.Height),
            PIXEL_SIZE
        );

        _mimiEyesAnim.Draw(
            spriteBatch,
            gameTime,
            ScreenPositionO(new Vector2(-171, 251)),
            Color.White,
            0,
            new Vector2(_mimiEyesTexture.Width / 6, _mimiEyesTexture.Height),
            PIXEL_SIZE
        );

        DrawChildren(spriteBatch, gameTime);
        _sceneManager.Draw(spriteBatch, gameTime);

        spriteBatch.Draw(
            _cauldronTexture,
            ScreenPositionO(new Vector2(-113, 100)),
            null,
            Color.White,
            0,
            new Vector2(_cauldronTexture.Width / 2, 17),
            PIXEL_SIZE,
            SpriteEffects.None,
            0
        );
    }

    public override Desktop? DrawMyra()
    {
        Debug.Assert(_sceneManager is not null);
        return _sceneManager.DrawMyra();
    }

}
