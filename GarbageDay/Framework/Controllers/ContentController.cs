using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using xTile;
using xTile.Dimensions;
using xTile.ObjectModel;

namespace ImJustMatt.GarbageDay.Framework.Controllers
{
    internal class ContentController : IAssetLoader, IAssetEditor
    {
        private readonly GarbageDay _mod;

        internal ContentController(GarbageDay mod)
        {
            _mod = mod;
        }

        /// <summary>Allows editing Maps to remove vanilla garbage cans</summary>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.DataType == typeof(Map) && _mod.Maps.Contains(asset.AssetName) || _mod.Config.Debug;
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
                        if (!GarbageDay.GarbageCans.TryGetValue(parts[1], out var garbageCan))
                        {
                            garbageCan = new GarbageCanController(_mod.Helper.Content, _mod.Helper.Events, _mod.Helper.Reflection, _mod.Config);
                            GarbageDay.GarbageCans.Add(parts[1], garbageCan);
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
                _mod.Monitor.Log($"Found {additions} new garbage cans, replaced {edits} on {asset.AssetName}");
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
                return (T) _mod.GlobalLoot;
            if (assetParts.ElementAtOrDefault(0) != "Loot" || assetParts.Count != 2)
                throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
            return _mod.LocalLoot.TryGetValue(assetParts[1], out var lootTable)
                ? (T) lootTable
                : (T) (object) new Dictionary<string, double>();
        }
    }
}