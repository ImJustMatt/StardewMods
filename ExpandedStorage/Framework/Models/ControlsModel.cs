using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    internal class ControlsModel
    {
        internal static readonly ConfigHelper ConfigHelper = new(new ControlsModel(), new List<KeyValuePair<string, string>>
        {
            new("OpenCrafting", "Open the crafting menu using inventory from a held storage"),
            new("ScrollUp", "Button for scrolling the item storage menu up one row"),
            new("ScrollDown", "Button for scrolling the item storage menu down one row"),
            new("PreviousTab", "Button for switching to the previous tab"),
            new("NextTab", "Button for switching to the next tab"),
            new("CarryChest", "Pick up a placed storage"),
            new("AccessCarriedChest", "Open the menu for a held storage")
        });

        public KeybindList OpenCrafting { get; set; } = KeybindList.ForSingle(SButton.K);
        public KeybindList ScrollUp { get; set; } = KeybindList.ForSingle(SButton.DPadUp);
        public KeybindList ScrollDown { get; set; } = KeybindList.ForSingle(SButton.DPadDown);
        public KeybindList PreviousTab { get; set; } = KeybindList.ForSingle(SButton.DPadLeft);
        public KeybindList NextTab { get; set; } = KeybindList.ForSingle(SButton.DPadRight);
        public KeybindList CarryChest { get; set; } = KeybindList.ForSingle(SButton.ControllerA);
        public KeybindList AccessCarriedChest { get; set; } = KeybindList.ForSingle(SButton.ControllerX);
    }
}