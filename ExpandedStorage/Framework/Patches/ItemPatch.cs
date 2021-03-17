using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
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
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage))
                return true;

            // Disallow stacking for any chest instance objects
            if (storage.Config.Option("CanCarry", true) != StorageConfigController.Choice.Enable
                && storage.Config.Option("AccessCarried", true) != StorageConfigController.Choice.Enable
                && __instance is not Chest
                && other is not Chest)
                return true;

            __result = false;
            return false;
        }
    }
}