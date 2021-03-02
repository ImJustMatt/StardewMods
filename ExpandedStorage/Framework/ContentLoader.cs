using System;
using System.Linq;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework
{
    internal class ContentLoader
    {
        private readonly ModConfig _config;
        private readonly IExpandedStorageAPI _expandedStorageAPI;
        private readonly IModHelper _helper;
        private readonly IManifest _manifest;
        private readonly IMonitor _monitor;

        internal ContentLoader(IModHelper helper,
            IManifest manifest,
            IMonitor monitor,
            ModConfig config,
            IExpandedStorageAPI expandedStorageAPI)
        {
            _helper = helper;
            _manifest = manifest;
            _monitor = monitor;
            _config = config;

            _expandedStorageAPI = expandedStorageAPI;

            // Default Exclusions
            _expandedStorageAPI.DisableWithModData("aedenthorn.AdvancedLootFramework/IsAdvancedLootFrameworkChest");
            _expandedStorageAPI.DisableDrawWithModData("aedenthorn.CustomChestTypes/IsCustomChest");

            // Events
            _expandedStorageAPI.ReadyToLoad += OnReadyToLoad;
            _expandedStorageAPI.StoragesLoaded += OnStoragesLoaded;
        }

        /// <summary>Load Expanded Storage Content Packs.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReadyToLoad(object sender, EventArgs e)
        {
            var contentPacks = _helper.ContentPacks.GetOwned();
            _monitor.Log("Loading Expanded Storage Content", LogLevel.Info);
            foreach (var contentPack in contentPacks)
            {
                _expandedStorageAPI.LoadContentPack(contentPack);
            }

            // Load Default Tabs
            foreach (var storageTab in _config.DefaultTabs)
            {
                // Localized Tab Name
                storageTab.Value.TabName = _helper.Translation.Get(storageTab.Key).Default(storageTab.Key);

                _expandedStorageAPI.RegisterStorageTab(_manifest, storageTab.Key, storageTab.Value);
            }
        }

        /// <summary>Load Vanilla Storages with default config.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStoragesLoaded(object sender, EventArgs e)
        {
            var expandedStorages = _expandedStorageAPI.GetAllStorages();
            var bigCraftables = Game1.bigCraftablesInformation.Where(Storage.IsVanillaStorage);
            foreach (var bigCraftable in bigCraftables)
            {
                var data = bigCraftable.Value.Split('/').ToArray();
                if (expandedStorages.Any(data[0].Equals))
                    continue;

                _expandedStorageAPI.RegisterStorage(_manifest, data[0], new Storage(data[0]));
                _expandedStorageAPI.SetStorageConfig(_manifest, data[0], _config.DefaultStorage);
            }
        }

        internal void ReloadDefaultStorageConfigs()
        {
            var storageNames = _expandedStorageAPI.GetOwnedStorages(_manifest);
            foreach (var storageName in storageNames)
            {
                _expandedStorageAPI.SetStorageConfig(_manifest, storageName, _config.DefaultStorage);
            }
        }
    }
}