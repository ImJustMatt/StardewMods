using System;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class UtilityPatch : Patch<ConfigController>
    {
        public UtilityPatch(IMonitor monitor, ConfigController config) : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Utility), nameof(Utility.playerCanPlaceItemHere)),
                new HarmonyMethod(GetType(), nameof(PlayerCanPlaceItemHerePrefix))
            );
        }

        public static bool PlayerCanPlaceItemHerePrefix(ref bool __result, GameLocation location, Item item, int x, int y, Farmer f)
        {
            if (!ExpandedStorage.TryGetStorage(item, out var storage) || storage.SpriteSheet is not {Texture: { }} spriteSheet)
                return true;

            x = 64 * (x / 64);
            y = 64 * (y / 64);

            if (Utility.isPlacementForbiddenHere(location) || item == null || Game1.eventUp || f.bathingClothes.Value || f.onBridge.Value)
            {
                __result = false;
                return false;
            }

            // Is Within Tile With Leeway
            if (!Utility.withinRadiusOfPlayer(x, y, Math.Max(spriteSheet.TileWidth, spriteSheet.TileHeight), f))
            {
                __result = false;
                return false;
            }

            // Position intersects with farmer
            var rect = new Rectangle(x, y, spriteSheet.TileWidth * 64, spriteSheet.TileHeight * 64);
            if (location.farmers.Any(farmer => farmer.GetBoundingBox().Intersects(rect)))
            {
                __result = false;
                return false;
            }

            // Is Close Enough to Farmer
            rect.Inflate(32, 32);
            if (!rect.Intersects(f.GetBoundingBox()))
            {
                __result = false;
                return false;
            }

            for (var i = 0; i < spriteSheet.TileWidth; i++)
            {
                for (var j = 0; j < spriteSheet.TileHeight; j++)
                {
                    var tileLocation = new Vector2(x / 64 + i, y / 64 + j);
                    if (item.canBePlacedHere(location, tileLocation)
                        && location.getObjectAtTile((int) tileLocation.X, (int) tileLocation.Y) == null
                        && location.isTilePlaceable(tileLocation, item))
                        continue;

                    // Item cannot be placed here
                    __result = false;
                    return false;
                }
            }

            __result = true;
            return false;
        }
    }
}