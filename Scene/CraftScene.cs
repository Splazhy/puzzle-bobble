using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;

namespace PuzzleBobble.Scene;

public class CraftScene : AbstractScene
{
    private Desktop? _desktop;

    public CraftScene() : base("scene_craft")
    {
    }

    public override void Initialize(Game game)
    {

    }

    public override void LoadContent(ContentManager content)
    {
        base.LoadContent(content);
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
            VerticalAlignment = VerticalAlignment.Bottom
        };
        BackBtn.Click += (sender, args) => ChangeScene(new MenuScene());

        var CraftBtn = new Button
        {
            Content = new Label { Text = "C" },
            Left = 372,
            Top = 240,
            Padding = new Thickness(10, 5)
        };

        var OrderBtn = new Button
        {
            Content = new Label { Text = "O" },
            Left = 372,
            Top = 270,
            Padding = new Thickness(10, 5)
        };

        // Label
        var inventoryLabel = new Label
        {
            Text = "Inventory",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TextColor = Color.White,
        };

        var CraftLabel = new Label
        {
            Text = "Crafting Potion",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TextColor = Color.White,
        };

        var OrderLabel = new Label
        {
            Text = "Order Items",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TextColor = Color.White,
        };

        // Panel
        var rightTop = new Panel
        {
            Widgets =
            {
                inventoryLabel
            }
        };
        rightGrid.Widgets.Add(rightTop);
        Grid.SetRow(rightTop, 0);

        var dividerLine = new HorizontalSeparator
        {
            Thickness = 2,
            Color = Color.Black,
        };
        rightGrid.Widgets.Add(dividerLine);
        Grid.SetRow(dividerLine, 1);

        var rightBottom = new Panel
        {
            Widgets =
            {
                CraftLabel
            }
        };
        rightGrid.Widgets.Add(rightBottom);
        Grid.SetRow(rightBottom, 2);

        var leftPanel = new Panel
        {
            Widgets =
            {
                CraftBtn,
                OrderBtn,
                BackBtn
            }
        };
        mainGrid.Widgets.Add(leftPanel);
        Grid.SetColumn(leftPanel, 0);

        var rightPanel = new Panel
        {
            Background = new SolidBrush(new Color(158, 69, 57)),
            Widgets =
            {
                rightGrid
            }
        };
        mainGrid.Widgets.Add(rightPanel);
        Grid.SetColumn(rightPanel, 1);

        // Button event
        OrderBtn.Click += (sender, args) =>
        {
            rightBottom.Widgets.Clear();
            rightBottom.Widgets.Add(OrderLabel);

            Grid.SetRow(rightBottom, 2);
        };

        CraftBtn.Click += (sender, args) =>
       {
           rightBottom.Widgets.Clear();
           rightBottom.Widgets.Add(CraftLabel);

           Grid.SetRow(rightBottom, 2);
       };

        _desktop = new Desktop
        {
            Root = mainGrid
        };
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentTranslate)
    {
        _desktop?.Render();
    }
}
