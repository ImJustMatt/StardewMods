using System;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.ExpandedStorage.Framework.Extensions
{
    internal static class ItemExtensions
    {
        private static AssetController Assets;

        public static void Init(AssetController assets)
        {
            Assets = assets;
        }

        public static Chest ToChest(this Item item, StorageController storage = null, Chest oldChest = null)
        {
            // Get config for chest
            if (storage == null && !Assets.TryGetStorage(item, out storage))
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

            // Add held object chest
            if (storage.HeldStorage)
            {
                var heldChest = new Chest(true, Vector2.Zero, item.ParentSheetIndex);
                if (item is Object obj && obj.heldObject.Value is Chest oldHeldChest && oldHeldChest.items.Any())
                    heldChest.items.CopyFrom(oldHeldChest.items);
                chest.heldObject.Value = heldChest;
            }

            chest.lidFrameCount.Value = Math.Max(storage.Frames, 1);

            // Copy modData from original item
            foreach (var modData in item.modData)
                chest.modData.CopyFrom(modData);

            // Copy modData from config
            foreach (var modData in storage.ModData)
            {
                if (!chest.modData.ContainsKey(modData.Key))
                    chest.modData.Add(modData.Key, modData.Value);
            }

            oldChest ??= item is Chest oldItemChest ? oldItemChest : null;
            if (oldChest == null) return chest;

            chest.playerChoiceColor.Value = oldChest.playerChoiceColor.Value;
            if (oldChest.items.Any())
                chest.items.CopyFrom(oldChest.items);

            return chest;
        }
    }
}