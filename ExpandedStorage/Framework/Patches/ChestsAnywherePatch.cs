using System;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using StardewModdingAPI;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestsAnywherePatch : Patch<ConfigController>
    {
        private readonly bool _isChestsAnywhereLoaded;
        private readonly Type _type;

        internal ChestsAnywherePatch(IMonitor monitor, ConfigController config, bool isChestsAnywhereLoaded)
            : base(monitor, config)
        {
            _isChestsAnywhereLoaded = isChestsAnywhereLoaded;

            if (!isChestsAnywhereLoaded)
                return;

            var chestsAnywhereAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("ChestsAnywhere,"));
            _type = chestsAnywhereAssembly.GetType("Pathoschild.Stardew.ChestsAnywhere.Framework.Containers.ShippingBinContainer");
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            if (_isChestsAnywhereLoaded)
            {
                Monitor.LogOnce("Patching Chests Anywhere for Refreshing Shipping Bin");
                var methodInfo = AccessTools.GetDeclaredMethods(_type)
                    .Find(m => m.Name.Equals("GrabItemFromContainerImpl", StringComparison.OrdinalIgnoreCase));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromContainerImplPostfix)));
            }
        }

        public static void GrabItemFromContainerImplPostfix(object __instance, Item item, Farmer player)
        {
            MenuController.RefreshItems();
        }
    }
}