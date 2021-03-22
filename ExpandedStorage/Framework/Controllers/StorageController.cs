using System;
using System.Collections.Generic;
using System.Linq;
using Helpers.ConfigData;
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

        internal static readonly ConfigHelper ConfigHelper = new(new FieldHandler(), new StorageController(), new List<KeyValuePair<string, string>>
        {
            new("SpecialChestType", "Can be one of None, MiniShippingBin, JunimoChest, AutoLoader, or Enricher"),
            new("IsFridge", "Make the Storage into a Mini-Fridge when placed"),
            new("OpenNearby", "Play opening animation when player is nearby"),
            new("OpenSound", "Sound played when storage object is opened"),
            new("PlaceSound", "Sound played when storage object is placed"),
            new("CarrySound", "Sound played when storage object is picked up"),
            new("OpenNearbySound", "Sound played when storage opens while approached"),
            new("CloseNearbySound", "Sound played when storage closes after approached"),
            new("HeldStorage", "Pull items from held chest such as Auto-Grabber"),
            new("IsPlaceable", "Allow storage to be placed in a game location"),
            new("Image", "SpriteSheet for the storage object"),
            new("Animation", "Can be one of None, Loop, or Color"),
            new("Frames", "Number of animation frames in the SpriteSheet"),
            new("Delay", "Number of ticks for each Animation Frame"),
            new("Depth", "Number of pixels from the bottom of the SpriteSheet that occupy the ground for placement"),
            new("PlayerColor", "Enables the Player Color Selector from the Storage Menu"),
            new("PlayerConfig", "Enables Storage Capacity and Features to be overriden by config file"),
            new("Tabs", "Tabs used to filter this Storage Menu inventory"),
            new("AllowList", "Storage may only hold items with allowed context tags"),
            new("BlockList", "Storage may hold allowed items except for those with blocked context tags"),
            new("ModData", "Add modData to placed chests (if key does not already exist)"),
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
                ConfigHelper.CopyValues(storage, this);
            }

            // Vanilla overrides
            switch (storageName)
            {
                case "Auto-Grabber":
                    HeldStorage = true;
                    if (string.IsNullOrWhiteSpace(Image))
                    {
                        Frames = 1;
                        PlayerColor = false;
                    }

                    break;
                case "Junimo Chest":
                    SpecialChestType = "JunimoChest";
                    break;
                case "Mini-Shipping Bin":
                    SpecialChestType = "MiniShippingBin";
                    if (string.IsNullOrWhiteSpace(Image))
                    {
                        OpenNearby = 1;
                    }

                    OpenSound = "shwip";
                    BlockList.Add("VacuumItems");
                    break;
                case "Mini-Fridge":
                    IsFridge = true;
                    if (string.IsNullOrWhiteSpace(Image))
                    {
                        Frames = 2;
                        PlayerColor = false;
                        OpenSound = "doorCreak";
                        PlaceSound = "hammer";
                    }

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
            return IsAllowed(item) && !IsBlocked(item) && SpecialChestType != "MiniShippingBin" || Utility.highlightShippableObjects(item);
        }

        internal void Log(string storageName, IMonitor monitor, LogLevel logLevel)
        {
            monitor.Log(string.Join("\n",
                $"{storageName} Config:",
                ConfigHelper.Summary(this),
                StorageConfigController.ConfigHelper.Summary(Config, false)
            ), logLevel);
        }

        private class FieldHandler : BaseFieldHandler
        {
            private static readonly string[] Fields = {"Tabs", "AllowList", "BlockList", "ModData"};

            public override bool CanHandle(IField field)
            {
                return Fields.Contains(field.Name);
            }

            public override void CopyValue(IField field, object source, object target)
            {
                if (field.Info == null)
                {
                    return;
                }

                if (field.Info.PropertyType == typeof(HashSet<string>))
                {
                    var value = (HashSet<string>) field.Info.GetValue(source, null);
                    field.Info.SetValue(target, new HashSet<string>(value));
                }
                else if (field.Info.PropertyType == typeof(IList<string>))
                {
                    var value = (IList<string>) field.Info.GetValue(source, null);
                    field.Info.SetValue(target, new List<string>(value));
                }
                else if (field.Info.PropertyType == typeof(Dictionary<string, string>))
                {
                    var value = (Dictionary<string, string>) field.Info.GetValue(source, null);
                    field.Info.SetValue(target, new Dictionary<string, string>(value));
                }
            }
        }
    }
}