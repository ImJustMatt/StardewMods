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
        internal static readonly IList<GarbageCan> GarbageCans = new List<GarbageCan>();
        private ModConfig _config;
        private IExpandedStorageAPI _expandedStorageAPI;
        private bool _garbageChecked = true;
        private int _objectId;
        private bool _objectsPlaced;

        /// <summary>Allows editing Maps to remove vanilla garbage cans</summary>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetName.StartsWith("Maps") && asset.DataType == typeof(Map);
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
                    if ((layer.Tiles[x, y]?.TileSheet.Id.Equals("Town") ?? false) && layer.Tiles[x, y].TileIndex == 78)
                    {
                        // Look for Action: Garbage
                        PropertyValue property = null;
                        var tile = layer.PickTile(new Location(x, y) * Game1.tileSize, Game1.viewport.Size);
                        tile?.Properties.TryGetValue("Action", out property);
                        var parts = property?.ToString().Split(' ');

                        // Add to list
                        if (parts?.ElementAtOrDefault(0) == "Garbage" && !string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(1)))
                        {
                            var garbageCan = GarbageCans.SingleOrDefault(c => c.WhichCan.Equals(parts[1]));
                            if (garbageCan != null)
                            {
                                GarbageCans.Remove(garbageCan);
                                edits++;
                                additions--;
                            }
                            garbageCan = new GarbageCan(Helper.Content, Helper.Events, Helper.Reflection, _config)
                            {
                                MapName = PathUtilities.NormalizePath(map.AssetName),
                                WhichCan = parts[1],
                                Tile = new Vector2(x, y)
                            };
                            GarbageCans.Add(garbageCan);
                            additions++;
                        }

                        // Remove Base
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
            Monitor.Log($"Found {additions} new garbage cans, replaced {edits}");
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
                return Helper.Content.Load<T>(Path.Combine("assets", "global-loot.json"));
            if (assetParts.ElementAtOrDefault(0) != "Loot" || assetParts.Count != 2)
                throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
            return (T) (object) new Dictionary<string, double>();
        }

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();

            new Patcher<ModConfig>(ModManifest.UniqueID).ApplyAll(
                new ChestPatch(Monitor, _config)
            );

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

            // Get Sheet Index for object
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
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        /// <summary>Reset object id and tracked garbage cans</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            _objectId = 0;
            GarbageCans.Clear();
        }

        /// <summary>Add storage to garbage can spot</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (_objectId == 0 || !Context.IsWorldReady)
                return;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            if (!_objectsPlaced)
            {
                _objectsPlaced = true;
                foreach (var location in Game1.locations)
                {
                    var mapPath = PathUtilities.NormalizePath(location.mapPath.Value);
                    foreach (var garbageCan in GarbageCans.Where(g => g.MapName.Equals(mapPath)))
                    {
                        garbageCan.Location = location;
                        if (location.Objects.ContainsKey(garbageCan.Tile))
                            continue;
                        var chest = new Chest(true, garbageCan.Tile, _objectId);
                        chest.modData.Add("furyx639.GarbageDay", garbageCan.WhichCan);
                        location.Objects.Add(garbageCan.Tile, chest);
                    }
                }
            }

            if (_garbageChecked) return;
            _garbageChecked = true;
            foreach (var garbageCan in GarbageCans)
            {
                garbageCan.DayStart();
            }
        }

        /// <summary>Add trash to garbage cans</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            _garbageChecked = false;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }
    }
}