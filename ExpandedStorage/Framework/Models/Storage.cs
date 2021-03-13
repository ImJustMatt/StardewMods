#nullable enable
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class Storage : StorageConfig, IStorage
    {
        public enum AnimationType
        {
            None,
            Loop,
            Color
        }

        public enum SourceType
        {
            Unknown,
            Vanilla,
            JsonAssets,
            CustomChestTypes
        }

        internal new static readonly ConfigHelper ConfigHelper = new(new List<KeyValuePair<string, string>>
        {
            new("SpecialChestType", "Can be one of None, MiniShippingBin, JunimoChest, AutoLoader, or Enricher"),
            new("IsFridge", "Make the Storage into a Mini-Fridge when placed"),
            new("OpenNearby", "Play opening animation when player is nearby"),
            new("OpenSound", "Sound played when storage object is opened"),
            new("PlaceSound", "Sound played when storage object is placed"),
            new("CarrySound", "Sound played when storage object is picked up"),
            new("IsPlaceable", "Allow storage to be placed in a game location"),
            new("Image", "SpriteSheet for the storage object"),
            new("Frames", "Number of animation frames in the SpriteSheet"),
            new("Depth", "Number of pixels from the bottom of the SpriteSheet that occupy the ground for placement"),
            new("Animation", "Can be one of None, Loop, or Color"),
            new("Delay", "Number of ticks for each Animation Frame"),
            new("PlayerColor", "Enables the Player Color Selector from the Storage Menu"),
            new("PlayerConfig", "Enables Storage Capacity and Features to be overriden by config file"),
            new("Tabs", "Tabs used to filter this Storage Menu inventory")
        });

        private static readonly HashSet<string> ExcludeModDataKeys = new();

        public static readonly HashSet<string> VanillaNames = new()
        {
            "Chest",
            "Stone Chest",
            "Junimo Chest",
            "Mini-Shipping Bin",
            "Mini-Fridge"
        };

        internal static uint Frame;

        internal static HSLColor ColorWheel;

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
                    OpenNearby = true;
                    OpenSound = "shwip";
                    break;
                case "Mini-Fridge":
                    IsFridge = true;
                    OpenSound = "doorCreak";
                    PlaceSound = "hammer";
                    PlayerColor = false;
                    Frames = 2;
                    break;
                case "Junimo Chest":
                    SpecialChestType = "JunimoChest";
                    break;
                case "Stone Chest":
                    PlaceSound = "hammer";
                    break;
            }
        }

        /// <summary>Which mod was used to load these assets into the game.</summary>
        internal SourceType Source { get; set; } = SourceType.Unknown;

        internal StorageSprite? SpriteSheet => !string.IsNullOrWhiteSpace(Image)
            ? _storageSprite ??= new StorageSprite(this)
            : null;

        public string SpecialChestType { get; set; } = "None";
        public bool IsFridge { get; set; }
        public bool OpenNearby { get; set; }
        public string OpenNearbySound { get; set; } = "doorCreak";
        public string CloseNearbySound { get; set; } = "doorCreakReverse";
        public string OpenSound { get; set; } = "openChest";
        public string PlaceSound { get; set; } = "axe";
        public string CarrySound { get; set; } = "pickUpItem";
        public bool IsPlaceable { get; set; } = true;
        public string? Image { get; set; }
        public int Frames { get; set; } = 5;
        public int Depth { get; set; }
        public string Animation { get; set; } = "None";
        public int Delay { get; set; } = 5;
        public bool PlayerColor { get; set; } = true;
        public bool PlayerConfig { get; set; } = true;
        public IDictionary<string, string> ModData { get; set; } = new Dictionary<string, string>();
        public HashSet<string> AllowList { get; set; } = new();
        public HashSet<string> BlockList { get; set; } = new();

        internal static void Init(IModEvents events)
        {
            ColorWheel = new HSLColor(0, 1, 0.5f);
            events.GameLoop.SaveLoaded += delegate { events.GameLoop.UpdateTicked += OnUpdateTicked; };
            events.GameLoop.ReturnedToTitle += delegate { events.GameLoop.UpdateTicked -= OnUpdateTicked; };
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            Frame = e.Ticks;
            ColorWheel.H = e.Ticks / 5 % 100 / 100f;
        }

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
            OpenNearby = storage.OpenNearby;
            if (OpenNearbySound == "doorCreak") OpenNearbySound = storage.OpenNearbySound;
            if (CloseNearbySound == "doorCreakReverse") CloseNearbySound = storage.CloseNearbySound;
            if (OpenSound == "openChest") OpenSound = storage.OpenSound;
            if (CarrySound == "pickUpItem") CarrySound = storage.CarrySound;
            if (PlaceSound == "axe") PlaceSound = storage.PlaceSound;
            if (IsPlaceable) IsPlaceable = storage.IsPlaceable;
            if (!string.IsNullOrWhiteSpace(storage.Image)) Image = storage.Image;
            if (!string.IsNullOrWhiteSpace(storage.Animation)) Animation = storage.Animation;
            if (storage.Frames > 0) Frames = storage.Frames;
            if (storage.Delay > 0) Delay = storage.Delay;
            PlayerColor = storage.PlayerColor;
            PlayerConfig = storage.PlayerConfig;
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