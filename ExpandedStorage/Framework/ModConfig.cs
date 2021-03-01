using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.ExpandedStorage.Framework.Integrations;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace ImJustMatt.ExpandedStorage.Framework
{
    internal class ModConfig
    {
        /// <summary>Enable controller config settings.</summary>
        public bool Controller { get; set; } = true;

        /// <summary>Control scheme for Expanded Storage features.</summary>
        public ModConfigKeys Controls { get; set; } = new();

        /// <summary>Default config for unconfigured storages.</summary>
        public StorageConfig DefaultStorage { get; set; } = new()
        {
            Tabs = new List<string> {"Crops", "Seeds", "Materials", "Cooking", "Fishing", "Equipment", "Clothing", "Misc"}
        };

        /// <summary>Default tabs for unconfigured storages.</summary>
        public IDictionary<string, StorageTab> DefaultTabs { get; set; } = new Dictionary<string, StorageTab>
        {
            {
                "Clothing", new StorageTab("Shirts.png",
                    "category_clothing",
                    "category_boots", "category_hat")
            },
            {
                "Cooking",
                new StorageTab("Cooking.png",
                    "category_syrup",
                    "category_artisan_goods",
                    "category_ingredients",
                    "category_sell_at_pierres_and_marnies",
                    "category_sell_at_pierres",
                    "category_meat",
                    "category_cooking",
                    "category_milk",
                    "category_egg")
            },
            {
                "Crops",
                new StorageTab("Crops.png",
                    "category_greens",
                    "category_flowers",
                    "category_fruits",
                    "category_vegetable")
            },
            {
                "Equipment",
                new StorageTab("Tools.png",
                    "category_equipment",
                    "category_ring",
                    "category_tool",
                    "category_weapon")
            },
            {
                "Fishing",
                new StorageTab("Fish.png",
                    "category_bait",
                    "category_fish",
                    "category_tackle",
                    "category_sell_at_fish_shop")
            },
            {
                "Materials",
                new StorageTab("Minerals.png",
                    "category_monster_loot",
                    "category_metal_resources",
                    "category_building_resources",
                    "category_minerals",
                    "category_crafting",
                    "category_gem")
            },
            {
                "Misc",
                new StorageTab("Misc.png",
                    "category_big_craftable",
                    "category_furniture",
                    "category_junk")
            },
            {
                "Seeds",
                new StorageTab("Seeds.png",
                    "category_seeds",
                    "category_fertilizer")
            }
        };

        /// <summary>Only vacuum to storages in the first row of player inventory.</summary>
        public bool VacuumToFirstRow { get; set; } = true;

        /// <summary>Adds three extra rows to the Inventory Menu.</summary>
        public bool ExpandInventoryMenu { get; set; } = true;

        /// <summary>Symbol used to search items by context tags.</summary>
        public string SearchTagSymbol { get; set; } = "#";

        protected internal string SummaryReport => string.Join("\n",
            "Expanded Storage Configuration",
            $"{"Config Option",-20} | Current Value",
            $"{new string('-', 21)}|{new string('-', 15)}",
            $"{"Next Tab",-20} | {Controls.NextTab}",
            $"{"Previous Tab",-20} | {Controls.PreviousTab}",
            $"{"Scroll Up",-20} | {Controls.ScrollUp}",
            $"{"Scroll Down",-20} | {Controls.ScrollDown}",
            $"{"Show Crafting",-20} | {Controls.OpenCrafting}",
            $"{"Resize Menu",-20} | {ExpandInventoryMenu}",
            $"{"Search Tag Symbol",-20} | {SearchTagSymbol}",
            $"{"Vacuum First Row",-20} | {VacuumToFirstRow}",
            $"{"Enable Controller",-20} | {Controller}",
            string.Join("\n",
                StorageConfig.StorageOptions.Keys
                    .Where(option => DefaultStorage.Option(option) != StorageConfig.Choice.Unspecified)
                    .Select(option => $"{option,-20} | {DefaultStorage.Option(option)}")
            )
        );

        internal void CopyFrom(ModConfig config)
        {
            Controls = config.Controls;
            Controller = config.Controller;
            VacuumToFirstRow = config.VacuumToFirstRow;
            ExpandInventoryMenu = config.ExpandInventoryMenu;
            SearchTagSymbol = config.SearchTagSymbol;
            DefaultStorage = new Storage();
            DefaultStorage.CopyFrom(config.DefaultStorage);
            DefaultTabs.Clear();
            foreach (var tab in config.DefaultTabs)
            {
                var newTab = new StorageTab();
                newTab.CopyFrom(tab.Value);
                DefaultTabs.Add(tab.Key, newTab);
            }
        }

        public static void RegisterModConfig(IManifest manifest, IGenericModConfigMenuAPI modConfigAPI, ModConfig config)
        {
            // Controls
            modConfigAPI.RegisterLabel(manifest,
                "Controls",
                "Controller/Keyboard controls");

            modConfigAPI.RegisterSimpleOption(manifest,
                "Scroll Up",
                "Button for scrolling up",
                () => config.Controls.ScrollUp.Keybinds.Single(kb => kb.IsBound).Buttons.First(),
                value => config.Controls.ScrollUp = KeybindList.ForSingle(value));
            modConfigAPI.RegisterSimpleOption(manifest,
                "Scroll Down",
                "Button for scrolling down",
                () => config.Controls.ScrollDown.Keybinds.Single(kb => kb.IsBound).Buttons.First(),
                value => config.Controls.ScrollDown = KeybindList.ForSingle(value));
            modConfigAPI.RegisterSimpleOption(manifest,
                "Previous Tab",
                "Button for switching to the previous tab",
                () => config.Controls.PreviousTab.Keybinds.Single(kb => kb.IsBound).Buttons.First(),
                value => config.Controls.PreviousTab = KeybindList.ForSingle(value));
            modConfigAPI.RegisterSimpleOption(manifest,
                "Next Tab",
                "Button for switching to the next tab",
                () => config.Controls.NextTab.Keybinds.Single(kb => kb.IsBound).Buttons.First(),
                value => config.Controls.NextTab = KeybindList.ForSingle(value));

            // Tweaks
            modConfigAPI.RegisterLabel(manifest,
                "Tweaks",
                "Modify behavior for certain features");

            modConfigAPI.RegisterSimpleOption(manifest,
                "Enable Controller",
                "Enables settings designed to improve controller compatibility",
                () => config.Controller,
                value => config.Controller = value);
            modConfigAPI.RegisterSimpleOption(manifest,
                "Resize Inventory Menu",
                "Allows the inventory menu to have 4-6 rows instead of the default 3",
                () => config.ExpandInventoryMenu,
                value => config.ExpandInventoryMenu = value);
            modConfigAPI.RegisterSimpleOption(manifest,
                "Search Symbol",
                "Symbol used to search items by context tag",
                () => config.SearchTagSymbol,
                value => config.SearchTagSymbol = value);
            modConfigAPI.RegisterSimpleOption(manifest,
                "Vacuum To First Row",
                "Uncheck to allow vacuuming to any chest in player inventory",
                () => config.VacuumToFirstRow,
                value => config.VacuumToFirstRow = value);

            // Default Storage Config
            var optionChoices = Enum.GetNames(typeof(StorageConfig.Choice));

            Func<string> OptionGet(string option)
            {
                return () => config.DefaultStorage.Option(option).ToString();
            }

            Action<string> OptionSet(string option)
            {
                return value =>
                {
                    if (Enum.TryParse(value, out StorageConfig.Choice choice))
                        config.DefaultStorage.SetOption(option, choice);
                };
            }

            modConfigAPI.RegisterLabel(manifest,
                "Default Storage",
                "Default config for unconfigured storages.");

            modConfigAPI.RegisterSimpleOption(manifest, "Capacity", "Number of item slots the storage will contain",
                () => config.DefaultStorage.Capacity,
                value => config.DefaultStorage.Capacity = value);

            foreach (var option in StorageConfig.StorageOptions)
            {
                modConfigAPI.RegisterChoiceOption(manifest, option.Key, option.Value,
                    OptionGet(option.Key), OptionSet(option.Key), optionChoices);
            }
        }
    }
}