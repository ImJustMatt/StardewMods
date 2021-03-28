using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class ItemPatches : BasePatch<ExpandedStorage>
    {
        public ItemPatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Item), nameof(Item.canStackWith), new[] {typeof(ISalable)}),
                postfix: new HarmonyMethod(GetType(), nameof(CanStackWithPostfix))
            );
        }

        private static void CanStackWithPostfix(Item __instance, ref bool __result, ISalable other)
        {
            if (__instance is Chest
                || other is Chest
                || Mod.AssetController.TryGetStorage(__instance, out var storage)
                && (storage.Config.Option("CanCarry", true) == StorageConfigController.Choice.Enable
                    || storage.Config.Option("AccessCarried", true) != StorageConfigController.Choice.Enable))
            {
                __result = false;
            }
        }
    }
}