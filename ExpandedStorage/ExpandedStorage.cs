using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using ImJustMatt.ExpandedStorage.Framework.UI;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

// ReSharper disable ClassNeverInstantiated.Global

namespace ImJustMatt.ExpandedStorage
{
    public class ExpandedStorage : Mod
    {
        /// <summary>Tracks previously held chest before placing into world.</summary>
        internal static readonly PerScreen<Chest> HeldChest = new();

        /// <summary>Tracks all chests that may be used for vacuum items.</summary>
        internal static readonly PerScreen<IDictionary<Chest, Storage>> VacuumChests = new();

        /// <summary>Dictionary of Expanded Storage configs</summary>
        private static readonly IDictionary<string, Storage> Storages = new Dictionary<string, Storage>();

        /// <summary>Dictionary of Expanded Storage tabs</summary>
        private static readonly IDictionary<string, StorageTab> StorageTabs = new Dictionary<string, StorageTab>();

        /// <summary>The mod configuration.</summary>
        private ModConfig _config;

        /// <summary>Handled content loaded by Expanded Storage.</summary>
        private ContentLoader _contentLoader;

        /// <summary>Expanded Storage API.</summary>
        private ExpandedStorageAPI _expandedStorageAPI;

        private JsonAssetsIntegration _jsonAssets;
        private GenericModConfigMenuIntegration _modConfigMenu;

        /// <summary>Returns Storage by object context.</summary>
        internal static bool TryGetStorage(object context, out Storage storage)
        {
            storage = Storages
                .Select(c => c.Value)
                .FirstOrDefault(c => c.MatchesContext(context));
            return storage != null;
        }

        /// <summary>Returns ExpandedStorageTab by tab name.</summary>
        internal static StorageTab GetTab(string modUniqueId, string tabName)
        {
            return StorageTabs
                .Where(t => t.Key.EndsWith($"/{tabName}"))
                .Select(t => t.Value)
                .OrderByDescending(t => t.ModUniqueId.Equals(modUniqueId))
                .ThenByDescending(t => t.ModUniqueId.Equals("furyx639.ExpandedStorage"))
                .FirstOrDefault();
        }

        public override object GetApi()
        {
            return _expandedStorageAPI;
        }

        public override void Entry(IModHelper helper)
        {
            _jsonAssets = new JsonAssetsIntegration(helper.ModRegistry);
            _modConfigMenu = new GenericModConfigMenuIntegration(helper.ModRegistry);

            _config = helper.ReadConfig<ModConfig>();
            _config.DefaultStorage.SetAsDefault();
            Monitor.Log($"Mod Config:\n{ModConfig.ConfigHelper.Summary(_config)}\n{ModConfigKeys.ConfigHelper.Summary(_config.Controls, false)}", LogLevel.Debug);

            _expandedStorageAPI = new ExpandedStorageAPI(Helper, Monitor, Storages, StorageTabs, _modConfigMenu, _jsonAssets);
            _contentLoader = new ContentLoader(Helper, ModManifest, Monitor, _config, Storages, StorageTabs, _expandedStorageAPI, _jsonAssets);
            helper.Content.AssetLoaders.Add(_contentLoader);
            helper.Content.AssetEditors.Add(_contentLoader);

            MenuViewModel.Init(helper.Events, helper.Input, _config);
            MenuModel.Init(_config);
            HSLColorPicker.Init(helper.Content);
            ChestExtensions.Init(helper.Reflection);
            Storage.Init(helper.Events);

            // Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;

            if (helper.ModRegistry.IsLoaded("spacechase0.CarryChest"))
            {
                Monitor.Log("Do not run Expanded with Carry Chest!", LogLevel.Warn);
            }
            else
            {
                helper.Events.Input.ButtonPressed += OnButtonPressed;
                helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            }

            // Harmony Patches
            new Patcher<ModConfig>(ModManifest.UniqueID).ApplyAll(
                new ItemPatch(Monitor, _config),
                new ObjectPatch(Monitor, _config),
                new FarmerPatch(Monitor, _config),
                new ChestPatch(Monitor, helper.Reflection, _config),
                new ItemGrabMenuPatch(Monitor, _config, helper.Reflection),
                new InventoryMenuPatch(Monitor, _config),
                new MenuWithInventoryPatch(Monitor, _config),
                new DiscreteColorPickerPatch(Monitor, _config),
                new DebrisPatch(Monitor, _config),
                new UtilityPatch(Monitor, _config),
                new AutomatePatch(Monitor, _config, helper.Reflection, helper.ModRegistry.IsLoaded("Pathoschild.Automate")),
                new ChestsAnywherePatch(Monitor, _config, helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere"))
            );
        }

        /// <summary>Setup Generic Mod Config Menu</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (!_modConfigMenu.IsLoaded)
                return;

            void DefaultConfig()
            {
                _config = Helper.ReadConfig<ModConfig>();
                _config.DefaultStorage.SetAsDefault();
            }

            void SaveConfig()
            {
                Helper.WriteConfig(_config);
            }

            _modConfigMenu.API.RegisterModConfig(ModManifest, DefaultConfig, SaveConfig);
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Controls", "Controller/Keyboard controls", "Controls");
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Tweaks", "Modify behavior for certain features", "Tweaks");
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Default Storage", "Global default storage config", "Default Storage");

            _modConfigMenu.API.StartNewPage(ModManifest, "Controls");
            _modConfigMenu.API.RegisterLabel(ModManifest, "Controls", "Controller/Keyboard controls");
            _modConfigMenu.RegisterConfigOptions(ModManifest, ModConfigKeys.ConfigHelper, _config.Controls);
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");

            _modConfigMenu.API.StartNewPage(ModManifest, "Tweaks");
            _modConfigMenu.API.RegisterLabel(ModManifest, "Tweaks", "Modify behavior for certain features");
            _modConfigMenu.RegisterConfigOptions(ModManifest, ModConfig.ConfigHelper, _config);
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");

            _modConfigMenu.API.StartNewPage(ModManifest, "Default Storage");
            _modConfigMenu.API.RegisterLabel(ModManifest, "Default Storage", "Global default storage config");
            _modConfigMenu.RegisterConfigOptions(ModManifest, StorageConfig.ConfigHelper, _config.DefaultStorage);
            _modConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");
        }

        /// <summary>Track toolbar changes before user input.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            Helper.Events.World.ObjectListChanged -= OnObjectListChanged;
            foreach (var removed in e.Removed)
            {
                var x = removed.Value.modData.TryGetValue("furyx639.ExpandedStorage/X", out var xStr) ? int.Parse(xStr) : 0;
                var y = removed.Value.modData.TryGetValue("furyx639.ExpandedStorage/Y", out var yStr) ? int.Parse(yStr) : 0;
                if (!TryGetStorage(removed.Value, out var storage)
                    || x == 0 && y == 0
                    || storage.SpriteSheet is not { } spriteSheet
                    || spriteSheet.TileWidth <= 1 && spriteSheet.TileHeight <= 1) continue;
                spriteSheet.ForEachPos(x, y, pos =>
                {
                    if (!pos.Equals(removed.Key) && e.Location.Objects.ContainsKey(pos)) e.Location.Objects.Remove(pos);
                });
            }

            foreach (var added in e.Added)
            {
                if (added.Value is not Chest chest || !TryGetStorage(chest, out var storage)) continue;
                chest.modData["furyx639.ExpandedStorage/X"] = added.Key.X.ToString(CultureInfo.InvariantCulture);
                chest.modData["furyx639.ExpandedStorage/Y"] = added.Key.Y.ToString(CultureInfo.InvariantCulture);

                // Add objects for extra Tile spaces
                if (storage.SpriteSheet is { } spriteSheet && (spriteSheet.TileWidth > 1 || spriteSheet.TileHeight > 1))
                {
                    spriteSheet.ForEachPos((int) added.Key.X, (int) added.Key.Y, pos =>
                    {
                        if (!pos.Equals(added.Key) && !e.Location.Objects.ContainsKey(pos)) e.Location.Objects.Add(pos, chest.ToObject(storage));
                    });
                }
            }

            Helper.Events.World.ObjectListChanged += OnObjectListChanged;
        }

        /// <summary>Initialize player item vacuum chests.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;
            RefreshVacuumChests(Game1.player);
        }

        /// <summary>Refresh player item vacuum chests.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;
            RefreshVacuumChests(e.Player);
        }

        private void RefreshVacuumChests(Farmer who)
        {
            VacuumChests.Value = who.Items
                .Take(_config.VacuumToFirstRow ? 12 : who.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, i => TryGetStorage(i, out var storage) ? storage : null)
                .Where(s => s.Value?.Config.Option("VacuumItems", true) == StorageConfig.Choice.Enable)
                .ToDictionary(s => s.Key, s => s.Value);
            Monitor.VerboseLog($"Found {VacuumChests.Value.Count} For Vacuum:\n" + string.Join("\n", VacuumChests.Value.Select(s => $"\t{s.Key}")));
        }

        /// <summary>Track toolbar changes before user input.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            Storage storage = null;
            if (Game1.player.CurrentItem is Chest activeChest && TryGetStorage(activeChest, out storage))
            {
                if (!ReferenceEquals(HeldChest.Value, activeChest))
                {
                    HeldChest.Value = activeChest;
                    activeChest.fixLidFrame();
                }
            }
            else if (storage == null)
            {
                HeldChest.Value = null;
            }

            foreach (var chest in Game1.player.Items.OfType<Chest>())
            {
                chest.updateWhenCurrentLocation(Game1.currentGameTime, Game1.player.currentLocation);
            }
        }

        /// <summary>Raised after the player pressed/released a keyboard, mouse, or controller button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree) return;
            var pos = _config.Controller ? Game1.player.GetToolLocation() / 64f : e.Cursor.Tile;
            pos.X = (int) pos.X;
            pos.Y = (int) pos.Y;
            Game1.currentLocation.objects.TryGetValue(pos, out var obj);

            // Carry Chest
            if (obj != null && e.Button.IsUseToolButton() && Utility.withinRadiusOfPlayer((int) (64 * pos.X), (int) (64 * pos.Y), 1, Game1.player))
            {
                if (CarryChest(obj, Game1.currentLocation, pos)) Helper.Input.Suppress(e.Button);
                return;
            }

            // Access Carried Chest
            if (obj == null && HeldChest.Value != null && e.Button.IsActionButton())
            {
                if (AccessCarriedChest(HeldChest.Value)) Helper.Input.Suppress(e.Button);
            }
        }

        /// <summary>Raised after the player pressed/released any buttons on the keyboard, mouse, or controller.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsPlayerFree) return;
            var pos = _config.Controller ? Game1.player.GetToolLocation() / 64f : e.Cursor.Tile;
            pos.X = (int) pos.X;
            pos.Y = (int) pos.Y;
            Game1.currentLocation.objects.TryGetValue(pos, out var obj);
            if (_config.Controls.OpenCrafting.JustPressed())
            {
                if (OpenCrafting()) Helper.Input.SuppressActiveKeybinds(_config.Controls.OpenCrafting);
                return;
            }

            if (obj != null && _config.Controls.CarryChest.JustPressed() && Utility.withinRadiusOfPlayer((int) (64 * pos.X), (int) (64 * pos.Y), 1, Game1.player))
            {
                if (CarryChest(obj, Game1.currentLocation, pos)) Helper.Input.SuppressActiveKeybinds(_config.Controls.CarryChest);
                return;
            }

            if (obj == null && HeldChest.Value != null && _config.Controls.AccessCarriedChest.JustPressed())
            {
                if (AccessCarriedChest(HeldChest.Value)) Helper.Input.SuppressActiveKeybinds(_config.Controls.AccessCarriedChest);
            }
        }

        private static bool CarryChest(Object obj, GameLocation location, Vector2 pos)
        {
            if (!TryGetStorage(obj, out var storage)
                || storage.Config.Option("CanCarry", true) != StorageConfig.Choice.Enable
                || !Game1.player.addItemToInventoryBool(obj, true))
                return false;
            obj!.TileLocation = Vector2.Zero;
            if (!string.IsNullOrWhiteSpace(storage.CarrySound))
                location.playSound(storage.CarrySound);
            location.objects.Remove(pos);
            return true;
        }

        private static bool AccessCarriedChest(Chest chest)
        {
            if (!TryGetStorage(chest, out var storage) || storage.Config.Option("AccessCarried", true) != StorageConfig.Choice.Enable)
                return false;
            chest.checkForAction(Game1.player);
            return true;
        }

        private bool OpenCrafting()
        {
            if (HeldChest.Value == null || Game1.activeClickableMenu != null)
                return false;
            if (!TryGetStorage(HeldChest.Value, out var storage) || storage.Config.Option("AccessCarried", true) != StorageConfig.Choice.Enable)
                return false;
            HeldChest.Value.GetMutex().RequestLock(delegate
            {
                var pos = Utility.getTopLeftPositionForCenteringOnScreen(800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);
                Game1.activeClickableMenu = new CraftingPage(
                    (int) pos.X,
                    (int) pos.Y,
                    800 + IClickableMenu.borderWidth * 2,
                    600 + IClickableMenu.borderWidth * 2,
                    false,
                    true,
                    new List<Chest> {HeldChest.Value})
                {
                    exitFunction = delegate { HeldChest.Value.GetMutex().ReleaseLock(); }
                };
            });
            return true;
        }
    }
}