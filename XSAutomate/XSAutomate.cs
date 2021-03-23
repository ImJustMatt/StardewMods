using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.API;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.XSAutomate
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class XSAutomate : Mod
    {
        private const string ChestContainerType = "Pathoschild.Stardew.Automate.Framework.Storage.ChestContainer";
        private static IReflectionHelper _reflection;
        private static IExpandedStorageAPI _expandedStorageAPI;

        public override void Entry(IModHelper helper)
        {
            _reflection = helper.Reflection;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.LogOnce("Patching Automate for Restricted Storage");
            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);
            harmony.Patch(
                new AssemblyPatch("Automate").Method(ChestContainerType, "Store"),
                new HarmonyMethod(GetType(), nameof(StorePrefix))
            );
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _expandedStorageAPI = Helper.ModRegistry.GetApi<IExpandedStorageAPI>("furyx639.ExpandedStorage");
            var automateAPI = Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
            automateAPI.AddFactory(new AutomationFactoryController());
        }

        private static bool StorePrefix(Chest ___Chest, ITrackedStack stack)
        {
            var item = _reflection.GetProperty<Item>(stack, "Sample").GetValue();
            return _expandedStorageAPI.AcceptsItem(___Chest, item);
        }
    }
}