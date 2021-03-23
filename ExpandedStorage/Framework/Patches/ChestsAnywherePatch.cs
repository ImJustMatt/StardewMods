using System;
using System.Linq;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestsAnywherePatch : BasePatch<ExpandedStorage>
    {
        public ChestsAnywherePatch(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            var loaded = Mod.Helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere");
            if (!loaded) return;
            var chestsAnywhereAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("ChestsAnywhere,"));
            var type = chestsAnywhereAssembly.GetType("Pathoschild.Stardew.ChestsAnywhere.Framework.Containers.ShippingBinContainer");
            Monitor.LogOnce("Patching Chests Anywhere for Refreshing Shipping Bin");
            var methodInfo = AccessTools.GetDeclaredMethods(type)
                .Find(m => m.Name.Equals("GrabItemFromContainerImpl", StringComparison.OrdinalIgnoreCase));
            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromContainerImplPostfix)));
        }

        public static void GrabItemFromContainerImplPostfix()
        {
            MenuController.RefreshItems();
        }
    }
}