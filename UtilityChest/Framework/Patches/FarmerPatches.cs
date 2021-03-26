using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.UtilityChest.Framework.Extensions;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.UtilityChest.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class FarmerPatches : BasePatch<UtilityChest>
    {
        private static PerScreen<Chest> CurrentChest;

        public FarmerPatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            CurrentChest = Mod.CurrentChest;

            harmony.Patch(
                AccessTools.Property(typeof(Farmer), nameof(Farmer.CurrentTool)).GetGetMethod(),
                postfix: new HarmonyMethod(GetType(), nameof(CurrentToolPostfix))
            );
        }

        private static void CurrentToolPostfix(Farmer __instance, ref Tool __result)
        {
            if (__instance.CurrentItem is not Chest chest
                || !ReferenceEquals(chest, CurrentChest.Value)
                || chest.CurrentItem() is not Tool tool) return;
            __result = tool;
        }
    }
}