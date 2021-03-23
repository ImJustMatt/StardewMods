using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using StardewModdingAPI;
using StardewValley.Objects;

namespace ImJustMatt.GarbageDay.Framework.Patches
{
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class ChestPatch : BasePatch<GarbageDay>
    {
        public ChestPatch(IMod mod) : base(mod)
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
            if (!__instance.modData.ContainsKey("furyx639.GarbageDay")) return true;
            var garbageCan = GarbageDay.GarbageCans.Values.SingleOrDefault(gc => ReferenceEquals(gc.Chest, __instance));
            return garbageCan?.OpenCan() ?? true;
        }
    }
}