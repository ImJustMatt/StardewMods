using System;
using System.Collections.Generic;
using Helpers.ConfigData;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class ConfigController : ConfigModel
    {
        private static readonly ConfigHelper ConfigHelper = new(new FieldHandler(), new ConfigController(), new List<KeyValuePair<string, string>>
        {
            new("Controls", "Control scheme for Keyboard or Controller"),
            new("Controller", "Enables input designed to improve controller compatibility"),
            new("ExpandInventoryMenu", "Allows storage menu to have up to 6 rows"),
            new("ColorPicker", "Toggle the HSL Color Picker"),
            new("SearchTagSymbol", "Symbol used to search items by context tag"),
            new("VacuumToFirstRow", "Items will only be collected to Vacuum Storages in the active hotbar"),
            new("LogLevel", "Log Level used when loading in storages.")
        });

        public ConfigController()
        {
        }

        internal LogLevel LogLevelProperty
        {
            get => Enum.TryParse(LogLevel, out LogLevel logLevel) ? logLevel : StardewModdingAPI.LogLevel.Trace;
            private set => LogLevel = value.ToString();
        }

        internal void Log(IMonitor monitor)
        {
            monitor.Log(string.Join("\n",
                "Mod Config",
                ConfigHelper.Summary(this),
                ControlsModel.ConfigHelper.Summary(Controls, false),
                StorageConfigController.ConfigHelper.Summary(DefaultStorage, false)
            ), LogLevelProperty);
        }

        internal void RegisterModConfig(IModHelper helper, IManifest manifest, GenericModConfigMenuIntegration modConfigMenu)
        {
            if (!modConfigMenu.IsLoaded)
                return;

            void DefaultConfig()
            {
                modConfigMenu.RevertToDefault(ConfigHelper, this).Invoke();
                modConfigMenu.RevertToDefault(ControlsModel.ConfigHelper, Controls).Invoke();
            }

            void SaveConfig()
            {
                helper.WriteConfig(this);
            }

            modConfigMenu.API.RegisterModConfig(manifest, DefaultConfig, SaveConfig);
            modConfigMenu.API.SetDefaultIngameOptinValue(manifest, true);
            modConfigMenu.API.RegisterPageLabel(manifest, "Controls", "Controller/Keyboard controls", "Controls");
            modConfigMenu.API.RegisterPageLabel(manifest, "Tweaks", "Modify behavior for certain features", "Tweaks");
            modConfigMenu.API.RegisterPageLabel(manifest, "Default Storage", "Global default storage config", "Default Storage");

            modConfigMenu.API.StartNewPage(manifest, "Controls");
            modConfigMenu.API.RegisterLabel(manifest, "Controls", "Controller/Keyboard controls");
            modConfigMenu.RegisterConfigOptions(manifest, ControlsModel.ConfigHelper, Controls);
            modConfigMenu.API.RegisterPageLabel(manifest, "Go Back", "", "");

            modConfigMenu.API.StartNewPage(manifest, "Tweaks");
            modConfigMenu.API.RegisterLabel(manifest, "Tweaks", "Modify behavior for certain features");
            modConfigMenu.RegisterConfigOptions(manifest, ConfigHelper, this);
            modConfigMenu.API.RegisterPageLabel(manifest, "Go Back", "", "");

            modConfigMenu.API.StartNewPage(manifest, "Default Storage");
            modConfigMenu.API.RegisterLabel(manifest, "Default Storage", "Global default storage config");
            modConfigMenu.RegisterConfigOptions(manifest, StorageConfigController.ConfigHelper, DefaultStorage);
            modConfigMenu.API.RegisterPageLabel(manifest, "Go Back", "", "");
        }

        private class FieldHandler : BaseFieldHandler
        {
            private static readonly string[] Choices = Enum.GetNames(typeof(LogLevel));

            public override bool CanHandle(IField field)
            {
                return field.Name.Equals("LogLevel");
            }

            public override object GetValue(object instance, IField field)
            {
                return ((ConfigController) instance).LogLevelProperty;
            }

            public override void SetValue(object instance, IField field, object value)
            {
                var modConfig = (ConfigController) instance;
                modConfig.LogLevelProperty = Enum.TryParse((string) value, out LogLevel logLevel) ? logLevel : StardewModdingAPI.LogLevel.Trace;
            }

            public override void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IField field)
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