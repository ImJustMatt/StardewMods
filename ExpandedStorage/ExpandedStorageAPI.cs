using System.Collections.Generic;
using System.Linq;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage
{
    public class ExpandedStorageAPI : IExpandedStorageAPI
    {
        private readonly ExpandedStorage _mod;

        internal ExpandedStorageAPI(ExpandedStorage mod)
        {
            _mod = mod;
        }

        private AssetController AssetController => _mod.AssetController;

        public void DisableWithModData(string modDataKey)
        {
            StorageController.AddExclusion(modDataKey);
        }

        public void DisableDrawWithModData(string modDataKey)
        {
            ChestPatches.AddExclusion(modDataKey);
            ObjectPatches.AddExclusion(modDataKey);
        }

        public IList<string> GetAllStorages()
        {
            return AssetController.Storages.Keys.ToList();
        }

        public IList<string> GetOwnedStorages(IManifest manifest)
        {
            return AssetController.Storages
                .Where(storageConfig => storageConfig.Value.ModUniqueId == manifest.UniqueID)
                .Select(storageConfig => storageConfig.Key)
                .ToList();
        }

        public bool TryGetStorage(string storageName, out IStorage storage)
        {
            if (AssetController.Storages.TryGetValue(storageName, out var foundStorage))
            {
                storage = new StorageController(storageName, foundStorage);
                return true;
            }

            storage = null;
            return false;
        }

        public bool AcceptsItem(Chest chest, Item item)
        {
            return !AssetController.TryGetStorage(chest, out var storage) || storage.Filter(item);
        }

        public bool LoadContentPack(string path)
        {
            var temp = _mod.Helper.ContentPacks.CreateFake(path);
            var info = temp.ReadJsonFile<ContentModel>("content-pack.json");

            if (info == null)
            {
                _mod.Monitor.Log($"Cannot read content-data.json from {path}", LogLevel.Warn);
                return false;
            }

            var contentPack = _mod.Helper.ContentPacks.CreateTemporary(
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
            _mod.Monitor.Log($"Loading {contentPack.Manifest.Name} {contentPack.Manifest.Version}", LogLevel.Info);

            var expandedStorages = contentPack.ReadJsonFile<IDictionary<string, StorageModel>>("expanded-storage.json");
            if (expandedStorages == null)
            {
                _mod.Monitor.Log($"Nothing to load from {contentPack.Manifest.Name} {contentPack.Manifest.Version}", LogLevel.Warn);
                return false;
            }

            var storageTabs = contentPack.ReadJsonFile<IDictionary<string, TabModel>>("storage-tabs.json");
            var configs = contentPack.ReadJsonFile<Dictionary<string, StorageConfigModel>>("config.json") ?? new Dictionary<string, StorageConfigModel>();
            var playerConfigs = new Dictionary<string, StorageConfigController>();

            // Register Generic Mod Config Menu for Content Pack
            AssetController.RegisterModConfig(contentPack, _mod.ModConfigMenu, expandedStorages, playerConfigs);

            // Load default expanded storage config if specified
            StorageConfigController parentConfig = null;
            if (expandedStorages.TryGetValue("DefaultStorage", out var expandedStorageDefault))
            {
                parentConfig = new StorageConfigController(expandedStorageDefault);
                expandedStorages.Remove("DefaultStorage");
            }

            // Load expanded storages
            foreach (var expandedStorage in expandedStorages)
            {
                // Skip duplicate storage configs
                if (AssetController.Storages.ContainsKey(expandedStorage.Key))
                {
                    _mod.Monitor.Log($"Duplicate storage {expandedStorage.Key} in {contentPack.Manifest.UniqueID}.", LogLevel.Warn);
                    continue;
                }

                // Register new storage
                var storage = new StorageController(expandedStorage.Key, expandedStorage.Value)
                {
                    ModUniqueId = contentPack.Manifest.UniqueID,
                    Path = $"Mods/furyx639.ExpandedStorage/SpriteSheets/{expandedStorage.Key}",
                    Texture = !string.IsNullOrWhiteSpace(expandedStorage.Value.Image) && contentPack.HasFile($"assets/{expandedStorage.Value.Image}")
                        ? () => contentPack.LoadAsset<Texture2D>($"assets/{expandedStorage.Value.Image}")
                        : null
                };
                AssetController.Storages.Add(expandedStorage.Key, storage);

                // Register storage configuration
                var defaultConfig = new StorageConfigController(expandedStorage.Value)
                {
                    ParentConfig = parentConfig
                };
                if (storage.PlayerConfig)
                {
                    var playerConfig = new StorageConfigController(configs.TryGetValue(expandedStorage.Key, out var config) ? config : defaultConfig)
                    {
                        ParentConfig = defaultConfig
                    };
                    storage.Config = playerConfig;
                    playerConfigs.Add(expandedStorage.Key, playerConfig);
                    playerConfig.RegisterModConfig(expandedStorage.Key, contentPack.Manifest, _mod.ModConfigMenu);
                }
                else
                {
                    storage.Config = defaultConfig;
                }

                storage.Log(expandedStorage.Key, _mod.Monitor, _mod.Config.LogLevelProperty);
            }

            // Load expanded storage tabs
            if (storageTabs != null)
            {
                foreach (var storageTab in storageTabs)
                {
                    var tabId = $"{contentPack.Manifest.UniqueID}/{storageTab.Key}";
                    var tab = new TabController(storageTab.Value)
                    {
                        ModUniqueId = contentPack.Manifest.UniqueID,
                        Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}",
                        TabName = contentPack.Translation.Get(storageTab.Key).Default(storageTab.Key),
                        Texture = !string.IsNullOrWhiteSpace(storageTab.Value.TabImage) && contentPack.HasFile($"assets/{storageTab.Value.TabImage}")
                            ? () => contentPack.LoadAsset<Texture2D>($"assets/{storageTab.Value.TabImage}")
                            : null
                    };
                    AssetController.Tabs.Add(tabId, tab);
                }
            }

            // Generate file for Json Assets
            if (expandedStorages.Keys.All(StorageController.VanillaNames.Contains))
                return true;

            // Generate content.json for Json Assets
            contentPack.WriteJsonFile("content-pack.json", new ContentModel
            {
                Author = contentPack.Manifest.Author,
                Description = contentPack.Manifest.Description,
                Name = contentPack.Manifest.Name,
                UniqueID = contentPack.Manifest.UniqueID,
                UpdateKeys = contentPack.Manifest.UpdateKeys,
                Version = contentPack.Manifest.Version.ToString()
            });
            if (_mod.JsonAssets.IsLoaded)
                _mod.JsonAssets.API.LoadAssets(contentPack.DirectoryPath);
            return true;
        }
    }
}