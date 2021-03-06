#nullable enable
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.API;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class Storage : StorageConfig, IStorage
    {
        public enum SourceType
        {
            Unknown,
            Vanilla,
            JsonAssets,
            CustomChestTypes
        }

        private static readonly HashSet<string> ExcludeModDataKeys = new();

        public static readonly HashSet<string> VanillaNames = new()
        {
            "Chest",
            "Stone Chest",
            "Junimo Chest",
            "Mini-Shipping Bin",
            "Mini-Fridge"
        };

        /// <summary>List of ParentSheetIndex related to this item.</summary>
        internal readonly HashSet<int> ObjectIds = new();

        private StorageSprite? _storageSprite;

        /// <summary>The UniqueId of the Content Pack that storage data was loaded from.</summary>
        internal string ModUniqueId = "";

        /// <summary>The Asset path to the mod's SpriteSheets.</summary>
        internal string Path = "";

        internal Storage() : this("")
        {
        }

        internal Storage(string storageName)
        {
            switch (storageName)
            {
                case "Mini-Shipping Bin":
                    SpecialChestType = "MiniShippingBin";
                    OpenSound = "shwip";
                    PlaceSound = "axe";
                    CarrySound = "pickUpItem";
                    break;
                case "Mini-Fridge":
                    SpecialChestType = "None";
                    IsFridge = true;
                    OpenSound = "doorCreak";
                    PlaceSound = "hammer";
                    CarrySound = "pickUpItem";
                    break;
                case "Junimo Chest":
                    SpecialChestType = "JunimoChest";
                    OpenSound = "doorCreak";
                    PlaceSound = "axe";
                    CarrySound = "pickUpItem";
                    break;
                case "Stone Chest":
                    SpecialChestType = "None";
                    OpenSound = "openChest";
                    PlaceSound = "hammer";
                    CarrySound = "pickUpItem";
                    break;
                default:
                    SpecialChestType = "None";
                    OpenSound = "openChest";
                    PlaceSound = "axe";
                    CarrySound = "pickUpItem";
                    break;
            }
        }

        /// <summary>Which mod was used to load these assets into the game.</summary>
        internal SourceType Source { get; set; } = SourceType.Unknown;

        internal StorageSprite? SpriteSheet => !string.IsNullOrWhiteSpace(Image)
            ? _storageSprite ??= new StorageSprite(this)
            : null;

        internal string SummaryReport => string.Join("\n",
            $"{"Storage Option",-20} | Current Value",
            $"{new string('-', 21)}|{new string('-', 15)}",
            $"{"Special Chest Type",-20} | {SpecialChestType}",
            $"{"Is Fridge",-20} | {IsFridge}",
            $"{"Modded Capacity",-20} | {Capacity}",
            $"{"Open Sound",-20} | {OpenSound}",
            $"{"Place Sound",-20} | {PlaceSound}",
            $"{"Is Placeable",-20} | {IsPlaceable}",
            string.Join("\n",
                StorageOptions.Keys.Where(option => Option(option) != Choice.Unspecified).Select(option => $"{option,-20} | {Option(option)}")
            ),
            $"{"Allow List",-20} | {string.Join(", ", AllowList)},",
            $"{"Block List",-20} | {string.Join(", ", BlockList)},",
            $"{"Tabs",-20} | {string.Join(", ", Tabs)}"
        );

        public string SpecialChestType { get; set; }
        public bool IsFridge { get; set; }
        public string OpenSound { get; set; }
        public string PlaceSound { get; set; }
        public string CarrySound { get; set; }
        public bool IsPlaceable { get; set; } = true;
        public string Image { get; set; } = "";
        public int Frames { get; set; } = 5;
        public bool PlayerColor { get; set; } = true;
        public int Depth { get; set; }
        public IDictionary<string, string> ModData { get; set; } = new Dictionary<string, string>();
        public HashSet<string> AllowList { get; set; } = new();
        public HashSet<string> BlockList { get; set; } = new();

        internal static void AddExclusion(string modDataKey)
        {
            ExcludeModDataKeys.Add(modDataKey);
        }

        public bool MatchesContext(object context)
        {
            return context switch
            {
                Item item when ExcludeModDataKeys.Any(item.modData.ContainsKey) => false,
                AdventureGuild => false,
                LibraryMuseum => false,
                // Junimo Hut
                GameLocation => SpecialChestType == "MiniShippingBin",
                ShippingBin => SpecialChestType == "MiniShippingBin",
                Chest chest when chest.fridge.Value => IsFridge,
                Object obj when obj.bigCraftable.Value => ObjectIds.Contains(obj.ParentSheetIndex),
                _ => false
            };
        }

        internal static bool IsVanillaStorage(KeyValuePair<int, string> obj)
        {
            return obj.Value.EndsWith("Chest") || VanillaNames.Any(obj.Value.StartsWith);
        }

        private bool IsAllowed(Item item)
        {
            return !AllowList.Any() || AllowList.Any(item.MatchesTagExt);
        }

        private bool IsBlocked(Item item)
        {
            return BlockList.Any() && BlockList.Any(item.MatchesTagExt);
        }

        internal bool Filter(Item item)
        {
            return IsAllowed(item) && !IsBlocked(item);
        }

        internal bool HighlightMethod(Item item)
        {
            return Filter(item) && (SpecialChestType != "MiniShippingBin" || Utility.highlightShippableObjects(item));
        }

        internal static Storage Clone(IStorage storage)
        {
            var newStorage = new Storage();
            newStorage.CopyFrom(storage);
            return newStorage;
        }

        internal void CopyFrom(IStorage storage)
        {
            if (!IsFridge) IsFridge = storage.IsFridge;
            if (SpecialChestType == "None") SpecialChestType = storage.SpecialChestType;
            if (OpenSound == "openChest") OpenSound = storage.OpenSound;
            if (string.IsNullOrWhiteSpace(CarrySound)) CarrySound = storage.CarrySound;
            if (PlaceSound == "axe") PlaceSound = storage.PlaceSound;
            if (IsPlaceable) IsPlaceable = storage.IsPlaceable;
            if (!string.IsNullOrWhiteSpace(storage.Image)) Image = storage.Image;
            if (storage.Frames > 0) Frames = storage.Frames;
            PlayerColor = storage.PlayerColor;
            if (Depth == 0) Depth = storage.Depth;

            if (storage.AllowList != null && storage.AllowList.Any())
                AllowList = new HashSet<string>(storage.AllowList);

            if (storage.BlockList != null && storage.BlockList.Any())
                BlockList = new HashSet<string>(storage.BlockList);

            if (storage.ModData != null && storage.ModData.Any())
            {
                ModData.Clear();
                foreach (var modData in storage.ModData)
                {
                    if (!ModData.ContainsKey(modData.Key))
                        ModData.Add(modData.Key, modData.Value);
                }
            }

            CopyFrom((IStorageConfig) storage);
        }
    }
}