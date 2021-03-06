using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ItemPatch : Patch<ModConfig>
    {
        public ItemPatch(IMonitor monitor, ModConfig config) : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Item), nameof(Item.canStackWith), new[] {typeof(ISalable)}),
                new HarmonyMethod(GetType(), nameof(CanStackWithPrefix))
            );
        }

        public static bool CanStackWithPrefix(Item __instance, ref bool __result, ISalable other)
        {
            var storage = ExpandedStorage.GetStorage(__instance);
            if (storage == null)
                return true;

            // Disallow stacking for any chest instance objects
            if (storage.Option("CanCarry", true) != StorageConfig.Choice.Enable
                && storage.Option("AccessCarried", true) != StorageConfig.Choice.Enable
                && __instance is not Chest
                && other is not Chest)
                return true;

            __result = false;
            return false;
        }
    }
}