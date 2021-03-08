using System;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Extensions
{
    public static class ItemExtensions
    {
        public static Chest ToChest(this Item item, Storage storage = null)
        {
            // Get config for chest
            if (storage == null && !ExpandedStorage.TryGetStorage(item, out storage))
            {
                throw new InvalidOperationException($"Unexpected item '{item.Name}'.");
            }

            // Create Chest from Item
            var chest = new Chest(true, Vector2.Zero, item.ParentSheetIndex)
            {
                name = item.Name,
                SpecialChestType = Enum.TryParse(storage.SpecialChestType, out Chest.SpecialChestTypes specialChestType)
                    ? specialChestType
                    : Chest.SpecialChestTypes.None
            };
            chest.fridge.Value = storage.IsFridge;

            if (string.IsNullOrWhiteSpace(storage.Image))
                chest.lidFrameCount.Value = Math.Max(storage.Frames, 1);
            else if (item.ParentSheetIndex == 216)
                chest.lidFrameCount.Value = 2;

            // Copy modData from original item
            foreach (var modData in item.modData)
                chest.modData.CopyFrom(modData);

            // Copy modData from config
            foreach (var modData in storage.ModData)
            {
                if (!chest.modData.ContainsKey(modData.Key))
                    chest.modData.Add(modData.Key, modData.Value);
            }

            if (item is not Chest oldChest)
                return chest;

            chest.playerChoiceColor.Value = oldChest.playerChoiceColor.Value;
            if (oldChest.items.Any())
                chest.items.CopyFrom(oldChest.items);

            return chest;
        }
    }
}