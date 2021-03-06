using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using StardewModdingAPI;
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
                AccessTools.Method(typeof(Chest), nameof(Chest.ShowMenu)),
                new HarmonyMethod(GetType(), nameof(ShowMenuPrefix))
            );
        }

        /// <summary>Produce chest interactions on show menu</summary>
        private static bool ShowMenuPrefix(Chest __instance)
        {
            if (!__instance.modData.TryGetValue("furyx639.GarbageDay", out var whichCan)) return true;
            var garbageCan = GarbageDay.GarbageCans.SingleOrDefault(c => c.WhichCan.Equals(whichCan));
            return garbageCan?.OpenCan() ?? true;
        }
    }
}