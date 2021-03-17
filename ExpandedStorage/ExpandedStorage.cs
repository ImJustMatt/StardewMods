using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using ImJustMatt.ExpandedStorage.Framework.Views;
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
        internal static readonly PerScreen<IDictionary<Chest, StorageController>> VacuumChests = new();

        /// <summary>Dictionary of Expanded Storage configs</summary>
        internal static readonly IDictionary<string, StorageController> Storages = new Dictionary<string, StorageController>();

        /// <summary>Dictionary of Expanded Storage tabs</summary>
        internal static readonly IDictionary<string, TabController> Tabs = new Dictionary<string, TabController>();

        /// <summary>Handled content loaded by Expanded Storage.</summary>
        private ContentLoader _contentLoader;

        /// <summary>The mod configuration.</summary>
        internal ModConfig Config;

        /// <summary>Expanded Storage API.</summary>
        internal ExpandedStorageAPI ExpandedStorageAPI;

        internal JsonAssetsIntegration JsonAssets;
        internal GenericModConfigMenuIntegration ModConfigMenu;

        /// <summary>Returns Storage by object context.</summary>
        internal static bool TryGetStorage(object context, out StorageController storage)
        {
            storage = Storages
                .Select(c => c.Value)
                .FirstOrDefault(c => c.MatchesContext(context));
            return storage != null;
        }

        /// <summary>Returns ExpandedStorageTab by tab name.</summary>
        internal static TabController GetTab(string modUniqueId, string tabName)
        {
            return Tabs
                .Where(t => t.Key.EndsWith($"/{tabName}"))
                .Select(t => t.Value)
                .OrderByDescending(t => t.ModUniqueId.Equals(modUniqueId))
                .ThenByDescending(t => t.ModUniqueId.Equals("furyx639.ExpandedStorage"))
                .FirstOrDefault();
        }

        public override object GetApi()
        {
            return ExpandedStorageAPI;
        }

        public override void Entry(IModHelper helper)
        {
            JsonAssets = new JsonAssetsIntegration(helper.ModRegistry);
            ModConfigMenu = new GenericModConfigMenuIntegration(helper.ModRegistry);

            Config = helper.ReadConfig<ModConfig>();
            Config.DefaultStorage.SetAsDefault();
            Monitor.Log(string.Join("\n",
                "Mod Config",
                ModConfig.ConfigHelper.Summary(Config),
                ModConfigKeys.ConfigHelper.Summary(Config.Controls, false),
                StorageConfigController.ConfigHelper.Summary(Config.DefaultStorage, false)
            ), Config.LogLevelProperty);

            ExpandedStorageAPI = new ExpandedStorageAPI(this);
            _contentLoader = new ContentLoader(this);
            helper.Content.AssetLoaders.Add(_contentLoader);
            helper.Content.AssetEditors.Add(_contentLoader);

            MenuController.Init(helper.Events, helper.Input, Config);
            MenuModel.Init(Config);
            HSLColorPicker.Init(helper.Content);
            ChestExtensions.Init(helper.Reflection);
            StorageController.Init(helper.Events);

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
                new ItemPatch(Monitor, Config),
                new ObjectPatch(Monitor, Config),
                new FarmerPatch(Monitor, Config),
                new ChestPatch(Monitor, helper.Reflection, Config),
                new ItemGrabMenuPatch(Monitor, Config, helper.Reflection),
                new InventoryMenuPatch(Monitor, Config),
                new MenuWithInventoryPatch(Monitor, Config),
                new DiscreteColorPickerPatch(Monitor, Config),
                new DebrisPatch(Monitor, Config),
                new UtilityPatch(Monitor, Config),
                new AutomatePatch(Monitor, Config, helper.Reflection, helper.ModRegistry.IsLoaded("Pathoschild.Automate")),
                new ChestsAnywherePatch(Monitor, Config, helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere"))
            );
        }

        /// <summary>Setup Generic Mod Config Menu</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (!ModConfigMenu.IsLoaded)
                return;

            void DefaultConfig()
            {
                foreach (var field in ModConfig.ConfigHelper.Fields)
                {
                    ModConfig.ConfigHelper.SetValue(Config, field, field.DefaultValue);
                }

                foreach (var field in ModConfigKeys.ConfigHelper.Fields)
                {
                    ModConfigKeys.ConfigHelper.SetValue(Config.Controls, field, field.DefaultValue);
                }
            }

            void SaveConfig()
            {
                Helper.WriteConfig(Config);
            }

            ModConfigMenu.API.RegisterModConfig(ModManifest, DefaultConfig, SaveConfig);
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Controls", "Controller/Keyboard controls", "Controls");
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Tweaks", "Modify behavior for certain features", "Tweaks");
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Default Storage", "Global default storage config", "Default Storage");

            ModConfigMenu.API.StartNewPage(ModManifest, "Controls");
            ModConfigMenu.API.RegisterLabel(ModManifest, "Controls", "Controller/Keyboard controls");
            ModConfigMenu.RegisterConfigOptions(ModManifest, ModConfigKeys.ConfigHelper, Config.Controls);
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");

            ModConfigMenu.API.StartNewPage(ModManifest, "Tweaks");
            ModConfigMenu.API.RegisterLabel(ModManifest, "Tweaks", "Modify behavior for certain features");
            ModConfigMenu.RegisterConfigOptions(ModManifest, ModConfig.ConfigHelper, Config);
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");

            ModConfigMenu.API.StartNewPage(ModManifest, "Default Storage");
            ModConfigMenu.API.RegisterLabel(ModManifest, "Default Storage", "Global default storage config");
            ModConfigMenu.RegisterConfigOptions(ModManifest, StorageConfigController.ConfigHelper, Config.DefaultStorage);
            ModConfigMenu.API.RegisterPageLabel(ModManifest, "Go Back", "", "");
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
                .Take(Config.VacuumToFirstRow ? 12 : who.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, i => TryGetStorage(i, out var storage) ? storage : null)
                .Where(s => s.Value?.Config.Option("VacuumItems", true) == StorageConfigController.Choice.Enable)
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

            StorageController storage = null;
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
            var pos = Config.Controller ? Game1.player.GetToolLocation() / 64f : e.Cursor.Tile;
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
            var pos = Config.Controller ? Game1.player.GetToolLocation() / 64f : e.Cursor.Tile;
            pos.X = (int) pos.X;
            pos.Y = (int) pos.Y;
            Game1.currentLocation.objects.TryGetValue(pos, out var obj);
            if (Config.Controls.OpenCrafting.JustPressed())
            {
                if (OpenCrafting()) Helper.Input.SuppressActiveKeybinds(Config.Controls.OpenCrafting);
                return;
            }

            if (obj != null && Config.Controls.CarryChest.JustPressed() && Utility.withinRadiusOfPlayer((int) (64 * pos.X), (int) (64 * pos.Y), 1, Game1.player))
            {
                if (CarryChest(obj, Game1.currentLocation, pos)) Helper.Input.SuppressActiveKeybinds(Config.Controls.CarryChest);
                return;
            }

            if (obj == null && HeldChest.Value != null && Config.Controls.AccessCarriedChest.JustPressed())
            {
                if (AccessCarriedChest(HeldChest.Value)) Helper.Input.SuppressActiveKeybinds(Config.Controls.AccessCarriedChest);
            }
        }

        private static bool CarryChest(Object obj, GameLocation location, Vector2 pos)
        {
            if (!TryGetStorage(obj, out var storage)
                || storage.Config.Option("CanCarry", true) != StorageConfigController.Choice.Enable
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
            if (!TryGetStorage(chest, out var storage) || storage.Config.Option("AccessCarried", true) != StorageConfigController.Choice.Enable)
                return false;
            chest.checkForAction(Game1.player);
            return true;
        }

        private bool OpenCrafting()
        {
            if (HeldChest.Value == null || Game1.activeClickableMenu != null)
                return false;
            if (!TryGetStorage(HeldChest.Value, out var storage) || storage.Config.Option("AccessCarried", true) != StorageConfigController.Choice.Enable)
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