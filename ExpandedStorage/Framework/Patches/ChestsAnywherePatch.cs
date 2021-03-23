using System;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestsAnywherePatch : BasePatch<ExpandedStorage>
    {
        private readonly bool _loaded;
        private readonly Type _type;

        public ChestsAnywherePatch(IMod mod) : base(mod)
        {
            _loaded = Mod.Helper.ModRegistry.IsLoaded("Pathoschild.ChestsAnywhere");
            if (!_loaded) return;
            var chestsAnywhereAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("ChestsAnywhere,"));
            _type = chestsAnywhereAssembly.GetType("Pathoschild.Stardew.ChestsAnywhere.Framework.Containers.ShippingBinContainer");
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            if (!_loaded) return;
            Monitor.LogOnce("Patching Chests Anywhere for Refreshing Shipping Bin");
            var methodInfo = AccessTools.GetDeclaredMethods(_type)
                .Find(m => m.Name.Equals("GrabItemFromContainerImpl", StringComparison.OrdinalIgnoreCase));
            harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromContainerImplPostfix)));
        }

        public static void GrabItemFromContainerImplPostfix()
        {
            MenuController.RefreshItems();
        }
    }
}