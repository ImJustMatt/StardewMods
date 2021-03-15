using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using ImJustMatt.ExpandedStorage.Framework.Models;

namespace ImJustMatt.ExpandedStorage.Framework
{
    internal class ModConfig
    {
        internal static readonly ConfigHelper ConfigHelper = new(new ModConfig(), new List<KeyValuePair<string, string>>
        {
            new("Controls", "Control scheme for Keyboard or Controller"),
            new("Controller", "Enables input designed to improve controller compatibility"),
            new("ExpandInventoryMenu", "Allows storage menu to have up to 6 rows"),
            new("SearchTagSymbol", "Symbol used to search items by context tag"),
            new("VacuumToFirstRow", "Items will only be collected to Vacuum Storages in the active hotbar")
        });

        /// <summary>Control scheme for Keyboard or Controller.</summary>
        public ModConfigKeys Controls = new();

        public ModConfig()
        {
        }

        /// <summary>Enables input designed to improve controller compatibility.</summary>
        public bool Controller { get; set; } = true;

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

        /// <summary>Allows storage menu to have up to 6 rows.</summary>
        public bool ExpandInventoryMenu { get; set; } = true;

        /// <summary>Symbol used to search items by context tag.</summary>
        public string SearchTagSymbol { get; set; } = "#";

        /// <summary>Items will only be collected to Vacuum Storages in the active hotbar.</summary>
        public bool VacuumToFirstRow { get; set; } = true;
    }
}