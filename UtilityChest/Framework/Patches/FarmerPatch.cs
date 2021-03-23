using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.UtilityChest.Framework.Extensions;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.UtilityChest.Framework.Patches
{
    internal class FarmerPatch : BasePatch<UtilityChest>
    {
        private static PerScreen<Chest> CurrentChest;

        public FarmerPatch(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            CurrentChest = Mod.CurrentChest;

            harmony.Patch(
                AccessTools.Property(typeof(Farmer), nameof(Farmer.CurrentTool)).GetGetMethod(),
                new HarmonyMethod(GetType(), nameof(CurrentToolPrefix))
            );
        }

        private static bool CurrentToolPrefix(Farmer __instance, ref Tool __result)
        {
            if (CurrentChest.Value?.CurrentItem() is not Tool tool) return true;
            __result = tool;
            return false;
        }
    }
}