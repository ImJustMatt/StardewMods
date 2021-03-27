using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.GarbageDay.Framework.Controllers;
using ImJustMatt.GarbageDay.Framework.Models;
using ImJustMatt.GarbageDay.Framework.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

// ReSharper disable ClassNeverInstantiated.Global

namespace ImJustMatt.GarbageDay
{
    public class GarbageDay : Mod
    {
        internal static readonly IDictionary<string, GarbageCanController> GarbageCans = new Dictionary<string, GarbageCanController>();
        internal static int ObjectId;
        internal readonly Dictionary<string, Dictionary<string, double>> Loot = new();

        /// <summary>Handled content loaded by Expanded Storage.</summary>
        private AssetController _assetController;

        private IExpandedStorageAPI _expandedStorageAPI;

        /// <summary>Garbage Day API Instance</summary>
        private GarbageDayAPI _garbageDayAPI;

        private bool _objectsPlaced;
        internal ConfigController Config;

        public override object GetApi()
        {
            return _garbageDayAPI ??= new GarbageDayAPI(Loot);
        }

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ConfigController>();
            _assetController = new AssetController(this);
            helper.Content.AssetLoaders.Add(_assetController);
            helper.Content.AssetEditors.Add(_assetController);

            // Initialize Global Loot from config
            Loot.Add("Global", Config.GlobalLoot);

            new Patcher(this).ApplyAll(
                typeof(ChestPatches)
            );

            // Console Commands
            foreach (var command in CommandsController.Commands)
            {
                helper.ConsoleCommands.Add(command.Name, command.Documentation, command.Callback);
            }

            // Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            if (!Context.IsMainPlayer)
                return;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        /// <summary>Load Garbage Can</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Load Expanded Storage content
            _expandedStorageAPI = Helper.ModRegistry.GetApi<IExpandedStorageAPI>("furyx639.ExpandedStorage");
            _expandedStorageAPI.LoadContentPack(Helper.DirectoryPath);

            // Get ParentSheetIndex for object
            var jsonAssets = new JsonAssetsIntegration(Helper.ModRegistry);
            if (jsonAssets.IsLoaded)
                jsonAssets.API.IdsAssigned += delegate { ObjectId = jsonAssets.API.GetBigCraftableId("Garbage Can"); };

            var modConfigMenu = new GenericModConfigMenuIntegration(Helper.ModRegistry);
            if (!modConfigMenu.IsLoaded) return;
            Config.RegisterModConfig(Helper, ModManifest, modConfigMenu);
        }

        /// <summary>Initiate adding garbage can spots</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (_objectsPlaced) return;
            _objectsPlaced = true;
            Utility.ForAllLocations(delegate(GameLocation location)
            {
                var mapPath = PathUtilities.NormalizePath(location.mapPath.Value);

                foreach (var garbageCan in GarbageCans.Where(gc => gc.Value.MapName.Equals(mapPath)))
                {
                    garbageCan.Value.Location = location;
                    if (location.Objects.ContainsKey(garbageCan.Value.Tile)) continue;
                    var chest = new Chest(true, garbageCan.Value.Tile, ObjectId);
                    chest.modData.Add("furyx639.GarbageDay", garbageCan.Key);
                    chest.modData.Add("Pathoschild.ChestsAnywhere/IsIgnored", "true");
                    location.Objects.Add(garbageCan.Value.Tile, chest);
                    if (!Loot.ContainsKey(garbageCan.Key)) Loot.Add(garbageCan.Key, new Dictionary<string, double>());
                }
            });

            Monitor.Log(string.Join("\n",
                "Garbage Can Report",
                $"{"Name",-20} | {"Location",-30} | Coordinates",
                $"{new string('-', 21)}|{new string('-', 32)}|{new string('-', 15)}",
                string.Join("\n",
                    GarbageCans
                        .OrderBy(garbageCan => garbageCan.Key)
                        .Select(garbageCan => string.Join(" | ",
                            $"{garbageCan.Key,-20}",
                            $"{garbageCan.Value.Location.Name,-30}",
                            $"{garbageCan.Value.Tile.ToString()}")
                        ).ToList()
                )
            ), Config.LogLevelProperty);
        }

        /// <summary>Reset object id and tracked garbage cans</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            ObjectId = 0;
            _objectsPlaced = false;
        }

        /// <summary>Raised after a new in-game day starts, or after connecting to a multiplayer world.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            foreach (var garbageCan in GarbageCans.Values)
            {
                garbageCan.DayStart();
            }
        }
    }
}