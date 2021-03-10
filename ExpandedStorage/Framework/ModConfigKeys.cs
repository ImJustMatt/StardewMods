using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace ImJustMatt.ExpandedStorage.Framework
{
    internal class ModConfigKeys
    {
        private static readonly ConfigHelper ConfigHelper = new(new List<KeyValuePair<string, string>>
        {
            new("OpenCrafting", "Open the crafting menu using inventory from a carried storage"),
            new("ScrollUp", "Button for scrolling the item storage menu up one row"),
            new("ScrollDown", "Button for scrolling the item storage menu down one row"),
            new("PreviousTab", "Button for switching to the previous tab"),
            new("NextTab", "Button for switching to the next tab")
        });

        public KeybindList OpenCrafting { get; set; } = KeybindList.ForSingle(SButton.K);
        public KeybindList ScrollUp { get; set; } = KeybindList.ForSingle(SButton.DPadUp);
        public KeybindList ScrollDown { get; set; } = KeybindList.ForSingle(SButton.DPadDown);
        public KeybindList PreviousTab { get; set; } = KeybindList.ForSingle(SButton.DPadLeft);
        public KeybindList NextTab { get; set; } = KeybindList.ForSingle(SButton.DPadRight);

        internal static void RegisterModConfig(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, ModConfigKeys configKeys)
        {
            if (!modConfigMenu.IsLoaded)
                return;
            modConfigMenu.API.RegisterLabel(manifest,
                "Controls",
                "Controller/Keyboard controls");
            modConfigMenu.API.RegisterSimpleOption(manifest,
                "Scroll Up",
                "Button for scrolling the item storage menu up one row",
                () => configKeys.ScrollUp.Keybinds.Single(kb => kb.IsBound)?.Buttons.First() ?? SButton.DPadUp,
                value => configKeys.ScrollUp = KeybindList.ForSingle(value));
            modConfigMenu.API.RegisterSimpleOption(manifest,
                "Scroll Down",
                "Button for scrolling the item storage menu down one row",
                () => configKeys.ScrollDown.Keybinds.Single(kb => kb.IsBound)?.Buttons.First() ?? SButton.DPadUp,
                value => configKeys.ScrollDown = KeybindList.ForSingle(value));
            modConfigMenu.API.RegisterSimpleOption(manifest,
                "Previous Tab",
                "Button for switching to the previous tab",
                () => configKeys.PreviousTab.Keybinds.Single(kb => kb.IsBound)?.Buttons.First() ?? SButton.DPadUp,
                value => configKeys.PreviousTab = KeybindList.ForSingle(value));
            modConfigMenu.API.RegisterSimpleOption(manifest,
                "Next Tab",
                "Button for switching to the next tab",
                () => configKeys.NextTab.Keybinds.Single(kb => kb.IsBound)?.Buttons.First() ?? SButton.DPadUp,
                value => configKeys.NextTab = KeybindList.ForSingle(value));
            modConfigMenu.API.RegisterSimpleOption(manifest,
                "Open Crafting",
                "Open the crafting menu using inventory from a carried storage",
                () => configKeys.OpenCrafting.Keybinds.Single(kb => kb.IsBound)?.Buttons.First() ?? SButton.DPadUp,
                value => configKeys.OpenCrafting = KeybindList.ForSingle(value));
        }
    }
}