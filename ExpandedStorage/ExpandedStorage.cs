using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using ImJustMatt.ExpandedStorage.Framework.Views;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;

// ReSharper disable ClassNeverInstantiated.Global

namespace ImJustMatt.ExpandedStorage
{
    public class ExpandedStorage : Mod
    {
        /// <summary>Tracks all chests that may be used for vacuum items.</summary>
        internal VacuumChestController VacuumChests;

        internal ChestController ChestController;

        /// <summary>Controller for Active ItemGrabMenu.</summary>
        internal readonly PerScreen<MenuController> ActiveMenu = new();

        /// <summary>Handled content loaded by Expanded Storage.</summary>
        internal AssetController AssetController;

        /// <summary>The mod configuration.</summary>
        internal ConfigController Config;

        /// <summary>Expanded Storage API.</summary>
        internal ExpandedStorageAPI ExpandedStorageAPI;

        internal JsonAssetsIntegration JsonAssets;
        internal GenericModConfigMenuIntegration ModConfigMenu;

        public override object GetApi()
        {
            return ExpandedStorageAPI;
        }

        public override void Entry(IModHelper helper)
        {
            JsonAssets = new JsonAssetsIntegration(helper.ModRegistry);
            ModConfigMenu = new GenericModConfigMenuIntegration(helper.ModRegistry);

            Config = helper.ReadConfig<ConfigController>();
            Config.DefaultStorage.SetAsDefault();
            Config.Log(Monitor);
            
            AssetController = new AssetController(this);
            helper.Content.AssetLoaders.Add(AssetController);
            helper.Content.AssetEditors.Add(AssetController);

            ExpandedStorageAPI = new ExpandedStorageAPI(this, AssetController);
            // Default Exclusions
            ExpandedStorageAPI.DisableWithModData("aedenthorn.AdvancedLootFramework/IsAdvancedLootFrameworkChest");
            ExpandedStorageAPI.DisableDrawWithModData("aedenthorn.CustomChestTypes/IsCustomChest");

            VacuumChests = new VacuumChestController(AssetController, Monitor, helper.Events, Config.VacuumToFirstRow);
            ChestController = new ChestController(
                AssetController,
                Config,
                helper.Events,
                helper.Input,
                helper.ModRegistry.IsLoaded("spacechase0.CarryChest")
            );

            ItemExtensions.Init(AssetController);
            FarmerExtensions.Init(VacuumChests);
            StorageController.Init(helper.Events);
            HSLColorPicker.Init(helper.Content);

            if (helper.ModRegistry.IsLoaded("spacechase0.CarryChest"))
            {
                Monitor.Log("Do not run Expanded Storage with Carry Chest!", LogLevel.Warn);
            }

            // Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.MenuChanged += OnMenuChanged;

            // Harmony Patches
            new Patcher(this).ApplyAll(
                typeof(ItemPatches),
                typeof(ObjectPatches),
                typeof(FarmerPatches),
                typeof(ChestPatches),
                typeof(ItemGrabMenuPatches),
                typeof(InventoryMenuPatches),
                typeof(MenuWithInventoryPatches),
                typeof(DiscreteColorPickerPatches),
                typeof(DebrisPatches),
                typeof(UtilityPatches),
                typeof(ChestsAnywherePatches)
            );
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config.RegisterModConfig(Helper, ModManifest, ModConfigMenu);
        }

        /// <summary>Resets scrolling/overlay when chest menu exits or context changes.</summary>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            ActiveMenu.Value?.Dispose();
            if (e.NewMenu is ItemGrabMenu {shippingBin: false} menu)
                ActiveMenu.Value = new MenuController(menu, AssetController, Config, Helper.Events, Helper.Input);
        }
    }
}