using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.GarbageDay.Framework.Patches
{
    internal class ChestPatch : Patch<ModConfig>
    {
        public ChestPatch(IMonitor monitor, ModConfig config) : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.performToolAction)),
                new HarmonyMethod(GetType(), nameof(PerformToolActionPrefix))
            );

            // Patch NPC reaction to chest open
            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.ShowMenu)),
                new HarmonyMethod(GetType(), nameof(ShowMenuPrefix))
            );
        }

        /// <summary>Do not allow trash cans to be broken</summary>
        private static bool PerformToolActionPrefix(Chest __instance, ref bool __result, Tool t, GameLocation location)
        {
            if (!__instance.modData.ContainsKey("furyx639.GarbageDay")) return true;
            __result = false;
            return false;
        }

        /// <summary>Produce chest interactions on show menu</summary>
        private static bool ShowMenuPrefix(Chest __instance)
        {
            if (!__instance.modData.TryGetValue("furyx639.GarbageDay", out var whichCanStr)) return true;
            var whichCan = int.Parse(whichCanStr);
            var garbageCan = GarbageDay.GarbageCans.SingleOrDefault(c => c.WhichCan == whichCan);
            return garbageCan?.OpenCan() ?? true;
        }
    }
}