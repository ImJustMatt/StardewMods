using System.Linq;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.CustomBundles.Controllers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ImJustMatt.CustomBundles
{
    public class CustomBundles : Mod
    {
        private BundleController _bundleController;
        private bool _idsAssigned;

        public override void Entry(IModHelper helper)
        {
            _bundleController = new BundleController(helper.Content, Monitor);
            helper.Content.AssetLoaders.Add(_bundleController);
            helper.Content.AssetEditors.Add(_bundleController);

            // Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += delegate { QueueResetBundles(); };

            // Console Commands
            helper.ConsoleCommands.Add(
                "reset_bundles",
                "Resets all bundles and progress",
                delegate { QueueResetBundles(); }
            );

            helper.ConsoleCommands.Add(
                "print_bundles",
                "Prints all bundles to the console for debugging",
                delegate
                {
                    Monitor.Log(string.Join("\n",
                        Game1.netWorldState.Value.BundleData.Select(b => $"{b.Key,-25}|{b.Value}")
                    ));
                });
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var jsonAssets = new JsonAssetsIntegration(Helper.ModRegistry);
            if (jsonAssets.IsLoaded)
            {
                jsonAssets.API.IdsAssigned += delegate { _idsAssigned = true; };
                Helper.Events.GameLoop.ReturnedToTitle += delegate { _idsAssigned = false; };
            }
            else
            {
                _idsAssigned = true;
            }
        }

        /// <summary>Raised after the game state is updated</summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !_idsAssigned)
                return;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _bundleController.Merge();
        }

        /// <summary>Queues up MergeBundles for when the game state is ready</summary>
        private void QueueResetBundles()
        {
            if (Game1.bundleType == Game1.BundleType.Remixed)
                return;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }
    }
}