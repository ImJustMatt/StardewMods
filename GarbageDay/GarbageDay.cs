using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using xTile.Dimensions;
using xTile.ObjectModel;

namespace ImJustMatt.GarbageDay
{
    public class GarbageDay : Mod, IAssetEditor
    {
        private readonly IList<Vector2> _tiles = new List<Vector2>();

        /// <summary>Allows editing Maps to remove vanilla garbage cans</summary>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Maps/Town");
        }

        /// <summary>Remove and store</summary>
        public void Edit<T>(IAssetData asset)
        {
            if (!asset.AssetNameEquals("Maps/Town"))
                return;
            _tiles.Clear();
            var map = asset.AsMap();
            for (var x = 0; x < map.Data.Layers[0].LayerWidth; x++)
            {
                for (var y = 0; y < map.Data.Layers[0].LayerHeight; y++)
                {
                    // Remove Lid
                    if (map.Data.GetLayer("Front").Tiles[x, y]?.TileIndex == 46)
                    {
                        map.Data.GetLayer("Front").Tiles[x, y] = null;
                    }

                    // Remove Base
                    if (map.Data.GetLayer("Buildings").Tiles[x, y]?.TileIndex == 78)
                    {
                        map.Data.GetLayer("Buildings").Tiles[x, y] = null;
                    }

                    // Remove Action, Store Coordinates
                    PropertyValue property = null;
                    var tile = map.Data.GetLayer("Buildings").PickTile(new Location(x * 64, y * 64), Game1.viewport.Size);
                    tile?.Properties.TryGetValue("Action", out property);
                    if (!(property?.ToString().StartsWith("Garbage") ?? false)) continue;
                    tile.Properties.Remove("Action");
                    _tiles.Add(new Vector2(x, y));
                }
            }
        }

        public override void Entry(IModHelper helper)
        {
        }
    }
}