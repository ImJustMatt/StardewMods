using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestsAnywherePatch : BasePatch<ExpandedStorage>
    {
        private const string ShippingBinContainerType = "Pathoschild.Stardew.ChestsAnywhere.Framework.Containers.ShippingBinContainer";
        public ChestsAnywherePatch(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            if (!Mod.Helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere")) return;
            Monitor.LogOnce("Patching Chests Anywhere for Refreshing Shipping Bin");
            harmony.Patch(
                new AssemblyPatch("ChestsAnywhere").Method(ShippingBinContainerType, "GrabItemFromContainerImpl"),
                postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromContainerImplPostfix))
            );
        }

        public static void GrabItemFromContainerImplPostfix()
        {
            MenuController.RefreshItems();
        }
    }
}