using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.Framework.Controllers;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    internal class ConfigModel
    {
        /// <summary>Control scheme for Keyboard or Controller.</summary>
        public ControlsModel Controls = new();

        /// <summary>Enables input designed to improve controller compatibility.</summary>
        public bool Controller { get; set; } = true;

        /// <summary>Allows storage menu to have up to 6 rows.</summary>
        public bool ExpandInventoryMenu { get; set; } = true;

        /// <summary>Toggle the HSL Color Picker.</summary>
        public bool ColorPicker { get; set; } = true;

        /// <summary>Symbol used to search items by context tag.</summary>
        public string SearchTagSymbol { get; set; } = "#";

        /// <summary>Items will only be collected to Vacuum Storages in the active hotbar.</summary>
        public bool VacuumToFirstRow { get; set; } = true;

        /// <summary>Log Level used when loading in storages.</summary>
        public string LogLevel { get; set; } = "Trace";

        /// <summary>Default config for unconfigured storages.</summary>
        public StorageConfigController DefaultStorage { get; set; } = new()
        {
            Tabs = new List<string> {"Crops", "Seeds", "Materials", "Cooking", "Fishing", "Equipment", "Clothing", "Misc"}
        };

        /// <summary>Default tabs for unconfigured storages.</summary>
        public IDictionary<string, TabController> DefaultTabs { get; set; } = new Dictionary<string, TabController>
        {
            {
                "Clothing", new TabController("Shirts.png",
                    "category_clothing",
                    "category_boots", "category_hat")
            },
            {
                "Cooking",
                new TabController("Cooking.png",
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
                new TabController("Crops.png",
                    "category_greens",
                    "category_flowers",
                    "category_fruits",
                    "category_vegetable")
            },
            {
                "Equipment",
                new TabController("Tools.png",
                    "category_equipment",
                    "category_ring",
                    "category_tool",
                    "category_weapon")
            },
            {
                "Fishing",
                new TabController("Fish.png",
                    "category_bait",
                    "category_fish",
                    "category_tackle",
                    "category_sell_at_fish_shop")
            },
            {
                "Materials",
                new TabController("Minerals.png",
                    "category_monster_loot",
                    "category_metal_resources",
                    "category_building_resources",
                    "category_minerals",
                    "category_crafting",
                    "category_gem")
            },
            {
                "Misc",
                new TabController("Misc.png",
                    "category_big_craftable",
                    "category_furniture",
                    "category_junk")
            },
            {
                "Seeds",
                new TabController("Seeds.png",
                    "category_seeds",
                    "category_fertilizer")
            }
        };
    }
}