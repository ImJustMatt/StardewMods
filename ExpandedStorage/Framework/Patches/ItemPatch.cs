using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class ItemPatch : BasePatch
    {
        public ItemPatch(IMod mod) : base(mod)
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