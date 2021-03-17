using System;
using System.Collections.Generic;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using Newtonsoft.Json;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class StorageConfig : BaseStorageConfig
    {
        public enum Choice
        {
            Unspecified,
            Enable,
            Disable
        }

        /// <summary>Default storage config for unspecified options</summary>
        private static StorageConfig _defaultConfig;

        internal static readonly ConfigHelper ConfigHelper = new(new FieldHandler(), new StorageConfig(), new List<KeyValuePair<string, string>>
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
            if (config == null)
                return;
            Capacity = config.Capacity;
            EnabledFeatures = new HashSet<string>(config.EnabledFeatures);
            DisabledFeatures = new HashSet<string>(config.DisabledFeatures);
            Tabs = new List<string>(config.Tabs);
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

        internal void SetAsDefault()
        {
            _defaultConfig = this;
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

        private class FieldHandler : ConfigHelper.IFieldHandler
        {
            private static readonly string[] Choices = Enum.GetNames(typeof(Choice));

            public bool CanHandle(ConfigHelper.IField field)
            {
                return !field.Name.Equals("Capacity");
            }

            public object GetValue(object instance, ConfigHelper.IField field)
            {
                return ((StorageConfig) instance).Option(field.Name);
            }

            public void SetValue(object instance, ConfigHelper.IField field, object value)
            {
                var storageConfig = (StorageConfig) instance;
                storageConfig.EnabledFeatures.Remove(field.Name);
                storageConfig.DisabledFeatures.Remove(field.Name);
                if (value is not string stringValue || !Enum.TryParse(stringValue, out Choice choice))
                    return;
                switch (choice)
                {
                    case Choice.Disable:
                        storageConfig.DisabledFeatures.Add(field.Name);
                        break;
                    case Choice.Enable:
                        storageConfig.EnabledFeatures.Add(field.Name);
                        break;
                    case Choice.Unspecified:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, ConfigHelper.IField field)
            {
                modConfigMenu.API.RegisterChoiceOption(
                    manifest,
                    field.Name,
                    field.Description,
                    () => GetValue(instance, field).ToString(),
                    value => SetValue(instance, field, value),
                    Choices
                );
            }
        }
    }
}