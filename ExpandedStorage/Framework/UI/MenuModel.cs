using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Netcode;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.ExpandedStorage.Framework.UI
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class MenuModel : IDisposable
    {
        private static readonly PerScreen<MenuModel> Instance = new();
        private static ModConfig _config;

        /// <summary>The object that the inventory menu is associated with</summary>
        internal readonly object Context;

        /// <summary>The inventory items that the inventory menu is associated with</summary>
        internal readonly IList<Item> Items;

        /// <summary>The number of rows for the inventory menu</summary>
        internal readonly int MenuRows;

        /// <summary>Expanded Storage Config data for Menu</summary>
        internal readonly Storage Storage;

        /// <summary>Expanded Storage Tab data for Menu</summary>
        internal readonly IList<StorageTab> StorageTabs;

        private int _currentTab;
        private string _searchText;
        private int _skippedRows;

        /// <summary>Track which menu is being handled and refresh if it changes</summary>
        internal ItemGrabMenu Menu;

        private MenuModel(ItemGrabMenu menu)
        {
            Instance.Value = this;

            Menu = menu;
            Context = menu.context;
            MenuRows = Menu.ItemsToGrabMenu.rows;
            ExpandedStorage.TryGetStorage(Context, out Storage);
            Items = menu.ItemsToGrabMenu.actualInventory;
            FilteredItems = Items;
            MaxRows = Math.Max(0, Items.Count.RoundUp(12) / 12 - MenuRows);

            _currentTab = -1;
            _skippedRows = 0;
            _searchText = "";

            if (Storage?.Tabs.Count > 0)
            {
                StorageTabs = Storage.Tabs
                    .Select(t => ExpandedStorage.GetTab(Storage.ModUniqueId, t))
                    .Where(t => t != null)
                    .ToList();
            }
            else if (Storage?.Config.Option("ShowTabs", true) == StorageConfig.Choice.Enable && StorageConfig.DefaultTabs?.Count > 0)
            {
                StorageTabs = StorageConfig.DefaultTabs
                    .Select(t => ExpandedStorage.GetTab(Storage.ModUniqueId, t))
                    .Where(t => t != null)
                    .ToList();
            }

            RegisterEvents();
        }

        /// <summary>Displayed inventory items after filter and scroll</summary>
        internal IList<Item> FilteredItems { get; set; }

        /// <summary>The text entered in the search bar of the current menu</summary>
        internal string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;
                _searchText = value;
                InvokeItemChanged();
            }
        }

        /// <summary>The number of skipped rows in the current menu</summary>
        internal int SkippedRows
        {
            get => _skippedRows;
            set
            {
                if (_skippedRows == value)
                    return;
                _skippedRows = value;
                InvokeItemChanged();
            }
        }

        internal int CurrentTab
        {
            get => _currentTab;
            set
            {
                if (_currentTab == value || StorageTabs == null)
                    return;
                _currentTab = value switch
                {
                    -2 => StorageTabs.Count - 1,
                    _ when value == StorageTabs.Count => -1,
                    _ => value
                };
                _skippedRows = 0;
                InvokeItemChanged();
            }
        }

        /// <summary>The maximum number of rows that can be skipped</summary>
        internal int MaxRows { get; set; }

        public void Dispose()
        {
            switch (Context)
            {
                case Object obj when obj.heldObject.Value is Chest chest:
                    chest.items.OnElementChanged -= ItemsOnElementChanged;
                    break;
                case Chest chest:
                    chest.items.OnElementChanged -= ItemsOnElementChanged;
                    break;
                case GameLocation location:
                    var farm = location as Farm ?? Game1.getFarm();
                    var shippingBin = farm.getShippingBin(Game1.player);
                    shippingBin.OnValueAdded -= ShippingBinOnValueChanged;
                    shippingBin.OnValueRemoved -= ShippingBinOnValueChanged;
                    break;
                case JunimoHut junimoHut:
                    junimoHut.output.Value.items.OnElementChanged -= ItemsOnElementChanged;
                    break;
            }
        }

        internal event EventHandler ItemChanged;

        /// <summary>Returns Offset to lower menu for expanded menus.</summary>
        public static int GetOffset(MenuWithInventory menu)
        {
            return _config.ExpandInventoryMenu
                   && menu is ItemGrabMenu {shippingBin: false} igm
                   && ExpandedStorage.TryGetStorage(igm.context, out var storage)
                ? storage.Config.Menu.Offset
                : 0;
        }

        /// <summary>Returns Padding to top menu for search box.</summary>
        public static int GetPadding(MenuWithInventory menu)
        {
            return menu is ItemGrabMenu {shippingBin: false} igm && ExpandedStorage.TryGetStorage(igm.context, out var storage) ? storage.Config.Menu.Padding : 0;
        }

        /// <summary>Returns Display Capacity of MenuWithInventory.</summary>
        public static int GetMenuCapacity(object context)
        {
            return _config.ExpandInventoryMenu && ExpandedStorage.TryGetStorage(context, out var storage) ? storage.Config.Menu.Capacity : 36;
        }

        /// <summary>Returns Displayed Rows of MenuWithInventory.</summary>
        public static int GetMenuRows(object context)
        {
            return _config.ExpandInventoryMenu && ExpandedStorage.TryGetStorage(context, out var storage) ? storage.Config.Menu.Rows : 3;
        }

        /// <summary>Returns the filtered list of items in the InventoryMenu.</summary>
        public static IList<Item> GetItems(IList<Item> items)
        {
            return Instance.Value != null && ReferenceEquals(Instance.Value.Items, items) ? Instance.Value.FilteredItems : items;
        }

        internal static void Init(ModConfig config)
        {
            _config = config;
        }

        protected internal static MenuModel Get(ItemGrabMenu menu)
        {
            if (Instance.Value == null)
                return new MenuModel(menu);

            if (Instance.Value != null && !Instance.Value.ContextMatches(menu))
            {
                Instance.Value.Dispose();
                return new MenuModel(menu);
            }

            if (Game1.options.SnappyMenus)
            {
                var oldId = Instance.Value.Menu.currentlySnappedComponent.myID;
                if (oldId != -1)
                    menu.currentlySnappedComponent = menu.getComponentWithID(oldId);
                menu.snapCursorToCurrentSnappedComponent();
            }

            Instance.Value.Menu = menu;
            return Instance.Value;
        }

        private void RegisterEvents()
        {
            switch (Context)
            {
                case Object obj when obj.heldObject.Value is Chest chest:
                    chest.items.OnElementChanged += ItemsOnElementChanged;
                    break;
                case Chest chest:
                    chest.items.OnElementChanged += ItemsOnElementChanged;
                    break;
                case JunimoHut junimoHut:
                    junimoHut.output.Value.items.OnElementChanged += ItemsOnElementChanged;
                    break;
                case GameLocation location:
                    var farm = location as Farm ?? Game1.getFarm();
                    var shippingBin = farm.getShippingBin(Game1.player);
                    shippingBin.OnValueAdded += ShippingBinOnValueChanged;
                    shippingBin.OnValueRemoved += ShippingBinOnValueChanged;
                    break;
            }
        }

        private bool ContextMatches(ItemGrabMenu menu)
        {
            return menu.context != null
                   && ReferenceEquals(menu.context, Context)
                   || ReferenceEquals(menu.ItemsToGrabMenu.actualInventory, Items);
        }

        private void ItemsOnElementChanged(NetList<Item, NetRef<Item>> list, int index, Item oldValue, Item newValue)
        {
            InvokeItemChanged();
        }

        private void ShippingBinOnValueChanged(Item value)
        {
            InvokeItemChanged();
        }

        private void InvokeItemChanged()
        {
            if (ItemChanged == null)
                return;
            foreach (var @delegate in ItemChanged.GetInvocationList()) @delegate.DynamicInvoke(this, null);
        }
    }
}