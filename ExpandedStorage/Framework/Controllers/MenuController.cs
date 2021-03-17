using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal class MenuController : IDisposable
    {
        private static readonly PerScreen<MenuController> Instance = new();

        private static IModEvents _events;
        private static IInputHelper _inputHelper;
        private static ModConfig _config;

        private readonly MenuModel _model;

        /// <summary>The screen ID for which the overlay was created, to support split-screen mode.</summary>
        private readonly int _screenId;

        private readonly MenuView _view;

        private MenuController(ItemGrabMenu menu)
        {
            _screenId = Context.ScreenId;
            _model = MenuModel.Get(menu);

            if (_model.Storage == null)
                return;

            Chest chest = null;
            if (_model.Context is Chest sourceItem)
            {
                chest = new Chest(true, sourceItem.ParentSheetIndex);
                chest.playerChoiceColor.Value = sourceItem.playerChoiceColor.Value;
            }

            _view = new MenuView(menu,
                new MenuView.Options
                {
                    ShowSearch = _model.Storage.Config.Option("ShowSearchBar", true) == StorageConfigController.Choice.Enable,
                    ShowColor = chest != null && _config.ColorPicker && _model.Storage.PlayerColor && _model.Storage.Config.Option("ShowColorPicker", true) == StorageConfigController.Choice.Enable,
                    Chest = chest,
                    Text = _model.SearchText
                },
                Scroll,
                SetTab,
                SetSearch);

            // Events
            _model.ItemChanged += OnItemChanged;
            _events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
            _events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            _events.Input.ButtonsChanged += OnButtonsChanged;
            _events.Input.ButtonPressed += OnButtonPressed;
            _events.Input.ButtonReleased += OnButtonReleased;
            _events.Input.CursorMoved += OnCursorMoved;
            _events.Input.MouseWheelScrolled += OnMouseWheelScrolled;

            if (_model.Storage.Config.Option("ShowTabs", true) == StorageConfigController.Choice.Enable && _model.StorageTabs.Any())
            {
                foreach (var tab in _model.StorageTabs) _view.AddTab(tab.TabName, Game1.content.Load<Texture2D>(tab.Path));
                _view.CurrentTab = _model.CurrentTab;
            }

            OnItemChanged(this, null);
        }

        public void Dispose()
        {
            Instance.Value = null;
            _events.Display.RenderingActiveMenu -= OnRenderingActiveMenu;
            _events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
            _events.Input.ButtonsChanged -= OnButtonsChanged;
            _events.Input.ButtonPressed -= OnButtonPressed;
            _events.Input.ButtonReleased -= OnButtonReleased;
            _events.Input.CursorMoved -= OnCursorMoved;
            _events.Input.MouseWheelScrolled -= OnMouseWheelScrolled;
            _model.ItemChanged -= OnItemChanged;
            _model?.Dispose();
            _view?.Dispose();
        }

        public static void RefreshItems()
        {
            if (Instance.Value == null || Context.ScreenId != Instance.Value._screenId)
                return;
            Instance.Value.OnItemChanged(Instance.Value, null);
        }

        internal static void Init(IModEvents events, IInputHelper inputHelper, ModConfig config)
        {
            _events = events;
            _inputHelper = inputHelper;
            _config = config;

            // Events
            _events.GameLoop.UpdateTicked += OnUpdateTicked;
            _events.Display.MenuChanged += OnMenuChanged;
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Instance.Value == null)
                return;
            if (!Context.HasScreenId(Instance.Value._screenId) || Game1.activeClickableMenu is not ItemGrabMenu)
                Instance.Value.Dispose();
            else
                Instance.Value._model.SearchText = MenuView.SearchText;
        }

        /// <summary>
        ///     Resets scrolling/overlay when chest menu exits or context changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            Instance.Value?.Dispose();
            if (e.NewMenu is ItemGrabMenu {shippingBin: false} menu)
                Instance.Value = new MenuController(menu);
        }

        /// <summary>Attempts to scroll offset by one row of slots relative to the inventory menu.</summary>
        /// <param name="direction">The direction which to scroll to.</param>
        private void Scroll(int direction)
        {
            switch (direction)
            {
                case > 0 when _model.SkippedRows > 0:
                    _model.SkippedRows--;
                    break;
                case < 0 when _model.SkippedRows < _model.MaxRows:
                    _model.SkippedRows++;
                    break;
            }
        }

        /// <summary>Sets the current tab by index.</summary>
        private void SetTab(int index)
        {
            _model.CurrentTab = index;
        }

        /// <summary>Switch to previous tab.</summary>
        private void PreviousTab()
        {
            _model.CurrentTab--;
        }

        /// <summary>Switch to next tab.</summary>
        private void NextTab()
        {
            _model.CurrentTab++;
        }

        /// <summary>Filter items by search text.</summary>
        private void SetSearch(string searchText)
        {
            _model.SearchText = searchText;
        }

        /// <summary>When a menu is open, raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        private void OnRenderingActiveMenu(object sender, RenderingActiveMenuEventArgs e)
        {
            _view?.DrawUnderlay(e.SpriteBatch);
        }

        /// <summary>When a menu is open, raised before that menu is drawn to the screen.</summary>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            _view?.DrawOverlay(e.SpriteBatch);
        }

        /// <summary>Track if configured control buttons are pressed or pass input to overlay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (_view == null || Context.ScreenId != _screenId)
                return;

            if (_config.Controls.ScrollDown.JustPressed())
            {
                Scroll(-1);
                _inputHelper.SuppressActiveKeybinds(_config.Controls.ScrollDown);
            }
            else if (_config.Controls.ScrollUp.JustPressed())
            {
                Scroll(1);
                _inputHelper.SuppressActiveKeybinds(_config.Controls.ScrollUp);
            }

            if (_model.Storage?.Tabs == null)
                return;

            if (_config.Controls.PreviousTab.JustPressed())
            {
                PreviousTab();
                _view.CurrentTab = _model.CurrentTab;
                _inputHelper.SuppressActiveKeybinds(_config.Controls.PreviousTab);
            }
            else if (_config.Controls.NextTab.JustPressed())
            {
                NextTab();
                _view.CurrentTab = _model.CurrentTab;
                _inputHelper.SuppressActiveKeybinds(_config.Controls.NextTab);
            }
        }

        /// <summary>Track if configured control buttons are pressed or pass input to overlay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (_view == null || Context.ScreenId != _screenId)
                return;

            var x = Game1.getMouseX(true);
            var y = Game1.getMouseY(true);

            if ((e.Button == SButton.MouseLeft || e.Button.IsUseToolButton()) && _view.LeftClick(x, y))
                _inputHelper.Suppress(e.Button);
            else if ((e.Button == SButton.MouseRight || e.Button.IsActionButton()) && _view.RightClick(x, y))
                _inputHelper.Suppress(e.Button);
            else if (_view.ReceiveKeyPress(e.Button))
                _inputHelper.Suppress(e.Button);
        }

        /// <summary>Track if configured control buttons are pressed or pass input to overlay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (_view == null || Context.ScreenId != _screenId)
                return;

            var x = Game1.getMouseX(true);
            var y = Game1.getMouseY(true);

            if ((e.Button == SButton.MouseLeft || e.Button.IsUseToolButton()) && _view.LeftClick(x, y, true))
                _inputHelper.Suppress(e.Button);
            else if ((e.Button == SButton.MouseRight || e.Button.IsActionButton()) && _view.RightClick(x, y, true))
                _inputHelper.Suppress(e.Button);

            if (_model.Context is Chest chest)
            {
                chest.playerChoiceColor.Value = _view.CurrentColor ?? chest.playerChoiceColor.Value;
            }
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (_view == null || Context.ScreenId != _screenId)
                return;

            var x = Game1.getMouseX(true);
            var y = Game1.getMouseY(true);
            _view.Hover(x, y);
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (_view == null || Context.ScreenId != _screenId)
                return;

            Scroll(e.Delta);

            var cur = Game1.oldMouseState;
            Game1.oldMouseState = new MouseState(
                cur.X,
                cur.Y,
                e.NewValue,
                cur.LeftButton,
                cur.MiddleButton,
                cur.RightButton,
                cur.XButton1,
                cur.XButton2
            );
        }

        /// <summary>Sync UI state to currently filtered view.</summary>
        /// <param name="sender">The even sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnItemChanged(object sender, EventArgs e)
        {
            var items = _model.Items.Where(item => item != null);

            if (_model.CurrentTab != -1 && _model.StorageTabs != null)
            {
                var currentTab = _model.StorageTabs.ElementAtOrDefault(_model.CurrentTab);
                if (currentTab != null)
                    items = items.Where(currentTab.Filter);
            }

            if (!string.IsNullOrWhiteSpace(_model.SearchText)) items = items.Where(SearchMatches);

            var list = items.ToList();
            _model.MaxRows = Math.Max(0, list.Count.RoundUp(12) / 12 - _model.MenuRows);
            _model.SkippedRows = (int) MathHelper.Clamp(_model.SkippedRows, 0, _model.MaxRows);
            _model.FilteredItems = list
                .Skip(_model.SkippedRows * 12)
                .Take(_model.MenuRows * 12 + 12)
                .ToList();

            // Update Inventory Menu to correct item slot
            for (var i = 0; i < _model.Menu.ItemsToGrabMenu.inventory.Count; i++)
            {
                var item = _model.FilteredItems.ElementAtOrDefault(i);
                _model.Menu.ItemsToGrabMenu.inventory[i].name = item != null
                    ? _model.Items.IndexOf(item).ToString()
                    : _model.Items.Count.ToString();
            }

            // Show/hide arrows
            _view?.ToggleArrows(_model.SkippedRows > 0, _model.SkippedRows < _model.MaxRows);
        }

        private bool SearchMatches(Item item)
        {
            var searchParts = _model.SearchText.Split(' ');
            foreach (var searchPart in searchParts)
            {
                var matchCondition = !searchPart.StartsWith("!");
                var searchPhrase = matchCondition ? searchPart : searchPart.Substring(1);
                if (string.IsNullOrWhiteSpace(searchPhrase))
                    return true;
                if (searchPhrase.StartsWith(_config.SearchTagSymbol))
                {
                    if (item.MatchesTagExt(searchPhrase.Substring(1), false) != matchCondition)
                        return false;
                }
                else if ((item.Name.IndexOf(searchPhrase, StringComparison.InvariantCultureIgnoreCase) == -1 &&
                          item.DisplayName.IndexOf(searchPhrase, StringComparison.InvariantCultureIgnoreCase) == -1) == matchCondition)
                {
                    return false;
                }
            }

            return true;
        }
    }
}