using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Common.Helpers;
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

        internal static readonly ConfigHelper ConfigHelper = new(ValueOfProperty, new List<KeyValuePair<string, string>>
        {
            new("AccessCarried", "Allow storage to be access while carried"),
            new("CanCarry", "Allow storage to be picked up"),
            new("Indestructible", "Cannot be broken by tools even while empty"),
            new("ShowColorPicker", "Show color toggle and bars for colorable storages"),
            new("ShowSearchBar", "Show search bar above chest inventory"),
            new("ShowTabs", "Show tabs below chest inventory"),
            new("VacuumItems", "Allow storage to automatically collect dropped items")
        });

        /// <summary>Default storage settings for unspecified options</summary>
        private static StorageConfig _defaultConfig;

        internal StorageMenu Menu => new(Capacity == 0 ? _defaultConfig : this);
        internal int ActualCapacity => Capacity == 0 ? _defaultConfig.Capacity : Capacity;
        internal static IList<string> DefaultTabs => _defaultConfig?.Tabs;
        public int Capacity { get; set; }
        public HashSet<string> EnabledFeatures { get; set; } = new() {"CanCarry", "ShowColorPicker", "ShowSearchBar", "ShowTabs"};
        public HashSet<string> DisabledFeatures { get; set; } = new();
        public IList<string> Tabs { get; set; } = new List<string>();

        private static object ValueOfProperty(string property, object instance)
        {
            var value = ((StorageConfig) instance).Option(property);
            return value != Choice.Unspecified ? value : null;
        }

        internal void SetDefault()
        {
            _defaultConfig = this;
        }

        internal Choice Option(string option, bool globalOverride = false)
        {
            if (DisabledFeatures.Contains(option))
                return Choice.Disable;
            if (EnabledFeatures.Contains(option))
                return Choice.Enable;
            return globalOverride ? _defaultConfig.Option(option) : Choice.Unspecified;
        }

        internal void SetOption(string option, Choice choice)
        {
            EnabledFeatures.Remove(option);
            DisabledFeatures.Remove(option);
            switch (choice)
            {
                case Choice.Disable:
                    DisabledFeatures.Add(option);
                    break;
                case Choice.Enable:
                    EnabledFeatures.Add(option);
                    break;
            }
        }

        internal void CopyFrom(IStorageConfig config)
        {
            if (config.Capacity != 0) Capacity = config.Capacity;

            if (config.EnabledFeatures != null)
            {
                foreach (var enabledFeature in config.EnabledFeatures)
                {
                    SetOption(enabledFeature, Choice.Enable);
                }
            }

            if (config.DisabledFeatures != null)
            {
                foreach (var disabledFeature in config.DisabledFeatures)
                {
                    SetOption(disabledFeature, Choice.Disable);
                }
            }

            if (config.Tabs == null || !config.Tabs.Any()) return;
            Tabs.Clear();
            foreach (var tab in config.Tabs)
            {
                Tabs.Add(tab);
            }
        }

        internal void RegisterModConfig(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu)
        {
            if (!modConfigMenu.IsLoaded)
                return;

            Func<string> OptionGet(string option) => () => Option(option).ToString();

            Action<string> OptionSet(string option) => value =>
            {
                if (Enum.TryParse(value, out Choice choice)) SetOption(option, choice);
            };

            var optionChoices = Enum.GetNames(typeof(Choice));

            modConfigMenu.API.RegisterSimpleOption(manifest, "Capacity", "Number of item slots the storage will contain",
                () => Capacity,
                value => Capacity = value);

            foreach (var option in ConfigHelper.Properties)
            {
                modConfigMenu.API.RegisterChoiceOption(manifest, option.Key, option.Value,
                    OptionGet(option.Key), OptionSet(option.Key), optionChoices);
            }
        }
    }
}