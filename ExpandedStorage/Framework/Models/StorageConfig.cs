using System.Collections.Generic;
using System.Linq;
using ImJustMatt.ExpandedStorage.API;

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

        public static readonly IDictionary<string, string> StorageOptions = new Dictionary<string, string>
        {
            {"AccessCarried", "Allow storage to be access while carried"},
            {"CanCarry", "Allow storage to be picked up"},
            {"ShowColorPicker", "Show color toggle and bars for colorable storages"},
            {"ShowSearchBar", "Show search bar above chest inventory"},
            {"ShowTabs", "Show tabs below chest inventory"},
            {"VacuumItems", "Allow storage to automatically collect dropped items"}
        };

        /// <summary>Default storage settings for unspecified options</summary>
        private static StorageConfig _defaultConfig;

        internal StorageMenu Menu => new(Capacity == 0 ? _defaultConfig : this);

        internal int ActualCapacity => Capacity == 0 ? _defaultConfig.Capacity : Capacity;
        internal static IList<string> DefaultTabs => _defaultConfig?.Tabs;

        public int Capacity { get; set; }
        public HashSet<string> EnabledFeatures { get; set; } = new() {"CanCarry", "ShowColorPicker", "ShowSearchBar", "ShowTabs"};
        public HashSet<string> DisabledFeatures { get; set; } = new();
        public IList<string> Tabs { get; set; } = new List<string>();

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
    }
}