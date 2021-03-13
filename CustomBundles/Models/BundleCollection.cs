using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.CustomBundles.Models
{
    internal class BundleCollection : IAssetLoader
    {
        private readonly IContentHelper _contentHelper;

        public BundleCollection(IContentHelper contentHelper)
        {
            _contentHelper = contentHelper;
        }

        public Dictionary<string, string> Bundles { get; private set; } = new();

        public bool Changed
        {
            get
            {
                var bundles = _contentHelper.Load<Dictionary<string, string>>(PathUtilities.NormalizePath("Data\\Bundles"), ContentSource.GameContent);
                foreach (var bundle in _contentHelper.Load<Dictionary<string, Bundle>>("Mods/furyx639.CustomBundles", ContentSource.GameContent).Where(bundle => bundle.Value.HasData))
                {
                    bundles[bundle.Key] = bundle.Value.GetData;
                }

                if (bundles.All(bundle =>
                    Game1.netWorldState.Value.BundleData.ContainsKey(bundle.Key)
                    && bundle.Value.Equals(Game1.netWorldState.Value.BundleData[bundle.Key])))
                {
                    return false;
                }

                Bundles = bundles;
                return true;
            }
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            var assetPrefix = PathUtilities.NormalizePath("Mods/furyx639.CustomBundles");

            return asset.AssetName.StartsWith(assetPrefix);
        }

        public T Load<T>(IAssetInfo asset)
        {
            return (T) (object) new Dictionary<string, Bundle>();
        }
    }
}