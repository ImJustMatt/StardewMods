using System;
using System.Collections.Generic;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using ImJustMatt.GarbageDay.Framework.Models;
using StardewModdingAPI;

namespace ImJustMatt.GarbageDay.Framework.Controllers
{
    internal class ConfigController : ConfigModel
    {
        private static readonly ConfigHelper ConfigHelper = new(new ConfigController(), new List<KeyValuePair<string, string>>
        {
            new("GetRandomItemFromSeason", "Global change that a random item from season is collected"),
            new("GarbageDay", "Day of week that trash is emptied out"),
            new("LogLevel", "Log Level used when loading in garbage cans")
        });

        public ConfigController()
        {
        }

        internal LogLevel LogLevelProperty
        {
            get => Enum.TryParse(LogLevel, out LogLevel logLevel) ? logLevel : StardewModdingAPI.LogLevel.Trace;
            private set => LogLevel = value.ToString();
        }

        internal void RegisterModConfig(IModHelper helper, IManifest manifest, GenericModConfigMenuIntegration modConfigMenu)
        {
            void SaveToFile()
            {
                helper.WriteConfig(this);
            }

            var choices = Enum.GetNames(typeof(LogLevel));

            modConfigMenu.API.RegisterModConfig(manifest, modConfigMenu.RevertToDefault(ConfigHelper, this), SaveToFile);
            modConfigMenu.API.RegisterClampedOption(manifest,
                "Garbage Pickup Day", "Day of week that garbage cans are emptied up (0 Sunday - 6 Saturday)",
                () => GarbageDay,
                value => GarbageDay = value,
                0, 6);
            modConfigMenu.API.RegisterClampedOption(manifest,
                "Get Random Item from Season", "Chance that a random item from season is added to the garbage can",
                () => (float) GetRandomItemFromSeason,
                value => GetRandomItemFromSeason = value,
                0, 1);
            modConfigMenu.API.RegisterChoiceOption(
                manifest,
                "Log Level",
                "Log Level used when loading in garbage cans",
                () => LogLevel,
                value => LogLevel = value,
                choices
            );
        }
    }
}