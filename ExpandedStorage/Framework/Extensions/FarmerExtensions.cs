﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ExpandedStorage.Framework.Extensions
{
    public static class FarmerExtensions
    {
        private const string ChestsAnywhereOrderKey = "Pathoschild.ChestsAnywhere/Order";
        private static IMonitor _monitor;
        internal static void Init(IMonitor monitor)
        {
            _monitor = monitor;
        }
        
        public static Item AddItemToInventory(this Farmer farmer, Item item)
        {
            if (!farmer.IsLocalPlayer)
                return item;
            
            // Find prioritized storage
            var storages = ExpandedStorage.VacuumChests.Value
                .Where(s => s.Value.Filter(item))
                .Select(s => s.Key)
                .OrderByDescending(s => s.modData.TryGetValue(ChestsAnywhereOrderKey, out var order) ? Convert.ToInt32(order) : 0)
                .ToList();

            if (!storages.Any())
                return item;

            static void ShowHud(Item showItem)
            {
                var name = showItem.DisplayName;
                var color = Color.WhiteSmoke;
                if (showItem is Object showObj)
                {
                    switch (showObj.Type)
                    {
                        case "Arch":
                            color = Color.Tan;
                            name += Game1.content.LoadString("Strings\\StringsFromCSFiles:Farmer.cs.1954");
                            break;
                        case "Fish":
                            color = Color.SkyBlue;
                            break;
                        case "Mineral":
                            color = Color.PaleVioletRed;
                            break;
                        case "Vegetable":
                            color = Color.PaleGreen;
                            break;
                        case "Fruit":
                            color = Color.Pink;
                            break;
                    }
                }
                Game1.addHUDMessage(new HUDMessage(name, Math.Max(1, showItem.Stack), true, color, showItem));
            }
            
            // Bypass storage for Golden Walnuts
            if (Utility.IsNormalObjectAtParentSheetIndex(item, 73))
            {
                farmer.foundWalnut(item.Stack);
                ShowHud(item);
                return null;
            }
            
            // Bypass storage for Lost Book
            if (Utility.IsNormalObjectAtParentSheetIndex(item, 102))
            {
                farmer.foundArtifact(((Object) item).ParentSheetIndex, 1);
                ShowHud(item);
                return null;
            }

            // Bypass storage for Qi Gems
            if (Utility.IsNormalObjectAtParentSheetIndex(item, 858))
            {
                farmer.QiGems += item.Stack;
                Game1.playSound("qi_shop_purchase");
                farmer.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite("Maps\\springobjects", Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 858, 16, 16), 100f, 1, 8, new Vector2(0f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
                {
                    motion = new Vector2(0f, -6f),
                    acceleration = new Vector2(0f, 0.2f),
                    stopAcceleratingWhenVelocityIsZero = true,
                    attachedCharacter = farmer,
                    positionFollowsAttachedCharacter = true
                });
                ShowHud(item);
                return null;
            }

            if (Utility.IsNormalObjectAtParentSheetIndex(item, 930))
            {
                farmer.health = Math.Min(farmer.maxHealth, Game1.player.health + 10);
                farmer.currentLocation.debris.Add(new Debris(10, new Vector2(Game1.player.getStandingX(), Game1.player.getStandingY()), Color.Lime, 1f, farmer));
                Game1.playSound("healSound");
                return null;
            }

            // Insert item into storage
            var originalItem = item;
            var stack = (uint) item.Stack;
            foreach (var storage in storages)
            {
                item = storage.addItem(item);
                if (item == null)
                    break;
            }

            if (originalItem.HasBeenInInventory)
                return item;
            
            if (item != null && item.Stack == stack && originalItem is not SpecialItem)
                return item;
            
            switch (originalItem)
            {
                case SpecialItem specialItem:
                    specialItem.actionWhenReceived(farmer);
                    return item;
                case Object obj:
                {
                    if (obj.specialItem)
                    {
                        if (obj.bigCraftable.Value || originalItem is Furniture)
                        {
                            if (!farmer.specialBigCraftables.Contains(obj.ParentSheetIndex))
                                farmer.specialBigCraftables.Add(obj.ParentSheetIndex);
                        }
                        else if (!farmer.specialItems.Contains(obj.ParentSheetIndex))
                            farmer.specialItems.Add(obj.ParentSheetIndex);
                    }
                    
                    if (!obj.HasBeenPickedUpByFarmer)
                    {
                        if (obj.Category == -2 || obj.Type != null && obj.Type.Contains("Mineral"))
                        {
                            farmer.foundMineral(obj.ParentSheetIndex);
                        }
                        else if (originalItem is not Furniture && obj.Type != null && obj.Type.Contains("Arch"))
                        {
                            farmer.foundArtifact(obj.ParentSheetIndex, 1);
                        }
                    }
                    
                    Utility.checkItemFirstInventoryAdd(originalItem);
                    break;
                }
            }

            switch (originalItem.ParentSheetIndex)
            {
                case 384:
                    Game1.stats.GoldFound += stack;
                    break;
                case 378:
                    Game1.stats.CopperFound += stack;
                    break;
                case 380:
                    Game1.stats.IronFound += stack;
                    break;
                case 386:
                    Game1.stats.IridiumFound += stack;
                    break;
            }
            
            ShowHud(originalItem);
            return item;
        }
    }
}