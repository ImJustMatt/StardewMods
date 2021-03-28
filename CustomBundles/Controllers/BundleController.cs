using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.CustomBundles.Models;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.CustomBundles.Controllers
{
    internal class BundleController : IAssetLoader, IAssetEditor
    {
        private readonly IContentHelper _contentHelper;
        private readonly IMonitor _monitor;
        private bool _changed;

        public BundleController(IContentHelper contentHelper, IMonitor monitor)
        {
            _contentHelper = contentHelper;
            _monitor = monitor;
        }

        /// <summary>Checks if bundles are not in sync with the netWorldState</summary>
        private bool IsChanged
        {
            get
            {
                if (_changed)
                    return true;

                var bundlePath = PathUtilities.NormalizePath("Data/Bundles");
                _contentHelper.InvalidateCache(bundlePath);
                var bundles = Game1.content.Load<Dictionary<string, string>>(bundlePath);
                _changed = !bundles.All(bundle =>
                    Game1.netWorldState.Value.BundleData.ContainsKey(bundle.Key)
                    && bundle.Value.Equals(Game1.netWorldState.Value.BundleData[bundle.Key]));
                return _changed;
            }
            set => _changed = value;
        }

        /// <summary>Get whether this instance can edit the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            var assetName = PathUtilities.NormalizePath(asset.AssetName);
            var bundlePath = PathUtilities.NormalizePath("Data/Bundles");
            return assetName.Equals(bundlePath);
        }

        /// <summary>Edit a matched asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public void Edit<T>(IAssetData asset)
        {
            if (!Context.IsWorldReady)
                return;

            var modPath = PathUtilities.NormalizePath("Mods/CustomBundles");
            var bundles = asset.AsDictionary<string, string>().Data;
            var customBundles = _contentHelper
                .Load<Dictionary<string, Bundle>>(modPath, ContentSource.GameContent)
                .Where(bundle => bundle.Value.HasData);

            foreach (var bundle in customBundles)
            {
                bundles[bundle.Key] = bundle.Value.GetData;
            }
        }

        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            var assetName = PathUtilities.NormalizePath(asset.AssetName);
            var modPath = PathUtilities.NormalizePath("Mods/CustomBundles");
            return assetName.StartsWith(modPath);
        }

        /// <summary>Load a matched asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        /// <returns></returns>
        public T Load<T>(IAssetInfo asset)
        {
            return (T) (object) new Dictionary<string, Bundle>();
        }

        /// <summary>Merges bundle data into netWorldState</summary>
        public void Merge()
        {
            if (!IsChanged)
                return;
            IsChanged = false;

            _monitor.Log("Merging bundles");

            var bundlePath = PathUtilities.NormalizePath("Data/Bundles");
            var bundles = Game1.content.Load<Dictionary<string, string>>(bundlePath);
            Game1.netWorldState.Value.SetBundleData(bundles);

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