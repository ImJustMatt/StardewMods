using System;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Views
{
    internal class HSLColorPicker : DiscreteColorPicker
    {
        private const int Height = 598;
        private const int Width = 98;
        private const int Cells = 16;
        private const int Gap = 6;

        private static int BarHeight;
        private static int CellHeight;
        private static int CellsHeight;

        private static Texture2D HueBar;
        private static Color[] Colors;
        private static int TotalColors;
        internal readonly ClickableComponent HueSlider;
        internal readonly ClickableComponent LitSlider;
        internal readonly ClickableComponent SatSlider;

        internal readonly ClickableTextureComponent TransparentBox;

        private HSLColor _color;

        private Bar Hold = Bar.None;

        internal HSLColorPicker(int xPosition, int yPosition, Item itemToDrawColored = null)
            : base(xPosition, yPosition, 0, itemToDrawColored)
        {
            visible = Game1.player.showChestColorPicker;
            height = Height;
            width = Width;
            totalColors = TotalColors;

            TransparentBox = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + borderWidth / 2, yPositionOnScreen + borderWidth / 2, 7, 7),
                Game1.mouseCursors,
                new Rectangle(295, 503, 7, 7),
                4f
            );
            HueSlider = new ClickableComponent(new Rectangle(xPositionOnScreen + borderWidth / 2, 0, 20, 16), "Hue");
            SatSlider = new ClickableComponent(new Rectangle(xPositionOnScreen + borderWidth / 2 + 36, 0, 20, 16), "Sat");
            LitSlider = new ClickableComponent(new Rectangle(xPositionOnScreen + borderWidth / 2 + 36, 0, 20, 16), "Lit");
            populateClickableComponentList();

            if (itemToDrawColored is not Chest chest)
                return;

            _color = HSLColor.FromColor(chest.playerChoiceColor.Value);
            colorSelection = (int) (_color.H * HueBar.Height);
        }

        internal static void Init(IContentHelper contentHelper)
        {
            BarHeight = Height + Gap - borderWidth - 46;
            CellsHeight = (BarHeight - Gap) / 2;
            CellHeight = CellsHeight / Cells;

            HueBar = contentHelper.Load<Texture2D>("assets/Hue.png");
            TotalColors = HueBar.Width * HueBar.Height;
            Colors = new Color[TotalColors];
            HueBar.GetData(Colors);
        }

        internal void ReleaseBar()
        {
            Hold = Bar.None;
        }

        public new int getSelectionFromColor(Color c)
        {
            _color = HSLColor.FromColor(c);
            return c.Equals(Color.Black)
                ? 0
                : (int) MathHelper.Clamp(_color.H * totalColors, 0f, totalColors);
        }

        public new Color getCurrentColor()
        {
            return _color.ToRgbColor();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!visible)
                return;

            var area = new Rectangle(
                xPositionOnScreen + borderWidth / 2,
                yPositionOnScreen + borderWidth / 2,
                58,
                height - borderWidth);

            if (!area.Contains(x, y) && Hold == Bar.None)
                return;

            if (playSound && Hold == Bar.None)
                Game1.playSound("coin");

            var selection = y - area.Top;
            if (selection <= 36 && Hold == Bar.None)
            {
                colorSelection = 0;
                _color.S = 0;
                _color.L = 0;
            }
            else if (x <= area.Left + 32 && Hold == Bar.None || Hold == Bar.Hue)
            {
                // Hue Selection
                Hold = Bar.Hue;
                selection = (int) MathHelper.Clamp((selection - 36f) / BarHeight * totalColors, 1, totalColors);
                var color = HSLColor.FromColor(Colors[selection - 1]);
                if (colorSelection == 0)
                    _color = color;
                else
                    _color.H = color.H;
                colorSelection = selection;
            }
            else
            {
                selection -= 36;
                if (selection <= CellsHeight || Hold == Bar.Saturation)
                {
                    // Saturation
                    Hold = Bar.Saturation;
                    _color.S = MathHelper.Clamp(selection / (float) CellsHeight, 0f, 1f);
                }
                else if (selection >= CellsHeight + Gap || Hold == Bar.Lightness)
                {
                    // Lightness
                    Hold = Bar.Lightness;
                    _color.L = MathHelper.Clamp((selection - CellsHeight - Gap) / (float) CellsHeight, 0f, 1f);
                }
                else
                {
                    return;
                }
            }

            if (itemToDrawColored is not Chest chest)
                return;

            chest.playerChoiceColor.Value = _color.ToRgbColor();
            chest.playerChoiceColor.A = 255;
            chest.resetLidFrame();
        }

        public override void performHoverAction(int x, int y)
        {
            if (Hold != Bar.None)
                receiveLeftClick(x, y, false);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            switch (Hold)
            {
                case Bar.Hue:
                    _color.H = MathHelper.Clamp(_color.H + direction, 0, 1);
                    break;
                case Bar.Saturation:
                    _color.S = MathHelper.Clamp(_color.S + direction, 0, 1);
                    break;
                case Bar.Lightness:
                    _color.L += MathHelper.Clamp(_color.L + direction, 0, 1);
                    break;
                case Bar.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public new Color getColorFromSelection(int selection)
        {
            var color = _color.ToRgbColor();
            color.A = 255;
            return color;
        }

        public override void draw(SpriteBatch b)
        {
            if (!visible)
                return;

            // Chest
            if (itemToDrawColored is Chest chest)
                chest.draw(b, xPositionOnScreen + borderWidth / 2, yPositionOnScreen - 64, 1f, true);

            // Background
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.LightGray);

            // Transparent Square
            TransparentBox.draw(b);

            // Hue Bar
            b.Draw(HueBar,
                new Rectangle(xPositionOnScreen + borderWidth / 2, yPositionOnScreen + borderWidth / 2 + 36, 28, BarHeight),
                Color.White);

            for (var i = 0; i < Cells; i++)
            {
                // Saturation Bar
                b.Draw(Game1.staminaRect,
                    new Rectangle(
                        xPositionOnScreen + borderWidth / 2 + 36,
                        yPositionOnScreen + borderWidth / 2 + 36 + i * CellHeight,
                        22, CellHeight),
                    new HSLColor {H = _color.H, S = i / 16f, L = _color.L}.ToRgbColor());

                // Lightness Bar
                b.Draw(Game1.staminaRect,
                    new Rectangle(
                        xPositionOnScreen + borderWidth / 2 + 36,
                        yPositionOnScreen + borderWidth / 2 + 36 + i * CellHeight + CellsHeight + Gap,
                        22, CellHeight),
                    new HSLColor {H = _color.H, S = _color.S, L = i / 16f}.ToRgbColor());
            }

            // Current Selection
            if (colorSelection != 0)
            {
                //Hue
                HueSlider.bounds.Y = yPositionOnScreen + borderWidth / 2 + 36 + (int) (_color.H * BarHeight) - 2;
                b.Draw(Game1.mouseCursors,
                    new Rectangle(HueSlider.bounds.X - 8, HueSlider.bounds.Y, 20, 16),
                    new Rectangle(412, 495, 5, 4),
                    Color.White,
                    MathHelper.PiOver2,
                    new Vector2(2.5f, 4f),
                    SpriteEffects.None,
                    1);

                // Saturation
                SatSlider.bounds.Y = yPositionOnScreen + borderWidth / 2 + 36 + (int) (_color.S * CellsHeight) - 2;
                b.Draw(Game1.mouseCursors,
                    new Rectangle(SatSlider.bounds.X + 14, SatSlider.bounds.Y, 20, 16),
                    new Rectangle(412, 495, 5, 4),
                    Color.White,
                    MathHelper.PiOver2,
                    new Vector2(2.5f, 4f),
                    SpriteEffects.FlipVertically,
                    1);

                // Lightness
                LitSlider.bounds.Y = yPositionOnScreen + borderWidth / 2 + 36 + (int) (_color.L * CellsHeight) + CellsHeight + Gap - 2;
                b.Draw(Game1.mouseCursors,
                    new Rectangle(LitSlider.bounds.X + 14, LitSlider.bounds.Y, 20, 16),
                    new Rectangle(412, 495, 5, 4),
                    Color.White,
                    MathHelper.PiOver2,
                    new Vector2(2.5f, 4f),
                    SpriteEffects.FlipVertically,
                    1);
            }

            if (colorSelection == 0)
                drawTextureBox(b,
                    Game1.mouseCursors,
                    new Rectangle(375, 357, 3, 3),
                    xPositionOnScreen + borderWidth / 2 - 4,
                    yPositionOnScreen + borderWidth / 2 - 4,
                    36,
                    36,
                    Color.Black,
                    4f,
                    false);
        }

        private enum Bar
        {
            None,
            Hue,
            Saturation,
            Lightness
        }
    }
}