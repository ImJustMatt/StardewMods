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

        internal void RegisterModConfig(ExpandedStorage mod)
        {
            if (!mod.ModConfigMenu.IsLoaded)
                return;

            void DefaultConfig()
            {
                mod.ModConfigMenu.RevertToDefault(ConfigHelper, this).Invoke();
                mod.ModConfigMenu.RevertToDefault(ControlsModel.ConfigHelper, Controls).Invoke();
            }

            void SaveConfig()
            {
                mod.Helper.WriteConfig(this);
            }

            mod.ModConfigMenu.API.RegisterModConfig(mod.ModManifest, DefaultConfig, SaveConfig);
            mod.ModConfigMenu.API.SetDefaultIngameOptinValue(mod.ModManifest, true);
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Controls", "Controller/Keyboard controls", "Controls");
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Tweaks", "Modify behavior for certain features", "Tweaks");
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Default Storage", "Global default storage config", "Default Storage");

            mod.ModConfigMenu.API.StartNewPage(mod.ModManifest, "Controls");
            mod.ModConfigMenu.API.RegisterLabel(mod.ModManifest, "Controls", "Controller/Keyboard controls");
            mod.ModConfigMenu.RegisterConfigOptions(mod.ModManifest, ControlsModel.ConfigHelper, Controls);
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Go Back", "", "");

            mod.ModConfigMenu.API.StartNewPage(mod.ModManifest, "Tweaks");
            mod.ModConfigMenu.API.RegisterLabel(mod.ModManifest, "Tweaks", "Modify behavior for certain features");
            mod.ModConfigMenu.RegisterConfigOptions(mod.ModManifest, ConfigHelper, this);
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Go Back", "", "");

            mod.ModConfigMenu.API.StartNewPage(mod.ModManifest, "Default Storage");
            mod.ModConfigMenu.API.RegisterLabel(mod.ModManifest, "Default Storage", "Global default storage config");
            mod.ModConfigMenu.RegisterConfigOptions(mod.ModManifest, StorageConfigController.ConfigHelper, DefaultStorage);
            mod.ModConfigMenu.API.RegisterPageLabel(mod.ModManifest, "Go Back", "", "");
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