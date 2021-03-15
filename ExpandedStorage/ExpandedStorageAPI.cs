using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.Patches;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage
{
    public class ExpandedStorageAPI : IExpandedStorageAPI
    {
        private readonly IModHelper _helper;
        private readonly JsonAssetsIntegration _jsonAssets;
        private readonly GenericModConfigMenuIntegration _modConfigMenu;
        private readonly IMonitor _monitor;
        private readonly IDictionary<string, Storage> _storages;
        private readonly IDictionary<string, StorageTab> _storageTabs;

        internal ExpandedStorageAPI(
            IModHelper helper,
            IMonitor monitor,
            IDictionary<string, Storage> storages,
            IDictionary<string, StorageTab> storageTabs,
            GenericModConfigMenuIntegration modConfigMenu,
            JsonAssetsIntegration jsonAssets)
        {
            _helper = helper;
            _monitor = monitor;
            _storages = storages;
            _storageTabs = storageTabs;
            _modConfigMenu = modConfigMenu;
            _jsonAssets = jsonAssets;
        }

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
            return _storages.Keys.ToList();
        }

        public IList<string> GetOwnedStorages(IManifest manifest)
        {
            return _storages
                .Where(storageConfig => storageConfig.Value.ModUniqueId == manifest.UniqueID)
                .Select(storageConfig => storageConfig.Key)
                .ToList();
        }

        public bool TryGetStorage(string storageName, out IStorage storage)
        {
            if (_storages.TryGetValue(storageName, out var foundStorage))
            {
                storage = new Storage(storageName, foundStorage);
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
            var playerConfigs = contentPack.ReadJsonFile<Dictionary<string, StorageConfig>>("config.json") ?? new Dictionary<string, StorageConfig>();

            if (expandedStorages == null)
            {
                _monitor.Log($"Nothing to load from {contentPack.Manifest.Name} {contentPack.Manifest.Version}");
                return false;
            }

            // Load default expanded storage config if specified
            StorageConfig parentConfig = null;
            if (expandedStorages.TryGetValue("DefaultStorage", out var expandedStorageDefault))
            {
                parentConfig = new StorageConfig(expandedStorageDefault);
                expandedStorages.Remove("DefaultStorage");
            }

            if (_modConfigMenu.IsLoaded && expandedStorages.Values.Any(xs => xs.PlayerConfig))
            {
                // Register Generic Mod Config Menu
                void RevertToDefault()
                {
                    foreach (var playerConfig in playerConfigs.Values)
                        playerConfig.RevertToDefault();
                }

                void SaveToFile()
                {
                    contentPack.WriteJsonFile("config.json", playerConfigs);
                }

                _modConfigMenu.API.RegisterModConfig(contentPack.Manifest, RevertToDefault, SaveToFile);
                _modConfigMenu.API.RegisterLabel(contentPack.Manifest, contentPack.Manifest.Name, "");
                _modConfigMenu.API.RegisterParagraph(contentPack.Manifest, contentPack.Manifest.Description);
                foreach (var xs in expandedStorages.Where(xs => xs.Value.PlayerConfig))
                {
                    _modConfigMenu.API.RegisterPageLabel(
                        contentPack.Manifest,
                        xs.Key,
                        "",
                        xs.Key
                    );
                }
            }

            // Load expanded storages
            foreach (var xs in expandedStorages)
            {
                // Skip duplicate storage configs
                if (_storages.ContainsKey(xs.Key))
                {
                    _monitor.Log($"Duplicate storage {xs.Key} in {contentPack.Manifest.UniqueID}.", LogLevel.Warn);
                    continue;
                }

                // Register new storage
                var expandedStorage = new Storage(xs.Key, xs.Value)
                {
                    ModUniqueId = contentPack.Manifest.UniqueID,
                    Path = $"Mods/furyx639.ExpandedStorage/SpriteSheets/{xs.Key}",
                    Texture = !string.IsNullOrWhiteSpace(xs.Value.Image) && contentPack.HasFile($"assets/{xs.Value.Image}")
                        ? contentPack.LoadAsset<Texture2D>($"assets/{xs.Value.Image}")
                        : null
                };
                _storages.Add(xs.Key, expandedStorage);

                // Register storage configuration
                var defaultConfig = new StorageConfig(xs.Value)
                {
                    ParentConfig = parentConfig
                };
                if (expandedStorage.PlayerConfig)
                {
                    if (!playerConfigs.TryGetValue(xs.Key, out var playerConfig))
                    {
                        // Generate default player config
                        playerConfig = new StorageConfig(defaultConfig);
                        playerConfigs.Add(xs.Key, playerConfig);
                    }

                    playerConfig.ParentConfig = defaultConfig;
                    expandedStorage.Config = playerConfig;

                    if (!_modConfigMenu.IsLoaded)
                        continue;

                    // Add Expanded Storage to Generic Mod Config Menu
                    _modConfigMenu.API.StartNewPage(contentPack.Manifest, xs.Key);
                    _modConfigMenu.API.RegisterLabel(contentPack.Manifest, xs.Key, "");
                    _modConfigMenu.RegisterConfigOptions(contentPack.Manifest, StorageConfig.ConfigHelper, playerConfig);
                    _modConfigMenu.API.RegisterPageLabel(contentPack.Manifest, "Go Back", "", "");
                }
                else
                {
                    expandedStorage.Config = defaultConfig;
                }

                _monitor.Log(string.Join("\n",
                    $"{xs.Key} Config:",
                    Storage.ConfigHelper.Summary(expandedStorage),
                    StorageConfig.ConfigHelper.Summary(expandedStorage.Config, false)
                ), LogLevel.Debug);
            }

            // Load expanded storage tabs
            if (storageTabs != null)
            {
                foreach (var xsTab in storageTabs)
                {
                    var tabId = $"{contentPack.Manifest.UniqueID}/{xsTab.Key}";
                    var storageTab = new StorageTab(xsTab.Value)
                    {
                        ModUniqueId = contentPack.Manifest.UniqueID,
                        Path = $"Mods/furyx639.ExpandedStorage/Tabs/{tabId}",
                        TabName = contentPack.Translation.Get(xsTab.Key).Default(xsTab.Key),
                        Texture = !string.IsNullOrWhiteSpace(xsTab.Value.TabImage) && contentPack.HasFile($"assets/{xsTab.Value.TabImage}")
                            ? contentPack.LoadAsset<Texture2D>($"assets/{xsTab.Value.TabImage}")
                            : null
                    };
                    _storageTabs.Add(tabId, storageTab);
                }
            }

            // Generate file for Json Assets
            if (expandedStorages.Keys.All(Storage.VanillaNames.Contains))
                return true;

            // Generate content.json for Json Assets
            contentPack.WriteJsonFile("content-pack.json", new ContentPack
            {
                Author = contentPack.Manifest.Author,
                Description = contentPack.Manifest.Description,
                Name = contentPack.Manifest.Name,
                UniqueID = contentPack.Manifest.UniqueID,
                UpdateKeys = contentPack.Manifest.UpdateKeys,
                Version = contentPack.Manifest.Version.ToString()
            });
            if (_jsonAssets.IsLoaded)
                _jsonAssets.API.LoadAssets(contentPack.DirectoryPath);
            return true;
        }
    }
}