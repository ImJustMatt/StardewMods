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
    internal class AssetController : IAssetLoader, IAssetEditor
    {
        private readonly HashSet<string> _excludedAssets = new();
        private readonly GarbageDay _mod;

        internal AssetController(GarbageDay mod)
        {
            _mod = mod;
        }

        /// <summary>Allows editing Maps to remove vanilla garbage cans</summary>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.DataType == typeof(Map) && !_excludedAssets.Contains(asset.AssetName);
        }

        /// <summary>Remove and store</summary>
        public void Edit<T>(IAssetData asset)
        {
            var map = asset.AsMap().Data;
            if (!asset.AssetNameEquals(@"Maps\Town") && !map.Properties.ContainsKey("GarbageDay"))
            {
                _excludedAssets.Add(asset.AssetName);
                return;
            }
            var additions = 0;
            var edits = 0;
            for (var x = 0; x < map.Layers[0].LayerWidth; x++)
            {
                for (var y = 0; y < map.Layers[0].LayerHeight; y++)
                {
                    var layer = map.GetLayer("Buildings");
                    PropertyValue property = null;
                    string whichCan = "";
                    var tile = layer.PickTile(new Location(x, y) * Game1.tileSize, Game1.viewport.Size);

                    // Look for Garbage: WhichCan
                    tile?.Properties.TryGetValue("Garbage", out property);
                    whichCan = property?.ToString();
                    if (string.IsNullOrWhiteSpace(whichCan))
                    {
                        // Look for Action: Garbage
                        tile?.Properties.TryGetValue("Action", out property);
                        var parts = property?.ToString().Split(' ');
                        if (parts?.ElementAtOrDefault(0) == "Garbage")
                        {
                            whichCan = parts.ElementAtOrDefault(1);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(whichCan))
                    {
                        if (!GarbageDay.GarbageCans.TryGetValue(whichCan, out var garbageCan))
                        {
                            garbageCan = new GarbageCanController(_mod);
                            GarbageDay.GarbageCans.Add(whichCan, garbageCan);
                            additions++;
                        }
                        else
                        {
                            edits++;
                        }

                        garbageCan.MapName = PathUtilities.NormalizePath(asset.AssetName);
                        garbageCan.Tile = new Vector2(x, y);
                        map.GetLayer("Back").PickTile(new Location(x, y) * Game1.tileSize, Game1.viewport.Size)?.Properties.Add("NoPath", "");
                    }

                    // Remove Base
                    if ((layer.Tiles[x, y]?.TileSheet.Id.Equals("Town") ?? false) && layer.Tiles[x, y].TileIndex == 78)
                    {
                        layer.Tiles[x, y] = null;
                    }

                    // Remove Lid
                    layer = map.GetLayer("Front");
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

        /// <summary>Load Data for Mods/GarbageDay/Loot path</summary>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            var assetName = PathUtilities.NormalizePath(asset.AssetName);
            var modPath = PathUtilities.NormalizePath("Mods/GarbageDay/Loot/");
            return assetName.StartsWith(modPath) && asset.DataType == typeof(Dictionary<string, double>);
        }

        /// <summary>Provide base versions of GarbageDay loot</summary>
        public T Load<T>(IAssetInfo asset)
        {
            var whichCan = PathUtilities.GetSegments(asset.AssetName).ElementAtOrDefault(3);
            if (whichCan != null && _mod.Loot.TryGetValue(whichCan, out var lootTable))
            {
                return (T) (object) lootTable;
            }
            throw new InvalidOperationException();
        }
    }
}