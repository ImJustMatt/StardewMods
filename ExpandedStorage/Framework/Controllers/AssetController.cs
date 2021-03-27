using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class AssetController : IAssetLoader, IAssetEditor
    {
        private readonly ExpandedStorage _mod;

        /// <summary>Dictionary of Expanded Storage configs</summary>
        internal readonly IDictionary<string, StorageController> Storages = new Dictionary<string, StorageController>();

        /// <summary>Dictionary of Expanded Storage tabs</summary>
        internal readonly IDictionary<string, TabController> Tabs = new Dictionary<string, TabController>();

        private bool _isContentLoaded;

        internal AssetController(ExpandedStorage mod)
        {
            _mod = mod;

            // Events
            _mod.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            _mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
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
            return asset.AssetName.StartsWith(PathUtilities.NormalizePath("Mods/furyx639.ExpandedStorage/"));
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
                        || !Storages.TryGetValue(storageName, out var storage)
                        || storage.Texture == null) throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) storage.Texture.Invoke();
                case "Tabs":
                    var tabId = $"{assetParts.ElementAtOrDefault(1)}/{assetParts.ElementAtOrDefault(2)}";
                    if (!Tabs.TryGetValue(tabId, out var tab))
                        throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
                    return (T) (object) tab.Texture?.Invoke() ?? _mod.Helper.Content.Load<T>($"assets/{tab.TabImage}");
            }

            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        /// <summary>Returns Storage by object context.</summary>
        internal bool TryGetStorage(object context, out StorageController storage)
        {
            if (context is Item item && item.modData.TryGetValue("furyx639.ExpandedStorage/Storage", out var key) && Storages.TryGetValue(key, out storage)) return true;
            storage = Storages
                .Select(c => c.Value)
                .FirstOrDefault(c => c.MatchesContext(context));
            return storage != null;
        }

        /// <summary>Returns ExpandedStorageTab by tab name.</summary>
        internal TabController GetTab(string modUniqueId, string tabName)
        {
            return Tabs
                .Where(t => t.Key.EndsWith($"/{tabName}"))
                .Select(t => t.Value)
                .OrderByDescending(t => t.ModUniqueId.Equals(modUniqueId))
                .ThenByDescending(t => t.ModUniqueId.Equals("furyx639.ExpandedStorage"))
                .FirstOrDefault();
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
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
            foreach (var storageTab in _mod.Config.DefaultTabs)
            {
                var tabId = $"{_mod.ModManifest.UniqueID}/{storageTab.Key}";
                var tab = new TabController(storageTab.Value)
                {
                    ModUniqueId = _mod.ModManifest.UniqueID,
                    Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}",
                    TabName = _mod.Helper.Translation.Get(storageTab.Key).Default(storageTab.Key)
                };
                Tabs.Add(tabId, tab);
            }

            _isContentLoaded = true;
        }

        /// <summary>Raised after a new in-game day starts, or after connecting to a multiplayer world.</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            foreach (var sprite in Storages.Values.Where(s => s.StorageSprite != null).Select(s => s.StorageSprite))
            {
                sprite.InvalidateCache();
            }
        }

        /// <summary>Load Json Assets Ids.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnIdsLoaded(object sender, EventArgs e)
        {
            foreach (var storage in Storages)
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

            foreach (var storage in Storages.Where(storage => storage.Value.Source != StorageController.SourceType.JsonAssets))
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

            foreach (var bigCraftable in bigCraftables.Where(data => !Storages.ContainsKey(data.Value)))
            {
                var defaultStorage = new StorageController(bigCraftable.Value)
                {
                    ModUniqueId = _mod.ModManifest.UniqueID,
                    Config = new StorageConfigController(_mod.Config.DefaultStorage),
                    Source = StorageController.SourceType.Vanilla
                };
                defaultStorage.ObjectIds.Add(bigCraftable.Key);
                Storages.Add(bigCraftable.Value, defaultStorage);
                _mod.Monitor.Log(string.Join("\n",
                    $"{bigCraftable.Value} Config:",
                    StorageController.ConfigHelper.Summary(defaultStorage),
                    StorageConfigController.ConfigHelper.Summary(defaultStorage.Config, false)
                ), _mod.Config.LogLevelProperty);
            }
        }

        internal static void RegisterModConfig(
            IContentPack contentPack,
            GenericModConfigMenuIntegration modConfigMenu,
            IDictionary<string, StorageModel> expandedStorages,
            Dictionary<string, StorageConfigController> playerConfigs)
        {
            if (!modConfigMenu.IsLoaded || !expandedStorages.Values.Any(expandedStorage => expandedStorage.PlayerConfig))
                return;

            void RevertToDefault()
            {
                foreach (var playerConfig in playerConfigs)
                {
                    if (!expandedStorages.TryGetValue(playerConfig.Key, out var expandedStorage)) continue;
                    playerConfig.Value.Capacity = expandedStorage.Capacity;
                    playerConfig.Value.Tabs = new List<string>(expandedStorage.Tabs);
                    playerConfig.Value.EnabledFeatures = new HashSet<string>(expandedStorage.EnabledFeatures);
                    playerConfig.Value.DisabledFeatures = new HashSet<string>(expandedStorage.DisabledFeatures);
                }
            }

            void SaveToFile()
            {
                contentPack.WriteJsonFile("config.json", playerConfigs);
            }

            modConfigMenu.API.RegisterModConfig(contentPack.Manifest, RevertToDefault, SaveToFile);
            modConfigMenu.API.SetDefaultIngameOptinValue(contentPack.Manifest, true);
            modConfigMenu.API.RegisterLabel(contentPack.Manifest, contentPack.Manifest.Name, "");
            modConfigMenu.API.RegisterParagraph(contentPack.Manifest, contentPack.Manifest.Description);
            foreach (var expandedStorage in expandedStorages.Where(expandedStorage => expandedStorage.Value.PlayerConfig))
            {
                modConfigMenu.API.RegisterPageLabel(
                    contentPack.Manifest,
                    expandedStorage.Key,
                    "",
                    expandedStorage.Key
                );
            }
        }
    }
}