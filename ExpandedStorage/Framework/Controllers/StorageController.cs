using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Common.Helpers;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    public class StorageController : StorageModel
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

        internal static readonly ConfigHelper ConfigHelper = new(new StorageController(), new List<KeyValuePair<string, string>>
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
            "Mini-Fridge",
            "Auto-Grabber"
        };

        internal static uint Frame;

        internal static HSLColor ColorWheel;

        /// <summary>List of ParentSheetIndex related to this item.</summary>
        internal readonly HashSet<int> ObjectIds = new();

        private StorageSpriteController _storageSprite;

        internal StorageConfigController Config;

        /// <summary>The UniqueId of the Content Pack that storage data was loaded from.</summary>
        internal string ModUniqueId = "";

        /// <summary>The Asset path to the mod's SpriteSheets.</summary>
        internal string Path = "";

        [JsonConstructor]
        internal StorageController(string storageName = "", IStorage storage = null)
        {
            if (storage != null)
            {
                IsFridge = storage.IsFridge;
                SpecialChestType = storage.SpecialChestType;
                OpenNearby = storage.OpenNearby;
                OpenNearbySound = storage.OpenNearbySound;
                CloseNearbySound = storage.CloseNearbySound;
                OpenSound = storage.OpenSound;
                CarrySound = storage.CarrySound;
                PlaceSound = storage.PlaceSound;
                IsPlaceable = storage.IsPlaceable;
                Image = storage.Image;
                Animation = storage.Animation;
                Frames = storage.Frames;
                Delay = storage.Delay;
                PlayerColor = storage.PlayerColor;
                PlayerConfig = storage.PlayerConfig;
                Depth = storage.Depth;
                AllowList = new HashSet<string>(storage.AllowList);
                BlockList = new HashSet<string>(storage.BlockList);
                ModData = new Dictionary<string, string>(storage.ModData);
            }

            // Vanilla overrides
            switch (storageName)
            {
                case "Auto-Grabber":
                    Frames = 1;
                    break;
                case "Junimo Chest":
                    SpecialChestType = "JunimoChest";
                    break;
                case "Mini-Shipping Bin":
                    SpecialChestType = "MiniShippingBin";
                    OpenNearby = 1;
                    OpenSound = "shwip";
                    BlockList.Add("VacuumItems");
                    break;
                case "Mini-Fridge":
                    IsFridge = true;
                    OpenSound = "doorCreak";
                    PlaceSound = "hammer";
                    PlayerColor = false;
                    Frames = 2;
                    break;
                case "Stone Chest":
                    PlaceSound = "hammer";
                    break;
            }
        }

        /// <summary>Which mod was used to load these assets into the game.</summary>
        internal SourceType Source { get; set; } = SourceType.Unknown;

        internal Func<Texture2D> Texture { get; set; }

        internal StorageSpriteController SpriteSheet => Texture != null
            ? _storageSprite ??= new StorageSpriteController(this)
            : null;

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
                Object obj when obj.bigCraftable.Value && obj.heldObject.Value is Chest => ObjectIds.Contains(obj.ParentSheetIndex),
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

        internal void Log(string storageName, IMonitor monitor, LogLevel logLevel)
        {
            monitor.Log(string.Join("\n",
                $"{storageName} Config:",
                ConfigHelper.Summary(this),
                StorageConfigController.ConfigHelper.Summary(Config, false)
            ), logLevel);
        }
    }
}