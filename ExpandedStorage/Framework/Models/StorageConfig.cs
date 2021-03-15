using System;
using System.Collections.Generic;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using Newtonsoft.Json;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class StorageConfig : IStorageConfig
    {
        public enum Choice
        {
            Unspecified,
            Enable,
            Disable
        }

        /// <summary>Default storage config for unspecified options</summary>
        private static StorageConfig _defaultConfig;

        internal static readonly ConfigHelper ConfigHelper = new(new PropertyHandler(), new StorageConfig(), new List<KeyValuePair<string, string>>
        {
            new("Capacity", "Number of item slots the storage will contain"),
            new("AccessCarried", "Allow storage to be access while carried"),
            new("CanCarry", "Allow storage to be picked up"),
            new("Indestructible", "Cannot be broken by tools even while empty"),
            new("ShowColorPicker", "Show color toggle and bars for colorable storages"),
            new("ShowSearchBar", "Show search bar above chest inventory"),
            new("ShowTabs", "Show tabs below chest inventory"),
            new("VacuumItems", "Allow storage to automatically collect dropped items")
        });

        private StorageConfig _parent;

        [JsonConstructor]
        internal StorageConfig(IStorageConfig config = null)
        {
            if (config != null)
                CopyFrom(config);
        }

        internal static IList<string> DefaultTabs => _defaultConfig?.Tabs;

        /// <summary>Parent storage config for unspecified options</summary>
        internal StorageConfig ParentConfig
        {
            get => _parent ?? _defaultConfig;
            set => _parent = value;
        }

        internal StorageMenu Menu => Capacity == 0 && !ReferenceEquals(ParentConfig, this)
            ? ParentConfig.Menu
            : new StorageMenu(this);

        internal int ActualCapacity =>
            Capacity switch
            {
                0 => ReferenceEquals(ParentConfig, this) ? 0 : ParentConfig?.ActualCapacity ?? 0,
                -1 => int.MaxValue,
                _ => Capacity
            };

        public int Capacity { get; set; }
        public HashSet<string> EnabledFeatures { get; set; } = new() {"CanCarry", "ShowColorPicker", "ShowSearchBar", "ShowTabs"};
        public HashSet<string> DisabledFeatures { get; set; } = new();
        public IList<string> Tabs { get; set; } = new List<string>();

        internal void SetAsDefault()
        {
            _defaultConfig = this;
        }

        internal void RevertToDefault()
        {
            if (ParentConfig != null)
                CopyFrom(ParentConfig);
        }

        internal Choice Option(string option, bool globalOverride = false)
        {
            if (DisabledFeatures.Contains(option))
                return Choice.Disable;
            if (EnabledFeatures.Contains(option))
                return Choice.Enable;
            return globalOverride && !ReferenceEquals(ParentConfig, this)
                ? ParentConfig?.Option(option, true) ?? Choice.Unspecified
                : Choice.Unspecified;
        }

        private void CopyFrom(IStorageConfig config)
        {
            Capacity = config.Capacity;
            EnabledFeatures = config.EnabledFeatures;
            DisabledFeatures = config.DisabledFeatures;
            Tabs = config.Tabs;
        }

        private class PropertyHandler : ConfigHelper.IPropertyHandler
        {
            private static readonly string[] Choices = Enum.GetNames(typeof(Choice));

            public bool CanHandle(ConfigHelper.IProperty property)
            {
                return property.Name != "Capacity";
            }

            public object GetValue(object instance, ConfigHelper.IProperty property)
            {
                return ((StorageConfig) instance).Option(property.Name);
            }

            public void SetValue(object instance, ConfigHelper.IProperty property, object value)
            {
                var storageConfig = (StorageConfig) instance;
                storageConfig.EnabledFeatures.Remove(property.Name);
                storageConfig.DisabledFeatures.Remove(property.Name);
                if (value is not string stringValue || !Enum.TryParse(stringValue, out Choice choice))
                    return;
                switch (choice)
                {
                    case Choice.Disable:
                        storageConfig.DisabledFeatures.Add(property.Name);
                        break;
                    case Choice.Enable:
                        storageConfig.EnabledFeatures.Add(property.Name);
                        break;
                    case Choice.Unspecified:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, ConfigHelper.IProperty property)
            {
                modConfigMenu.API.RegisterChoiceOption(
                    manifest,
                    property.Name,
                    property.Description,
                    () => GetValue(instance, property).ToString(),
                    value => SetValue(instance, property, value),
                    Choices
                );
            }
        }
    }
}