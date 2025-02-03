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

public class CraftScene : AbstractScene
{
    private SpriteFont? _font;
    private Desktop? _desktop;
    private SaveData? _saveData;

    private ItemData.Assets? _itemAssets;

    public override Color BackgroundColor => new(69, 41, 63);


    public CraftScene() : base("scene_craft")
    {
    }

    private readonly Dictionary<ItemData, int> inventory = [];

    public override void Initialize(Game game, SaveData sd)
    {
        _saveData = sd;

        using var transaction = _saveData.BeginTransaction();
        var unaccountedPlays = _saveData.GetUnaccountedPlayHistoryEntries();
        foreach (var play in unaccountedPlays)
        {
            var info = _saveData.GetPlayHistory(play);
            if (info.Status != GameState.Success)
            {
                if (info.Status == GameState.Playing)
                {
                    _saveData.UpdatePlayHistoryEntry(play, info.Duration, GameState.Fail);
                }
                _saveData.SetPlayHistoryAccountedFor(play);
                continue;
            }
            var details = _saveData.GetPlayHistoryDetails(play);
            foreach (var (statItem, count) in details)
            {
                if (statItem.StartsWith("ball-"))
                {
                    var ballId = statItem[5..];
                    _saveData.AddToInventory($"ingredient-{ballId}", count);
                }
            }
            _saveData.SetPlayHistoryAccountedFor(play);
        }

        transaction.Commit();
        _saveData.CleanupCachedStmts();

        RefreshFromSaveData();
    }

    public override void LoadContent(ContentManager content)
    {
        _itemAssets = new ItemData.Assets(content);
        _font = content.Load<SpriteFont>("Fonts/Arial24");

        SetupMyra();
        base.LoadContent(content);
    }

    public void RefreshFromSaveData()
    {
        inventory.Clear();
        Debug.Assert(_saveData is not null);

        var saveInventory = _saveData.GetInventory();
        foreach (var (itemId, count) in saveInventory)
        {
            var itemData = new ItemData(itemId);
            inventory[itemData] = count;
        }
    }

    private void SetupMyra()
    {
        // Grid

        var mainGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 8,
        };

        mainGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        mainGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));

        var rightGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 0,
        };

        rightGrid.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
        rightGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        rightGrid.RowsProportions.Add(new Proportion(ProportionType.Part, 1));

        // Button
        var BackBtn = new Button
        {
            Content = new Label { Text = "Back" },
            Left = 366,
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(20, 10),
        };
        BackBtn.Click += (sender, args) => ChangeScene(new MenuScene());

        // Panel
        var rightTop = new Panel
        {
            Widgets =
            {
            }
        };
        rightGrid.Widgets.Add(rightTop);
        Grid.SetRow(rightTop, 0);

        var rightBottom = new Panel
        {
            Widgets =
            {
                BackBtn
            }
        };
        rightGrid.Widgets.Add(rightBottom);
        Grid.SetRow(rightBottom, 2);

        var leftPanel = new Panel
        {
            Widgets =
            {
            }
        };
        mainGrid.Widgets.Add(leftPanel);
        Grid.SetColumn(leftPanel, 0);

        var rightPanel = new Panel
        {
            Widgets =
            {
                rightGrid
            }
        };
        mainGrid.Widgets.Add(rightPanel);
        Grid.SetColumn(rightPanel, 1);

        // TODO: well, maybe someday later

        _desktop = new Desktop
        {
            Root = mainGrid
        };
    }


    public override void Update(GameTime gameTime, Vector2 parentTranslate)
    {
        base.Update(gameTime, parentTranslate);
        UpdateChildren(gameTime);

        if (GoBackTriggered)
        {
            ChangeScene(new MenuScene());
        }
    }

    private Vector2 textOffSet = new Vector2(900, 100);
    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Debug.Assert(_itemAssets is not null);

        DrawChildren(spriteBatch, gameTime);

        foreach (var ((item, count), index) in inventory.Select((e, i) => (e, i)))
        {
            item.Draw(spriteBatch, _itemAssets, new Vector2(0 + textOffSet.X, 16 * index * PIXEL_SIZE + textOffSet.Y), Vector2.Zero);
            spriteBatch.DrawString(_font, $"{item.ItemId} x {count}", new Vector2(16 * PIXEL_SIZE + textOffSet.X, 16 * index * PIXEL_SIZE + textOffSet.Y), Color.White);
        }
    }

    public override Desktop? DrawMyra()
    {
        return _desktop;
    }

}
