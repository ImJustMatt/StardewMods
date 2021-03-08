using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.ExpandedStorage
{
    public class ExpandedStorageAPI : IExpandedStorageAPI, IAssetLoader, IAssetEditor
    {
        private readonly IList<string> _contentDirs = new List<string>();
        private readonly IDictionary<string, IContentPack> _contentPacks = new Dictionary<string, IContentPack>();
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IDictionary<string, Storage> _storageConfigs;
        private readonly IDictionary<string, StorageTab> _tabConfigs;

        private bool _isContentLoaded;
        private JsonAssetsIntegration _jsonAssets;
        private GenericModConfigMenuIntegration _modConfigMenu;

        internal ExpandedStorageAPI(
            IModHelper helper,
            IMonitor monitor,
            IDictionary<string, Storage> storageConfigs,
            IDictionary<string, StorageTab> tabConfigs)
        {
            _helper = helper;
            _monitor = monitor;
            _storageConfigs = storageConfigs;
            _tabConfigs = tabConfigs;

            // Events
            _helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals("Data/BigCraftablesInformation"))
            {
                // Load bigCraftable on next tick for vanilla storages
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
            var assetPrefix = PathUtilities.NormalizePath("Mods/furyx639.ExpandedStorage");
            return asset.AssetName.StartsWith(assetPrefix);
        }

        /// <summary>Provide base versions of ExpandedStorage assets</summary>
        public T Load<T>(IAssetInfo asset)
        {
            var assetParts = PathUtilities.GetSegments(asset.AssetName).Skip(2).ToList();
            IContentPack contentPack = null;

            switch (assetParts.ElementAtOrDefault(0))
            {
                case "SpriteSheets":
                    var storageName = assetParts.ElementAtOrDefault(1);

                    if (string.IsNullOrWhiteSpace(storageName)
                        || !_storageConfigs.TryGetValue(storageName, out var storage)
                        || !_contentPacks.TryGetValue(storage.ModUniqueId, out contentPack)
                        || !contentPack.HasFile($"assets/{storage.Image}"))
                        throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return contentPack.LoadAsset<T>($"assets/{storage.Image}");

                case "Tabs":
                    var tabId = $"{assetParts.ElementAtOrDefault(1)}/{assetParts.ElementAtOrDefault(2)}";

                    if (!_tabConfigs.TryGetValue(tabId, out var tab))
                        throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return _contentPacks.TryGetValue(tab.ModUniqueId, out contentPack) && contentPack.HasFile($"assets/{tab.TabImage}")
                        ? contentPack.LoadAsset<T>($"assets/{tab.TabImage}")
                        : _helper.Content.Load<T>($"assets/{tab.TabImage}");
            }

            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        public event EventHandler ReadyToLoad;
        public event EventHandler StoragesLoaded;

        public void DisableWithModData(string modDataKey)
        {
            Storage.AddExclusion(modDataKey);
        }

        public void DisableDrawWithModData(string modDataKey)
        {
            ChestPatch.AddExclusion(modDataKey);
            ObjectPatch.AddExclusion(modDataKey);
        }

        public IList<string> GetAllStorages()
        {
            return _storageConfigs.Keys.ToList();
        }

        public IList<string> GetOwnedStorages(IManifest manifest)
        {
            return _storageConfigs
                .Where(storageConfig => storageConfig.Value.ModUniqueId == manifest.UniqueID)
                .Select(storageConfig => storageConfig.Key)
                .ToList();
        }

        public bool TryGetStorage(string storageName, out IStorage storage)
        {
            if (_storageConfigs.TryGetValue(storageName, out var storageConfig))
            {
                storage = Storage.Clone(storageConfig);
                return true;
            }

            storage = null;
            return false;
        }

        public bool LoadContentPack(string path)
        {
            var temp = _helper.ContentPacks.CreateFake(path);
            var info = temp.ReadJsonFile<ContentPack>("content-pack.json");

            if (info == null)
            {
                _monitor.Log($"Cannot read content-data.json from {path}", LogLevel.Warn);
                return false;
            }

            var contentPack = _helper.ContentPacks.CreateTemporary(
                path,
                info.UniqueID,
                info.Name,
                info.Description,
                info.Author,
                new SemanticVersion(info.Version));

            return LoadContentPack(contentPack);
        }

        public bool LoadContentPack(IContentPack contentPack)
        {
            _monitor.Log($"Loading {contentPack.Manifest.Name} {contentPack.Manifest.Version}", LogLevel.Info);

            var expandedStorages = contentPack.ReadJsonFile<IDictionary<string, Storage>>("expanded-storage.json");
            var storageTabs = contentPack.ReadJsonFile<IDictionary<string, StorageTab>>("storage-tabs.json");

            if (expandedStorages == null)
            {
                _monitor.Log($"Nothing to load from {contentPack.Manifest.Name} {contentPack.Manifest.Version}");
                return false;
            }

            var playerConfigs = contentPack.ReadJsonFile<Dictionary<string, StorageConfig>>("config.json")
                                ?? new Dictionary<string, StorageConfig>();
            var defaultConfigs = new Dictionary<string, StorageConfig>();

            void RevertToDefault()
            {
                foreach (var defaultConfig in defaultConfigs)
                    if (playerConfigs.TryGetValue(defaultConfig.Key, out var playerConfig))
                        playerConfig.CopyFrom(defaultConfig.Value);
            }

            void SaveToFile()
            {
                foreach (var playerConfig in playerConfigs)
                    if (_storageConfigs.TryGetValue(playerConfig.Key, out var storage) && storage.ModUniqueId == contentPack.Manifest.UniqueID)
                        storage.CopyFrom(playerConfig.Value);
                contentPack.WriteJsonFile("config.json", playerConfigs);
            }

            if (expandedStorages.Any(config => config.Value.PlayerConfig))
            {
                _modConfigMenu.API?.RegisterModConfig(contentPack.Manifest, RevertToDefault, SaveToFile);
            }

            // Load default if specified
            if (expandedStorages.TryGetValue("DefaultStorage", out var defaultStorage))
            {
                expandedStorages.Remove("DefaultStorage");
            }

            // Load expanded storages
            foreach (var expandedStorage in expandedStorages)
            {
                var defaultConfig = new Storage(expandedStorage.Key);

                if (defaultStorage != null)
                    defaultConfig.CopyFrom(defaultStorage);

                defaultConfig.CopyFrom(expandedStorage.Value);

                defaultConfigs.Add(expandedStorage.Key, defaultConfig);
                RegisterStorage(contentPack.Manifest, expandedStorage.Key, defaultConfig);

                if (!defaultConfig.PlayerConfig)
                    continue;

                if (!playerConfigs.TryGetValue(expandedStorage.Key, out var playerConfig))
                {
                    // Generate default player config
                    playerConfig = new StorageConfig();
                    playerConfig.CopyFrom(defaultConfig);
                    playerConfigs.Add(expandedStorage.Key, playerConfig);
                }

                SetStorageConfig(contentPack.Manifest, expandedStorage.Key, playerConfig);
                if (_modConfigMenu.IsLoaded)
                    RegisterConfig(contentPack.Manifest, expandedStorage.Key, playerConfig);
            }

            // Add asset loader
            _contentPacks.Add(contentPack.Manifest.UniqueID, contentPack);

            // Generate file for Json Assets
            if (_jsonAssets != null && !expandedStorages.Keys.All(Storage.VanillaNames.Contains))
            {
                // Generate content-pack.json
                contentPack.WriteJsonFile("content-pack.json", new ContentPack
                {
                    Author = contentPack.Manifest.Author,
                    Description = contentPack.Manifest.Description,
                    Name = contentPack.Manifest.Name,
                    UniqueID = contentPack.Manifest.UniqueID,
                    UpdateKeys = contentPack.Manifest.UpdateKeys,
                    Version = contentPack.Manifest.Version.ToString()
                });

                _contentDirs.Add(contentPack.DirectoryPath);
            }

            if (storageTabs == null)
                return true;

            // Load expanded storage tabs
            foreach (var storageTab in storageTabs)
            {
                // Localized Tab Name
                storageTab.Value.TabName = contentPack.Translation.Get(storageTab.Key).Default(storageTab.Key);

                RegisterStorageTab(contentPack.Manifest, storageTab.Key, storageTab.Value);
            }

            return true;
        }

        public void SetStorageConfig(IManifest manifest, string storageName, IStorageConfig config)
        {
            if (!_storageConfigs.TryGetValue(storageName, out var storage) || storage.ModUniqueId != manifest.UniqueID)
            {
                _monitor.Log($"Unknown storage {storageName} in {manifest.UniqueID}.", LogLevel.Warn);
                return;
            }

            storage.CopyFrom(config);
            _monitor.Log($"{storageName} Config:\n{storage.StorageSummary}", LogLevel.Debug);
        }

        public void RegisterStorage(IManifest manifest, string storageName, IStorage storage)
        {
            // Skip duplicate storage configs
            if (_storageConfigs.TryGetValue(storageName, out var storageConfig) && storageConfig.ModUniqueId != manifest.UniqueID)
            {
                _monitor.Log($"Duplicate storage {storageName} in {manifest.UniqueID}.", LogLevel.Warn);
                return;
            }

            // Update existing storage
            if (storageConfig != null)
            {
                storageConfig.CopyFrom(storage);
                return;
            }

            // Add new storage
            storageConfig = new Storage(storageName);
            storageConfig.CopyFrom(storage);
            storageConfig.ModUniqueId = manifest.UniqueID;
            storageConfig.Path = $"Mods/furyx639.ExpandedStorage/SpriteSheets/{storageName}";
            _storageConfigs.Add(storageName, storageConfig);
        }

        public void RegisterStorageTab(IManifest manifest, string tabName, IStorageTab storageTab)
        {
            var tabId = $"{manifest.UniqueID}/{tabName}";
            if (_tabConfigs.TryGetValue(tabId, out var tabConfig))
            {
                tabConfig.CopyFrom(storageTab);
                tabConfig.ModUniqueId = manifest.UniqueID;
            }
            else
            {
                var tab = new StorageTab();
                tab.CopyFrom(storageTab);
                tab.ModUniqueId = manifest.UniqueID;
                tab.Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}";
                _tabConfigs.Add(tabId, tab);
            }
        }

        private void RegisterConfig(IManifest manifest, string storageName, StorageConfig config)
        {
            var optionChoices = Enum.GetNames(typeof(StorageConfig.Choice));

            Func<string> OptionGet(string option)
            {
                return () => config.Option(option).ToString();
            }

            Action<string> OptionSet(string option)
            {
                return value =>
                {
                    if (Enum.TryParse(value, out StorageConfig.Choice choice))
                        config.SetOption(option, choice);
                };
            }

            _modConfigMenu.API?.RegisterLabel(manifest, storageName, manifest.Description);

            _modConfigMenu.API?.RegisterSimpleOption(manifest, "Capacity", "Number of item slots the storage will contain",
                () => config.Capacity,
                value => config.Capacity = value);

            foreach (var option in StorageConfig.StorageOptions)
            {
                _modConfigMenu.API?.RegisterChoiceOption(manifest, option.Key, option.Value,
                    OptionGet(option.Key), OptionSet(option.Key), optionChoices);
            }
        }

        /// <summary>Load Json Asset API</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _modConfigMenu = new GenericModConfigMenuIntegration(_helper.ModRegistry);
            _jsonAssets = new JsonAssetsIntegration(_helper.ModRegistry);
            if (_jsonAssets.IsLoaded)
                _jsonAssets.API.IdsAssigned += OnIdsLoaded;
            else
                _monitor.Log("Json Assets not detected, Expanded Storages content will not be loaded", LogLevel.Warn);
            _helper.Events.GameLoop.UpdateTicked += OnReadyToLoad;
        }

        /// <summary>Load Expanded Storage content packs.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReadyToLoad(object sender, UpdateTickedEventArgs e)
        {
            _helper.Events.GameLoop.UpdateTicked -= OnReadyToLoad;
            InvokeAll(ReadyToLoad);
            foreach (var contentDir in _contentDirs)
                _jsonAssets.API?.LoadAssets(contentDir);
            _isContentLoaded = true;
        }

        /// <summary>Load Json Assets Ids.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnIdsLoaded(object sender, EventArgs e)
        {
            // Clear out old object ids
            foreach (var storageConfig in _storageConfigs
                .Where(config => config.Value.Source == Storage.SourceType.JsonAssets))
                storageConfig.Value.ObjectIds.Clear();

            // Add new object ids
            var bigCraftables = _jsonAssets.API.GetAllBigCraftableIds();
            foreach (var bigCraftable in bigCraftables)
            {
                if (!_storageConfigs.TryGetValue(bigCraftable.Key, out var storageConfig))
                    continue;
                storageConfig.Source = Storage.SourceType.JsonAssets;
                if (!storageConfig.ObjectIds.Contains(bigCraftable.Value))
                    storageConfig.ObjectIds.Add(bigCraftable.Value);
            }
        }

        /// <summary>Load Vanilla Asset Ids.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!_isContentLoaded)
                return;

            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            InvokeAll(StoragesLoaded);

            // Clear out old object ids
            foreach (var storageConfig in _storageConfigs
                .Where(config => config.Value.Source != Storage.SourceType.JsonAssets))
                storageConfig.Value.ObjectIds.Clear();

            var bigCraftables = Game1.bigCraftablesInformation.Where(Storage.IsVanillaStorage);
            foreach (var bigCraftable in bigCraftables)
            {
                var data = bigCraftable.Value.Split('/').ToArray();

                if (!_storageConfigs.TryGetValue(data[0], out var storageConfig))
                    continue;

                if (Storage.VanillaNames.Contains(data[0]))
                    storageConfig.Source = Storage.SourceType.Vanilla;
                else if (bigCraftable.Key >= 424000 && bigCraftable.Key < 425000)
                    storageConfig.Source = Storage.SourceType.CustomChestTypes;

                if (!storageConfig.ObjectIds.Contains(bigCraftable.Key))
                    storageConfig.ObjectIds.Add(bigCraftable.Key);
            }
        }

        private void InvokeAll(EventHandler eventHandler)
        {
            if (eventHandler == null)
                return;

            foreach (var @delegate in eventHandler.GetInvocationList()) @delegate.DynamicInvoke(this, null);
        }
    }
}