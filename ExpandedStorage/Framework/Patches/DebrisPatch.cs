using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class DebrisPatch : BasePatch<ExpandedStorage>
    {
        public DebrisPatch(IMod mod) : base(mod)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Debris), nameof(Debris.collect)),
                new HarmonyMethod(GetType(), nameof(collect_Prefix)));
        }

        /// <summary>Collect debris directly into carried chest.</summary>
        public static bool collect_Prefix(Debris __instance, ref bool __result, Farmer farmer, Chunk chunk)
        {
            chunk ??= __instance.Chunks.FirstOrDefault();
            if (chunk == null || !(ExpandedStorage.VacuumChests.Value?.Any() ?? false))
                return true;

            var switcher = __instance.debrisType.Value.Equals(Debris.DebrisType.ARCHAEOLOGY) || __instance.debrisType.Value.Equals(Debris.DebrisType.OBJECT)
                ? chunk.debrisType
                : chunk.debrisType - chunk.debrisType % 2;

            if (__instance.item == null && __instance.debrisType.Value == 0)
                return true;

            if (__instance.item != null)
            {
                __instance.item = farmer.AddItemToInventory(__instance.item);
                __result = __instance.item == null;
                return !__result;
            }

            Item item = __instance.debrisType.Value switch
            {
                Debris.DebrisType.ARCHAEOLOGY => new Object(chunk.debrisType, 1),
                _ when switcher <= -10000 => new MeleeWeapon(switcher),
                _ when switcher <= 0 => new Object(Vector2.Zero, -switcher),
                _ when switcher == 93 || switcher == 94 => new Torch(Vector2.Zero, 1, switcher)
                {
                    Quality = __instance.itemQuality
                },
                _ => new Object(Vector2.Zero, switcher, 1) {Quality = __instance.itemQuality}
            };

            item = farmer.AddItemToInventory(item);
            __result = item == null;
            return !__result;
        }
    }
}