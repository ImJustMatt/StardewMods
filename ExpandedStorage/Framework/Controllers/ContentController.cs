using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class ContentController : IAssetLoader, IAssetEditor
    {
        private readonly ExpandedStorage _mod;
        private bool _isContentLoaded;

        internal ContentController(ExpandedStorage mod)
        {
            _mod = mod;

            // Default Exclusions
            _mod.ExpandedStorageAPI.DisableWithModData("aedenthorn.AdvancedLootFramework/IsAdvancedLootFrameworkChest");
            _mod.ExpandedStorageAPI.DisableDrawWithModData("aedenthorn.CustomChestTypes/IsCustomChest");

            // Events
            _mod.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            // Load bigCraftable on next tick for vanilla storages
            if (asset.AssetNameEquals("Data/BigCraftablesInformation"))
            {
                _mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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
                        || !ExpandedStorage.Storages.TryGetValue(storageName, out var storage)
                        || storage.Texture == null) throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) storage.Texture.Invoke();
                case "Tabs":
                    var tabId = $"{assetParts.ElementAtOrDefault(1)}/{assetParts.ElementAtOrDefault(2)}";
                    if (!ExpandedStorage.Tabs.TryGetValue(tabId, out var tab))
                        throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) tab.Texture?.Invoke() ?? _mod.Helper.Content.Load<T>($"assets/{tab.TabImage}");
            }

            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (_mod.JsonAssets.IsLoaded)
                _mod.JsonAssets.API.IdsAssigned += OnIdsLoaded;
            else
                _mod.Monitor.Log("Json Assets not detected, Expanded Storages content will not be loaded", LogLevel.Warn);

            _mod.Monitor.Log("Loading Expanded Storage Content", LogLevel.Info);
            foreach (var contentPack in _mod.Helper.ContentPacks.GetOwned())
            {
                _mod.ExpandedStorageAPI.LoadContentPack(contentPack);
            }

            // Load Default Tabs
            foreach (var xsTab in _mod.Config.DefaultTabs)
            {
                var tabId = $"{_mod.ModManifest.UniqueID}/{xsTab.Key}";
                var storageTab = new TabController(xsTab.Value)
                {
                    ModUniqueId = _mod.ModManifest.UniqueID,
                    Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}",
                    TabName = _mod.Helper.Translation.Get(xsTab.Key).Default(xsTab.Key)
                };
                ExpandedStorage.Tabs.Add(tabId, storageTab);
            }

            _isContentLoaded = true;
        }

        /// <summary>Load Json Assets Ids.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnIdsLoaded(object sender, EventArgs e)
        {
            foreach (var storage in ExpandedStorage.Storages)
            {
                var bigCraftableId = _mod.JsonAssets.API.GetBigCraftableId(storage.Key);
                if (bigCraftableId != -1)
                {
                    storage.Value.ObjectIds.Clear();
                    storage.Value.ObjectIds.Add(bigCraftableId);
                    storage.Value.Source = StorageController.SourceType.JsonAssets;
                }
                else if (storage.Value.Source == StorageController.SourceType.JsonAssets)
                {
                    storage.Value.ObjectIds.Clear();
                    storage.Value.Source = StorageController.SourceType.Unknown;
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

            _mod.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            var bigCraftables = Game1.bigCraftablesInformation
                .Where(StorageController.IsVanillaStorage)
                .Select(data => new KeyValuePair<int, string>(data.Key, data.Value.Split('/')[0]))
                .ToList();

            foreach (var storage in ExpandedStorage.Storages.Where(storage => storage.Value.Source != StorageController.SourceType.JsonAssets))
            {
                var bigCraftableIds = bigCraftables
                    .Where(data => data.Value.Equals(storage.Key))
                    .Select(data => data.Key)
                    .ToList();

                storage.Value.ObjectIds.Clear();
                if (!bigCraftableIds.Any())
                {
                    storage.Value.Source = StorageController.SourceType.Unknown;
                    continue;
                }

                storage.Value.Source = StorageController.SourceType.Vanilla;
                foreach (var bigCraftableId in bigCraftableIds)
                {
                    storage.Value.ObjectIds.Add(bigCraftableId);
                    if (bigCraftableId >= 424000 && bigCraftableId <= 435000)
                    {
                        storage.Value.Source = StorageController.SourceType.CustomChestTypes;
                    }
                }
            }

            foreach (var bigCraftable in bigCraftables.Where(data => !ExpandedStorage.Storages.ContainsKey(data.Value)))
            {
                var defaultStorage = new StorageController(bigCraftable.Value)
                {
                    ModUniqueId = _mod.ModManifest.UniqueID,
                    Config = new StorageConfigController(_mod.Config.DefaultStorage),
                    Source = StorageController.SourceType.Vanilla
                };
                defaultStorage.ObjectIds.Add(bigCraftable.Key);
                ExpandedStorage.Storages.Add(bigCraftable.Value, defaultStorage);
                _mod.Monitor.Log(string.Join("\n",
                    $"{bigCraftable.Value} Config:",
                    StorageController.ConfigHelper.Summary(defaultStorage),
                    StorageConfigController.ConfigHelper.Summary(defaultStorage.Config, false)
                ), _mod.Config.LogLevelProperty);
            }
        }
    }
}