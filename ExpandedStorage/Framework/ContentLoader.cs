using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework
{
    internal class ContentLoader : IAssetLoader, IAssetEditor
    {
        private readonly ModConfig _config;
        private readonly ExpandedStorageAPI _expandedStorageAPI;
        private readonly IModHelper _helper;
        private readonly JsonAssetsIntegration _jsonAssets;
        private readonly IManifest _manifest;
        private readonly IMonitor _monitor;
        private readonly IDictionary<string, Storage> _storages;
        private readonly IDictionary<string, StorageTab> _storageTabs;
        private bool _isContentLoaded;

        internal ContentLoader(IModHelper helper,
            IManifest manifest,
            IMonitor monitor,
            ModConfig config,
            IDictionary<string, Storage> storages,
            IDictionary<string, StorageTab> storageTabs,
            ExpandedStorageAPI expandedStorageAPI,
            JsonAssetsIntegration jsonAssets)
        {
            _helper = helper;
            _manifest = manifest;
            _monitor = monitor;
            _config = config;
            _storages = storages;
            _storageTabs = storageTabs;
            _expandedStorageAPI = expandedStorageAPI;
            _jsonAssets = jsonAssets;

            // Default Exclusions
            _expandedStorageAPI.DisableWithModData("aedenthorn.AdvancedLootFramework/IsAdvancedLootFrameworkChest");
            _expandedStorageAPI.DisableDrawWithModData("aedenthorn.CustomChestTypes/IsCustomChest");

            // Events
            _helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            // Load bigCraftable on next tick for vanilla storages
            if (asset.AssetNameEquals("Data/BigCraftablesInformation"))
            {
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }

            return false;
        }

        /// <summary>Load a matched asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public void Edit<T>(IAssetData asset)
        {
        }

        /// <summary>Load Data for Mods/furyx639.ExpandedStorage path</summary>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetName.StartsWith(PathUtilities.NormalizePath("Mods/furyx639.ExpandedStorage"));
        }

        /// <summary>Provide base versions of ExpandedStorage assets</summary>
        public T Load<T>(IAssetInfo asset)
        {
            var assetParts = PathUtilities.GetSegments(asset.AssetName).Skip(2).ToList();
            switch (assetParts.ElementAtOrDefault(0))
            {
                case "SpriteSheets":
                    var storageName = assetParts.ElementAtOrDefault(1);
                    if (string.IsNullOrWhiteSpace(storageName)
                        || !_storages.TryGetValue(storageName, out var storage)
                        || storage.Texture == null) throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) storage.Texture;
                case "Tabs":
                    var tabId = $"{assetParts.ElementAtOrDefault(1)}/{assetParts.ElementAtOrDefault(2)}";
                    if (!_storageTabs.TryGetValue(tabId, out var tab))
                        throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) tab.Texture ?? _helper.Content.Load<T>($"assets/{tab.TabImage}");
            }

            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (_jsonAssets.IsLoaded)
                _jsonAssets.API.IdsAssigned += OnIdsLoaded;
            else
                _monitor.Log("Json Assets not detected, Expanded Storages content will not be loaded", LogLevel.Warn);

            _monitor.Log("Loading Expanded Storage Content", LogLevel.Info);
            foreach (var contentPack in _helper.ContentPacks.GetOwned())
            {
                _expandedStorageAPI.LoadContentPack(contentPack);
            }

            // Load Default Tabs
            foreach (var xsTab in _config.DefaultTabs)
            {
                var tabId = $"{_manifest.UniqueID}/{xsTab.Key}";
                var storageTab = new StorageTab(xsTab.Value)
                {
                    ModUniqueId = _manifest.UniqueID,
                    Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}",
                    TabName = _helper.Translation.Get(xsTab.Key).Default(xsTab.Key)
                };
                _storageTabs.Add(tabId, storageTab);
            }

            _isContentLoaded = true;
        }

        /// <summary>Load Json Assets Ids.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnIdsLoaded(object sender, EventArgs e)
        {
            foreach (var storage in _storages)
            {
                var bigCraftableId = _jsonAssets.API.GetBigCraftableId(storage.Key);
                if (bigCraftableId != -1)
                {
                    storage.Value.ObjectIds.Clear();
                    storage.Value.ObjectIds.Add(bigCraftableId);
                    storage.Value.Source = Storage.SourceType.JsonAssets;
                }
                else if (storage.Value.Source == Storage.SourceType.JsonAssets)
                {
                    storage.Value.ObjectIds.Clear();
                    storage.Value.Source = Storage.SourceType.Unknown;
                }
            }
        }

        /// <summary>Raised after the game state is updated</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!_isContentLoaded)
                return;

            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            var bigCraftables = Game1.bigCraftablesInformation
                .Where(Storage.IsVanillaStorage)
                .Select(data => new KeyValuePair<int, string>(data.Key, data.Value.Split('/')[0]))
                .ToList();

            foreach (var storage in _storages.Where(storage => storage.Value.Source != Storage.SourceType.JsonAssets))
            {
                var bigCraftableIds = bigCraftables
                    .Where(data => data.Value.Equals(storage.Key))
                    .Select(data => data.Key)
                    .ToList();

                storage.Value.ObjectIds.Clear();
                if (!bigCraftableIds.Any())
                {
                    storage.Value.Source = Storage.SourceType.Unknown;
                    continue;
                }

                storage.Value.Source = Storage.SourceType.Vanilla;
                foreach (var bigCraftableId in bigCraftableIds)
                {
                    storage.Value.ObjectIds.Add(bigCraftableId);
                    if (bigCraftableId >= 424000 && bigCraftableId <= 435000)
                    {
                        storage.Value.Source = Storage.SourceType.CustomChestTypes;
                    }
                }
            }

            foreach (var bigCraftable in bigCraftables.Where(data => !_storages.ContainsKey(data.Value)))
            {
                var defaultStorage = new Storage(bigCraftable.Value)
                {
                    ModUniqueId = _manifest.UniqueID,
                    Config = new StorageConfig(_config.DefaultStorage),
                    Source = Storage.SourceType.Vanilla
                };
                defaultStorage.ObjectIds.Add(bigCraftable.Key);
                _storages.Add(bigCraftable.Value, defaultStorage);
                _monitor.Log(string.Join("\n",
                    $"{bigCraftable.Value} Config:",
                    Storage.ConfigHelper.Summary(defaultStorage),
                    StorageConfig.ConfigHelper.Summary(defaultStorage.Config, false)
                ));
            }
        }
    }
}