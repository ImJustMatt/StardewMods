using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.GarbageDay.Framework;
using ImJustMatt.GarbageDay.Framework.Models;
using ImJustMatt.GarbageDay.Framework.Patches;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;
using xTile.ObjectModel;

// ReSharper disable ClassNeverInstantiated.Global

namespace ImJustMatt.GarbageDay
{
    public class GarbageDay : Mod, IAssetLoader, IAssetEditor
    {
        internal static readonly IDictionary<string, GarbageCan> GarbageCans = new Dictionary<string, GarbageCan>();
        private readonly IDictionary<string, double> _globalLoot = new Dictionary<string, double>();
        private readonly IDictionary<string, IDictionary<string, double>> _localLoot = new Dictionary<string, IDictionary<string, double>>();
        private readonly HashSet<string> _maps = new();
        private ModConfig _config;
        private IExpandedStorageAPI _expandedStorageAPI;
        private GarbageDayAPI _garbageDayAPI;
        private int _objectId;
        private bool _objectsPlaced;

        /// <summary>Allows editing Maps to remove vanilla garbage cans</summary>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.DataType == typeof(Map) && _maps.Contains(asset.AssetName) || _config.Debug;
        }

        /// <summary>Remove and store</summary>
        public void Edit<T>(IAssetData asset)
        {
            var map = asset.AsMap();
            var additions = 0;
            var edits = 0;
            for (var x = 0; x < map.Data.Layers[0].LayerWidth; x++)
            {
                for (var y = 0; y < map.Data.Layers[0].LayerHeight; y++)
                {
                    var layer = map.Data.GetLayer("Buildings");

                    // Look for Action: Garbage
                    PropertyValue property = null;
                    var tile = layer.PickTile(new Location(x, y) * Game1.tileSize, Game1.viewport.Size);
                    tile?.Properties.TryGetValue("Action", out property);
                    var parts = property?.ToString().Split(' ');

                    // Add to list
                    if (parts?.ElementAtOrDefault(0) == "Garbage" && !string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(1)))
                    {
                        if (!GarbageCans.TryGetValue(parts[1], out var garbageCan))
                        {
                            garbageCan = new GarbageCan(Helper.Content, Helper.Events, Helper.Reflection, _config);
                            GarbageCans.Add(parts[1], garbageCan);
                            additions++;
                        }
                        else
                        {
                            edits++;
                        }
                        garbageCan.MapName = map.AssetName;
                        garbageCan.Tile = new Vector2(x, y);
                    }

                    // Remove Base
                    if ((layer.Tiles[x, y]?.TileSheet.Id.Equals("Town") ?? false) && layer.Tiles[x, y].TileIndex == 78)
                    {
                        layer.Tiles[x, y] = null;
                    }

                    // Remove Lid
                    layer = map.Data.GetLayer("Front");
                    if ((layer.Tiles[x, y]?.TileSheet.Id.Equals("Town") ?? false) && layer.Tiles[x, y].TileIndex == 46)
                    {
                        layer.Tiles[x, y] = null;
                    }
                }
            }

            if (additions != 0 || edits != 0)
            {
                Monitor.Log($"Found {additions} new garbage cans, replaced {edits} on {asset.AssetName}");
            }
        }

        /// <summary>Load Data for Mods/furyx639.GarbageDay path</summary>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            var assetPrefix = PathUtilities.NormalizePath("Mods/furyx639.GarbageDay");
            return asset.AssetName.StartsWith(assetPrefix) && asset.DataType == typeof(Dictionary<string, double>);
        }

        /// <summary>Provide base versions of GarbageDay loot</summary>
        public T Load<T>(IAssetInfo asset)
        {
            if (asset.DataType != typeof(Dictionary<string, double>))
                throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
            var assetParts = PathUtilities.GetSegments(asset.AssetName).Skip(2).ToList();
            if (assetParts.ElementAtOrDefault(0) == "GlobalLoot")
                return (T) _globalLoot;
            if (assetParts.ElementAtOrDefault(0) != "Loot" || assetParts.Count != 2)
                throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
            return _localLoot.TryGetValue(assetParts[1], out var lootTable)
                ? (T) lootTable
                : (T) (object) new Dictionary<string, double>();
        }

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();

            _garbageDayAPI = new GarbageDayAPI(_maps, _globalLoot, _localLoot);

            // Initialize Global Loot from assets
            var globalLoot = helper.Content.Load<IDictionary<string, double>>(Path.Combine("assets", "global-loot.json"));
            _garbageDayAPI.AddLoot(globalLoot);

            // Add Maps from maps folder
            var files = Directory.GetFiles(Path.Combine(helper.DirectoryPath, "maps"), "*.json");
            foreach (var path in files)
            {
                var file = Path.GetFileName(path);
                var paths = helper.Content.Load<IList<string>>(Path.Combine("maps", file));
                _garbageDayAPI.AddMaps(paths);
            }

            // Load from Content Packs
            var contentPacks = helper.ContentPacks.GetOwned();
            foreach (var contentPack in contentPacks)
            {
                var content = contentPack.ReadJsonFile<Content>("garbage-day.json");
                if (content != null)
                {
                    Monitor.Log($"Loading {contentPack.Manifest.Name} {contentPack.Manifest.Version}", LogLevel.Info);
                }
                else
                {
                    Monitor.Log($"Nothing to load from {contentPack.Manifest.Name} {contentPack.Manifest.Version}");
                    continue;
                }

                // Add Maps
                _garbageDayAPI.AddMaps(content.Maps);

                // Add Global Loot
                _garbageDayAPI.AddLoot(content.GlobalLoot);

                // Add Local Loot
                foreach (var loot in content.LocalLoot)
                {
                    _garbageDayAPI.AddLoot(loot.Key, loot.Value);
                }
            }

            new Patcher<ModConfig>(ModManifest.UniqueID).ApplyAll(
                new ChestPatch(Monitor, _config)
            );

            // Console Commands
            helper.ConsoleCommands.Add("fill_garbage_cans",
                "Adds loot to all Garbage Cans.\n\nUsage: fill_garbage_cans <luck>\n- luck: Adds to player luck", delegate(string command, string[] args)
                {
                    var luck = float.TryParse(args?[0], out var luckFloat) ? luckFloat : 0;
                    foreach (var garbageCan in GarbageCans)
                    {
                        garbageCan.Value.DayStart(luck);
                    }
                });
            helper.ConsoleCommands.Add("remove_garbage_cans",
            "Remove all Garbage Cans. Run before saving to safely uninstall mod.",
            delegate
            {
                foreach (var garbageCan in GarbageCans.Values)
                {
                    if (garbageCan.Chest != null) garbageCan.Location.Objects.Remove(garbageCan.Tile);
                }
            });
            helper.ConsoleCommands.Add("reset_garbage_cans",
                "Resets all Garbage Cans by removing and replacing them.",
                delegate
                {
                    foreach (var garbageCan in GarbageCans)
                    {
                        var chest = new Chest(true, garbageCan.Value.Tile, _objectId);
                        chest.playerChoiceColor.Value = Color.DarkGray;
                        chest.modData.Add("furyx639.GarbageDay", garbageCan.Key);
                        chest.modData.Add("Pathoschild.ChestsAnywhere/IsIgnored", "true");
                        if (garbageCan.Value.Chest != null)
                        {
                            chest.items.CopyFrom(garbageCan.Value.Chest.items);
                            garbageCan.Value.Location.Objects.Remove(garbageCan.Value.Tile);
                        }
                        garbageCan.Value.Location.Objects.Add(garbageCan.Value.Tile, chest);
                    }
                });

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
            _expandedStorageAPI.ReadyToLoad += delegate { _expandedStorageAPI.LoadContentPack(Path.Combine(Helper.DirectoryPath, "assets", "GarbageCan")); };

            // Get ParentSheetIndex for object
            var jsonAssets = new JsonAssetsIntegration(Helper.ModRegistry);
            if (jsonAssets.IsLoaded)
                jsonAssets.API.IdsAssigned += delegate { _objectId = jsonAssets.API.GetBigCraftableId("Garbage Can"); };

            var modConfigMenu = new GenericModConfigMenuIntegration(Helper.ModRegistry);
            if (!modConfigMenu.IsLoaded) return;

            var config = new ModConfig
            {
                GarbageDay = _config.GarbageDay,
                GetRandomItemFromSeason = _config.GetRandomItemFromSeason
            };

            void RevertToDefault()
            {
                config.GarbageDay = _config.GarbageDay;
                config.GetRandomItemFromSeason = _config.GetRandomItemFromSeason;
            }

            void SaveToFile()
            {
                _config.GarbageDay = config.GarbageDay;
                _config.GetRandomItemFromSeason = config.GetRandomItemFromSeason;
                Helper.WriteConfig(_config);
            }

            modConfigMenu.API.RegisterModConfig(ModManifest, RevertToDefault, SaveToFile);
            modConfigMenu.API.RegisterClampedOption(ModManifest,
                "Garbage Pickup Day", "Day of week that garbage cans are emptied up (0 Sunday - 6 Saturday)",
                () => config.GarbageDay,
                value => config.GarbageDay = value,
                0, 6);
            modConfigMenu.API.RegisterClampedOption(ModManifest,
                "Get Random Item from Season", "Chance that a random item from season is added to the garbage can",
                () => (float) config.GetRandomItemFromSeason,
                value => config.GetRandomItemFromSeason = value,
                0, 1);
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
                    if (location.Objects.ContainsKey(garbageCan.Value.Tile))
                        continue;
                    var chest = new Chest(true, garbageCan.Value.Tile, _objectId);
                    chest.modData.Add("furyx639.GarbageDay", garbageCan.Key);
                    chest.modData.Add("Pathoschild.ChestsAnywhere/IsIgnored", "true");
                    location.Objects.Add(garbageCan.Value.Tile, chest);
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
            ));
        }

        /// <summary>Reset object id and tracked garbage cans</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            _objectId = 0;
            _objectsPlaced = false;
        }

        /// <summary>Add trash to garbage cans</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            foreach (var garbageCan in GarbageCans)
            {
                garbageCan.Value.DayStart();
            }
        }
    }
}