using Harmony;
using ImJustMatt.Common.Patches;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestsAnywherePatches : BasePatch<ExpandedStorage>
    {
        private const string ShippingBinContainerType = "Pathoschild.Stardew.ChestsAnywhere.Framework.Containers.ShippingBinContainer";

        public ChestsAnywherePatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            if (!Mod.Helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere")) return;
            Monitor.LogOnce("Patching Chests Anywhere for Refreshing Shipping Bin");
            harmony.Patch(
                new AssemblyPatch("ChestsAnywhere").Method(ShippingBinContainerType, "GrabItemFromContainerImpl"),
                postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromContainerImplPostfix))
            );
        }

        private static void GrabItemFromContainerImplPostfix()
        {
            Mod.ActiveMenu.Value.RefreshItems();
        }
    }
}