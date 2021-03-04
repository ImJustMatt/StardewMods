using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.UI
{
    internal class MenuView : IDisposable
    {
        private static readonly PerScreen<MenuView> Instance = new();

        /// <summary>Chest color picker.</summary>
        private readonly HSLColorPicker _colorPicker;

        /// <summary>Scrolls inventory menu down one row.</summary>
        private readonly ClickableTextureComponent _downArrow;

        private readonly ItemGrabMenu _menu;

        /// <summary>The screen ID for which the overlay was created, to support split-screen mode.</summary>
        private readonly int _screenId;

        private readonly Action<int> _scroll;
        private readonly Action<string> _search;

        /// <summary>Corresponds to the bounds of the searchField.</summary>
        private readonly ClickableComponent _searchArea;

        /// <summary>Input to filter items by name or context tags.</summary>
        private readonly TextBox _searchField;

        /// <summary>Icon to display next to search box.</summary>
        private readonly ClickableTextureComponent _searchIcon;

        private readonly Action<int> _setTab;

        /// <summary>Chest menu tab components.</summary>
        private readonly IList<ClickableTextureComponent> _tabs = new List<ClickableTextureComponent>();

        /// <summary>Scrolls inventory menu up one row.</summary>
        private readonly ClickableTextureComponent _upArrow;

        /// <summary>The number of draw cycles since the menu was initialized.</summary>
        private int _drawCount;

        /// <summary>Draw hoverText over chest menu.</summary>
        private string _hoverText;

        /// <summary>The last viewport bounds.</summary>
        private Rectangle _lastViewport;

        /// <summary>Y-Position for tabs when not selected.</summary>
        private int _tabY;

        /// <summary>Currently selected tab.</summary>
        internal int CurrentTab;

        protected internal MenuView(
            ItemGrabMenu menu,
            Options options,
            Action<int> scroll,
            Action<int> setTab,
            Action<string> search)
        {
            Instance.Value = this;
            _screenId = Context.ScreenId;

            _menu = menu;
            _scroll = scroll;
            _setTab = setTab;
            _search = search;

            var bounds = new Rectangle(_menu.xPositionOnScreen, _menu.yPositionOnScreen, _menu.width, _menu.height);
            var upperBounds = new Rectangle(
                _menu.ItemsToGrabMenu.xPositionOnScreen,
                _menu.ItemsToGrabMenu.yPositionOnScreen,
                _menu.ItemsToGrabMenu.width,
                _menu.ItemsToGrabMenu.height);
            _tabY = upperBounds.Bottom + 1 * Game1.pixelZoom;

            _upArrow = new ClickableTextureComponent(
                new Rectangle(upperBounds.Right + 8, upperBounds.Y - 40, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                Game1.pixelZoom);

            _downArrow = new ClickableTextureComponent(
                new Rectangle(_upArrow.bounds.X, upperBounds.Bottom - 36, _upArrow.bounds.Width, _upArrow.bounds.Height),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                Game1.pixelZoom);

            if (options.ShowColor)
            {
                _colorPicker = new HSLColorPicker(
                    bounds.Right + 96,
                    bounds.Top - 56,
                    options.Chest);
                _colorPicker.colorSelection = _colorPicker.getSelectionFromColor(options.Chest.playerChoiceColor.Value);
                _menu.chestColorPicker = _colorPicker;
            }

            if (options.ShowSearch)
            {
                _searchField = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
                {
                    X = upperBounds.X,
                    Y = upperBounds.Y - 14 * Game1.pixelZoom,
                    Width = upperBounds.Width,
                    Selected = false,
                    Text = options.Text
                };

                _searchArea = new ClickableComponent(new Rectangle(_searchField.X, _searchField.Y, _searchField.Width, _searchField.Height), "");

                _searchIcon = new ClickableTextureComponent(
                    new Rectangle(upperBounds.Right - 38, upperBounds.Y - 14 * Game1.pixelZoom + 6, 32, 32),
                    Game1.mouseCursors,
                    new Rectangle(80, 0, 13, 13),
                    2.5f);
            }

            CurrentTab = -1;
        }

        internal Color? CurrentColor => _colorPicker?.getCurrentColor();

        /// <summary>Returns whether the menu and its components have been initialized.</summary>
        private bool IsInitialized => _drawCount > 1;

        /// <summary>Returns Displayed Rows of MenuWithInventory.</summary>
        internal static HSLColorPicker ColorPicker =>
            Instance.Value == null || Instance.Value._screenId != Context.ScreenId
                ? null
                : Instance.Value._colorPicker;

        internal static string SearchText =>
            Instance.Value == null || Instance.Value._screenId != Context.ScreenId
                ? null
                : Instance.Value._searchField?.Text;

        /// <summary>Unregister Event Handling</summary>
        public void Dispose()
        {
            Instance.Value = null;
        }

        public void AddTab(string tabName, Texture2D texture)
        {
            var lastTab = _tabs.LastOrDefault();
            var i = _tabs.Count;
            var x = lastTab?.bounds.Right ?? _menu.ItemsToGrabMenu.xPositionOnScreen;
            var tab = new ClickableTextureComponent(
                new Rectangle(x, _tabY, 16 * Game1.pixelZoom, 16 * Game1.pixelZoom),
                texture,
                Rectangle.Empty,
                Game1.pixelZoom)
            {
                name = i.ToString(),
                hoverText = tabName
            };
            _tabs.Add(tab);
        }

        internal void ToggleArrows(bool upArrow, bool downArrow)
        {
            if (_upArrow != null)
                _upArrow.visible = upArrow;
            if (_downArrow != null)
                _downArrow.visible = downArrow;
        }

        /// <summary></summary>
        private void InitComponents()
        {
            var upperBounds = new Rectangle(
                _menu.ItemsToGrabMenu.xPositionOnScreen,
                _menu.ItemsToGrabMenu.yPositionOnScreen,
                _menu.ItemsToGrabMenu.width,
                _menu.ItemsToGrabMenu.height);

            if (_upArrow != null)
            {
                _upArrow.bounds.X = upperBounds.Right + 8;
                _upArrow.bounds.Y = upperBounds.Y - 40;
            }

            if (_downArrow != null)
            {
                _downArrow.bounds.X = upperBounds.Right + 8;
                _downArrow.bounds.Y = upperBounds.Bottom - 36;
            }

            if (_searchField != null)
            {
                _searchField.X = upperBounds.X;
                _searchField.Y = upperBounds.Y - 14 * Game1.pixelZoom;
                _searchArea.bounds.X = _searchField.X;
                _searchArea.bounds.Y = _searchField.Y;
                _searchIcon.bounds.X = upperBounds.Right - 38;
                _searchIcon.bounds.Y = upperBounds.Y - 14 * Game1.pixelZoom + 6;
            }

            _tabY = upperBounds.Bottom + 1 * Game1.pixelZoom;
            var xPosition = upperBounds.Left;
            foreach (var tab in _tabs)
            {
                tab.bounds.X = xPosition;
                tab.bounds.Y = _tabY;
                xPosition += tab.bounds.Width;
            }

            _lastViewport = new Rectangle(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height);
        }

        /// <summary>Draws overlay to screen (above menu but below tooltips/hover items)</summary>
        /// <param name="b">The SpriteBatch to draw to</param>
        public static void DrawOverlay(SpriteBatch b)
        {
            if (Instance.Value == null || Instance.Value._screenId != Context.ScreenId)
                return;

            if (Instance.Value._drawCount == 0
                || Game1.uiViewport.Width != Instance.Value._lastViewport.Width
                || Game1.uiViewport.Height != Instance.Value._lastViewport.Height)
                Instance.Value.InitComponents();

            Instance.Value._drawCount++;

            Instance.Value._upArrow?.draw(b);
            Instance.Value._downArrow?.draw(b);
            Instance.Value._searchField?.Draw(b, false);
            Instance.Value._searchIcon?.draw(b);

            if (Instance.Value._hoverText != null)
                IClickableMenu.drawHoverText(b, Instance.Value._hoverText, Game1.smallFont);
        }

        /// <summary>Draws underlay to screen (below menu)</summary>
        /// <param name="b">The SpriteBatch to draw to</param>
        public static void DrawUnderlay(SpriteBatch b)
        {
            if (Instance.Value == null || Instance.Value._screenId != Context.ScreenId)
                return;

            if (Instance.Value._drawCount == 0
                || Game1.uiViewport.Width != Instance.Value._lastViewport.Width
                || Game1.uiViewport.Height != Instance.Value._lastViewport.Height)
                Instance.Value.InitComponents();

            Instance.Value._drawCount++;

            for (var i = 0; i < Instance.Value._tabs.Count; i++)
            {
                Instance.Value._tabs[i].bounds.Y = Instance.Value._tabY + (Instance.Value.CurrentTab == i ? 1 * Game1.pixelZoom : 0);
                Instance.Value._tabs[i].draw(b,
                    Instance.Value.CurrentTab == i ? Color.White : Color.Gray,
                    0.86f + Instance.Value._tabs[i].bounds.Y / 20000f);
            }
        }

        /// <summary>Suppress input when search field is selected.</summary>
        /// <param name="button">The button that was pressed</param>
        /// <returns>True when an interaction occurs</returns>
        internal bool ReceiveKeyPress(SButton button)
        {
            if (!IsInitialized || Context.ScreenId != _screenId)
                return false;

            if (button != SButton.Escape)
                return _searchField != null && _searchField.Selected;

            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = null;
            return true;
        }

        /// <summary>Handles Left-Click interaction with overlay elements</summary>
        /// <param name="x">x-coordinate of left-click</param>
        /// <param name="y">Y-Coordinate of left-click</param>
        /// <param name="released">True when left click is released</param>
        /// /// <param name="playSound">Whether sound should be enabled for click</param>
        /// <returns>True when an interaction occurs</returns>
        internal bool LeftClick(int x, int y, bool released = false, bool playSound = true)
        {
            if (!IsInitialized || Context.ScreenId != _screenId)
                return false;

            if (released)
            {
                _colorPicker?.ReleaseBar();
                return false;
            }

            if (_searchField != null)
            {
                _searchField.Selected = _searchArea.containsPoint(x, y);
                if (_searchField.Selected)
                    return true;
            }

            if (_upArrow != null && _upArrow.containsPoint(x, y))
            {
                _scroll.Invoke(1);
                if (playSound)
                    Game1.playSound("shwip");
                return true;
            }

            if (_downArrow != null && _downArrow.containsPoint(x, y))
            {
                _scroll.Invoke(-1);
                if (playSound)
                    Game1.playSound("shwip");
                return true;
            }

            var tab = _tabs.FirstOrDefault(t => t.containsPoint(x, y));
            if (tab == null)
                return false;

            var i = Convert.ToInt32(tab.name);
            CurrentTab = CurrentTab == i ? -1 : i;
            _setTab.Invoke(CurrentTab);
            if (playSound)
                Game1.playSound("smallSelect");
            return true;
        }

        /// <summary>Handles Right-Click interaction with overlay elements</summary>
        /// <param name="x">x-coordinate of left-click</param>
        /// <param name="y">Y-Coordinate of left-click</param>
        /// <param name="released">True when left click is released</param>
        /// <param name="playSound">Whether sound should be enabled for click</param>
        /// <returns>True when an interaction occurs</returns>
        // ReSharper disable once UnusedParameter.Global
        internal bool RightClick(int x, int y, bool released = false, bool playSound = true)
        {
            if (!IsInitialized || Context.ScreenId != _screenId)
                return false;

            if (released)
                return false;

            if (_searchField == null)
                return false;

            _searchField.Selected = _searchArea.containsPoint(x, y);
            if (!_searchField.Selected)
                return false;

            _searchField.Text = "";
            _search.Invoke(_searchField.Text);
            return true;
        }

        /// <summary>Handles Hover interaction with overlay elements</summary>
        /// <param name="x">x-coordinate of mouse</param>
        /// <param name="y">Y-Coordinate of mouse</param>
        internal void Hover(int x, int y)
        {
            if (!IsInitialized || Context.ScreenId != _screenId)
                return;

            _upArrow?.tryHover(x, y, 0.25f);
            _downArrow?.tryHover(x, y, 0.25f);
            _searchField?.Hover(x, y);
            _colorPicker?.performHoverAction(x, y);

            var tab = _tabs.FirstOrDefault(t => t.containsPoint(x, y));
            _hoverText = tab?.hoverText;
        }

        internal class Options
        {
            internal Chest Chest;
            internal bool ShowColor;
            internal bool ShowSearch;
            internal string Text;

            public Options(Chest chest = null)
            {
                ShowSearch = false;
                ShowColor = false;
                Chest = chest;
            }
        }
    }
}