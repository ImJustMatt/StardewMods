using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Harmony;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.CustomBundles.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ImJustMatt.CustomBundles
{
    public class CustomBundles : Mod
    {
        private static IModHelper _helper;
        private static IMonitor _monitor;
        private static bool _idsAssigned;
        private static BundleCollection _bundleCollection;

        public override void Entry(IModHelper helper)
        {
            _helper = helper;
            _monitor = Monitor;

            _bundleCollection = new BundleCollection(_helper.Content);
            _helper.Content.AssetLoaders.Add(_bundleCollection);

            // Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            //helper.Events.GameLoop.DayStarted += OnDayStarted;

            // Patches
            var harmony = HarmonyInstance.Create(ModManifest.UniqueID);
            harmony.Patch(
                AccessTools.Method(typeof(Game1), nameof(Game1.loadForNewGame)),
                postfix: new HarmonyMethod(GetType(), nameof(LoadForNewGamePostfix))
            );

            // Console Commands
            helper.ConsoleCommands.Add(
                "reset_bundles",
                "Resets all bundles and progress",
                delegate { _helper.Events.GameLoop.UpdateTicked += ResetBundles; }
            );

            helper.ConsoleCommands.Add(
                "print_bundles",
                "Prints all bundles to the console for debugging",
                delegate
                {
                    Monitor.Log(
                        string.Join("\n",
                            Game1.netWorldState.Value.BundleData
                                .Select(b => $"{b.Key,-25}|{b.Value}")
                        )
                    );
                });
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (_bundleCollection.Changed)
            {
                _helper.Events.GameLoop.UpdateTicked += ResetBundles;
            }
        }

        private static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var jsonAssets = new JsonAssetsIntegration(_helper.ModRegistry);
            if (jsonAssets.IsLoaded)
            {
                jsonAssets.API.IdsAssigned += delegate { _idsAssigned = true; };
                _helper.Events.GameLoop.ReturnedToTitle += delegate { _idsAssigned = false; };
            }
            else
            {
                _idsAssigned = true;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static void LoadForNewGamePostfix(Game1 __instance, bool loadedGame)
        {
            if (loadedGame || Game1.bundleType == Game1.BundleType.Remixed)
                return;
            _helper.Events.GameLoop.UpdateTicked += ResetBundles;
        }

        private static void ResetBundles(object sender, UpdateTickedEventArgs e)
        {
            if (!_bundleCollection.Changed)
            {
                _helper.Events.GameLoop.UpdateTicked -= ResetBundles;
                return;
            }

            if (!Context.IsWorldReady || !_idsAssigned)
                return;
            _helper.Events.GameLoop.UpdateTicked -= ResetBundles;
            _monitor.Log("Resetting bundles");

            Game1.netWorldState.Value.SetBundleData(_bundleCollection.Bundles);

            if (!Game1.game1.GetNewGameOption<bool>("YearOneCompletable"))
                return;

            foreach (var itemSplit in Game1.netWorldState.Value.BundleData.Values.Select(value => value.Split('/')[2].Split(' ')))
            {
                for (var i = 0; i < itemSplit.Length; i += 3)
                {
                    if (itemSplit[i] != "266")
                        continue;
                    Game1.netWorldState.Value.VisitsUntilY1Guarantee = new Random((int) Game1.uniqueIDForThisGame * 12).Next(2, 31);
                }
            }
        }
    }
}