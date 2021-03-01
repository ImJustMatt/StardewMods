using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Integrations;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using ImJustMatt.ExpandedStorage.Framework.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        /// <summary>Dictionary of Expanded Storage content pack asset loaders</summary>
        public static readonly IDictionary<string, Func<string, Texture2D>> AssetLoaders = new Dictionary<string, Func<string, Texture2D>>();

        /// <summary>Tracks previously held chest lid frame.</summary>
        private readonly PerScreen<int> _currentLidFrame = new();

        /// <summary>Reflected currentLidFrame for previousHeldChest.</summary>
        private readonly PerScreen<IReflectedField<int>> _currentLidFrameReflected = new();

        /// <summary>The mod configuration.</summary>
        private ModConfig _config;

        /// <summary>Handled content loaded by Expanded Storage.</summary>
        private ContentLoader _contentLoader;

        /// <summary>Expanded Storage API.</summary>
        private ExpandedStorageAPI _expandedStorageAPI;

        /// <summary>Returns ExpandedStorageConfig by item name.</summary>
        public static Storage GetStorage(object context)
        {
            var storage = Storages
                .Select(c => c.Value)
                .FirstOrDefault(c => c.MatchesContext(context));
            return storage;
        }

        /// <summary>Returns true if item is an ExpandedStorage.</summary>
        private static bool HasStorage(object context)
        {
            return Storages.Any(c => c.Value.MatchesContext(context));
        }

        /// <summary>Returns ExpandedStorageTab by tab name.</summary>
        public static StorageTab GetTab(string modUniqueId, string tabName)
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
            _config = helper.ReadConfig<ModConfig>();
            _config.DefaultStorage.SetDefault();
            Monitor.Log(_config.SummaryReport, LogLevel.Debug);

            _expandedStorageAPI = new ExpandedStorageAPI(Helper, Monitor, Storages, StorageTabs);
            _contentLoader = new ContentLoader(Helper, ModManifest, Monitor, _config, _expandedStorageAPI);
            helper.Content.AssetEditors.Add(_expandedStorageAPI);

            ChestExtensions.Init(helper.Reflection);
            MenuViewModel.Init(helper.Events, helper.Input, _config);
            MenuModel.Init(_config);
            HSLColorPicker.Init(helper.Content);

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
                new ChestPatch(Monitor, _config),
                new ItemGrabMenuPatch(Monitor, _config, helper.Reflection),
                new InventoryMenuPatch(Monitor, _config),
                new MenuWithInventoryPatch(Monitor, _config),
                new DiscreteColorPickerPatch(Monitor, _config),
                new DebrisPatch(Monitor, _config),
                new UtilityPatch(Monitor, _config),
                new AutomatePatch(Monitor, _config, helper.Reflection, helper.ModRegistry.IsLoaded("Pathoschild.Automate")),
                new ChestsAnywherePatch(Monitor, _config, helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere")));
        }

        /// <summary>Setup Generic Mod Config Menu</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var modConfigApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            if (modConfigApi == null)
                return;

            var config = new ModConfig();
            config.CopyFrom(_config);

            void DefaultConfig()
            {
                config.CopyFrom(new ModConfig());
            }

            void SaveConfig()
            {
                _config.CopyFrom(config);
                Helper.WriteConfig(config);
                _contentLoader.ReloadDefaultStorageConfigs();
            }

            modConfigApi.RegisterModConfig(ModManifest, DefaultConfig, SaveConfig);
            ModConfig.RegisterModConfig(ModManifest, modConfigApi, config);
        }

        /// <summary>Track toolbar changes before user input.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            var location = e.Location;
            var removed = e.Removed.LastOrDefault(p => p.Value is Chest && HasStorage(p.Value));

            if (removed.Value != null)
            {
                var storage = GetStorage(removed.Value);
                if (storage?.SpriteSheet is {Texture: { }} spriteSheet)
                {
                    var x = removed.Value.modData.TryGetValue("furyx639.ExpandedStorage/X", out var xStr)
                        ? int.Parse(xStr)
                        : 0;
                    var y = removed.Value.modData.TryGetValue("furyx639.ExpandedStorage/Y", out var yStr)
                        ? int.Parse(yStr)
                        : 0;

                    void RemoveObject(Vector2 pos)
                    {
                        if (pos.Equals(removed.Key) || !location.Objects.ContainsKey(pos))
                            return;
                        location.Objects.Remove(pos);
                    }

                    Helper.Events.World.ObjectListChanged -= OnObjectListChanged;
                    spriteSheet.ForEachPos(x, y, RemoveObject);
                    Helper.Events.World.ObjectListChanged += OnObjectListChanged;
                }
            }

            var oldChest = HeldChest.Value;
            var chest = e.Added
                .Select(p => p.Value)
                .OfType<Chest>()
                .LastOrDefault(HasStorage);

            if (oldChest == null || chest == null || chest.items.Any() || !ReferenceEquals(e.Location, Game1.currentLocation))
                return;

            // Backup method for restoring carried Chest items
            chest.name = oldChest.name;
            chest.playerChoiceColor.Value = oldChest.playerChoiceColor.Value;
            if (oldChest.items.Any())
                chest.items.CopyFrom(oldChest.items);
            foreach (var modData in oldChest.modData)
                chest.modData.CopyFrom(modData);
        }

        /// <summary>Initialize player item vacuum chests.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;

            VacuumChests.Value = Game1.player.Items
                .Take(_config.VacuumToFirstRow ? 12 : Game1.player.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, GetStorage)
                .Where(s => s.Value?.Option("VacuumItems") == StorageConfig.Choice.Enable)
                .ToDictionary(s => s.Key, s => s.Value);

            Monitor.VerboseLog($"Found {VacuumChests.Value.Count} For Vacuum:\n" + string.Join("\n", VacuumChests.Value.Select(s => $"\t{s.Key}")));
        }

        /// <summary>Refresh player item vacuum chests.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;

            VacuumChests.Value = e.Player.Items
                .Take(_config.VacuumToFirstRow ? 12 : e.Player.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, GetStorage)
                .Where(s => s.Value?.Option("VacuumItems") == StorageConfig.Choice.Enable)
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

            if (Game1.player.CurrentItem is not Chest chest)
            {
                HeldChest.Value = null;
                return;
            }

            if (!ReferenceEquals(HeldChest.Value, chest))
            {
                HeldChest.Value = chest;
                chest.fixLidFrame();
            }

            if (chest.frameCounter.Value <= -1 || _currentLidFrame.Value > chest.getLastLidFrame())
                return;

            chest.frameCounter.Value--;
            if (chest.frameCounter.Value > 0 || !chest.GetMutex().IsLockHeld())
                return;

            if (_currentLidFrame.Value == chest.getLastLidFrame())
            {
                chest.frameCounter.Value = -1;
                _currentLidFrame.Value = chest.startingLidFrame.Value;
                _currentLidFrameReflected.Value.SetValue(_currentLidFrame.Value);
                chest.ShowMenu();
            }
            else
            {
                chest.frameCounter.Value = 5;
                _currentLidFrame.Value++;
                _currentLidFrameReflected.Value.SetValue(_currentLidFrame.Value);
            }
        }

        /// <summary>Track toolbar changes before user input.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            var location = Game1.currentLocation;
            var pos = _config.Controller ? Game1.player.GetToolLocation() / 64f : e.Cursor.Tile;
            Storage storage;
            pos.X = (int) pos.X;
            pos.Y = (int) pos.Y;
            location.objects.TryGetValue(pos, out var obj);

            // Carry Chest
            if (obj != null && e.Button.IsUseToolButton() && Utility.withinRadiusOfPlayer((int) (64 * pos.X), (int) (64 * pos.Y), 1, Game1.player))
            {
                storage = GetStorage(obj);
                if (storage == null
                    || storage.Option("CanCarry", true) != StorageConfig.Choice.Enable
                    || !Game1.player.addItemToInventoryBool(obj, true))
                    return;

                obj!.TileLocation = Vector2.Zero;
                if (!string.IsNullOrWhiteSpace(storage.CarrySound))
                    location.playSound(storage.CarrySound);
                location.objects.Remove(pos);
                Helper.Input.Suppress(e.Button);
                return;
            }

            // Access Carried Chest
            if (obj == null && HeldChest.Value != null && e.Button.IsActionButton())
            {
                storage = GetStorage(HeldChest.Value);
                if (storage == null || storage.Option("AccessCarried", true) != StorageConfig.Choice.Enable)
                    return;

                HeldChest.Value.GetMutex().RequestLock(delegate
                {
                    HeldChest.Value.fixLidFrame();
                    HeldChest.Value.performOpenChest();
                    _currentLidFrameReflected.Value = Helper.Reflection.GetField<int>(HeldChest.Value, "currentLidFrame");
                    _currentLidFrame.Value = HeldChest.Value.startingLidFrame.Value;
                    Game1.playSound(storage.OpenSound);
                    Game1.player.Halt();
                    Game1.player.freezePause = 1000;
                });

                Helper.Input.Suppress(e.Button);
            }
        }

        /// <summary>Track toolbar changes before user input.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (HeldChest.Value == null || Game1.activeClickableMenu != null || !_config.Controls.OpenCrafting.JustPressed())
                return;

            var storage = GetStorage(HeldChest.Value);
            if (storage == null || storage.Option("AccessCarried", true) != StorageConfig.Choice.Enable)
                return;

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

            Helper.Input.SuppressActiveKeybinds(_config.Controls.OpenCrafting);
        }
    }
}