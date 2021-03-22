using System;
using System.Linq;
using Harmony;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorageAutomate;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using ITrackedStack = ImJustMatt.ExpandedStorageAutomate.ITrackedStack;

namespace ImJustMatt.XSAutomate
{
    public class XSAutomate : Mod
    {
        private static IReflectionHelper _reflection;
        private static IExpandedStorageAPI _expandedStorageAPI;

        public override void Entry(IModHelper helper)
        {
            _reflection = helper.Reflection;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);
            var automateAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("Automate,"));
            var type = automateAssembly.GetType("Pathoschild.Stardew.Automate.Framework.Storage.ChestContainer");
            Monitor.LogOnce("Patching Automate for Restricted Storage");
            var methodInfo = AccessTools.GetDeclaredMethods(type)
                .Find(m => m.Name.Equals("Store", StringComparison.OrdinalIgnoreCase));
            harmony.Patch(methodInfo, new HarmonyMethod(GetType(), nameof(StorePrefix)));
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _expandedStorageAPI = Helper.ModRegistry.GetApi<IExpandedStorageAPI>("furyx639.ExpandedStorage");
            var automateAPI = Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
            automateAPI.AddFactory(new AutomationFactoryController());
        }

        private static bool StorePrefix(object __instance, ITrackedStack stack)
        {
            var chest = _reflection.GetField<Chest>(__instance, "Chest").GetValue();
            var item = _reflection.GetProperty<Item>(stack, "Sample").GetValue();
            return _expandedStorageAPI.AcceptsItem(chest, item);
        }
    }
}