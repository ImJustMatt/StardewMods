using System;
using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Extensions
{
    public static class ChestExtensions
    {
        private static readonly HashSet<int> HideColorPickerIds = new() {216, 248, 256};
        private static readonly HashSet<int> ShowBottomBraceIds = new() {130, 232};
        private static IReflectionHelper _reflection;

        internal static void Init(IReflectionHelper reflection)
        {
            _reflection = reflection;
        }

        public static InventoryMenu.highlightThisItem HighlightMethod(this Chest chest, Storage storage)
        {
            return item => !ReferenceEquals(item, chest) && storage.HighlightMethod(item);
        }

        public static Object ToObject(this Chest chest, Storage storage = null)
        {
            // Get config for chest
            if (storage == null && !ExpandedStorage.TryGetStorage(chest, out storage))
            {
                throw new InvalidOperationException($"Unexpected item '{chest.Name}'.");
            }

            // Create Chest from Item
            var obj = new Object(Vector2.Zero, chest.ParentSheetIndex)
            {
                name = chest.Name
            };

            // Copy modData from original item
            foreach (var modData in chest.modData)
                obj.modData.CopyFrom(modData);

            // Copy modData from config
            foreach (var modData in storage.ModData)
            {
                if (!obj.modData.ContainsKey(modData.Key))
                    obj.modData.Add(modData.Key, modData.Value);
            }

            return obj;
        }

        public static void Draw(this Chest chest, Storage storage, SpriteBatch spriteBatch, Vector2 pos, Vector2 origin, float alpha = 1f, float layerDepth = 0.89f, float scaleSize = 4f)
        {
            var drawColored = storage.PlayerColor
                              && !chest.playerChoiceColor.Value.Equals(Color.Black)
                              && !HideColorPickerIds.Contains(chest.ParentSheetIndex);

            if (storage.SpriteSheet is {Texture: { } texture} spriteSheet)
            {
                var currentFrame = 0;
                if (Enum.TryParse(storage.Animation, out Storage.AnimationType animationType) && animationType != Storage.AnimationType.None)
                {
                    if (animationType == Storage.AnimationType.Color)
                    {
                        if (storage.PlayerColor)
                        {
                            chest.playerChoiceColor.Value = Storage.ColorWheel.ToRgbColor();
                        }
                        else
                        {
                            chest.Tint = Storage.ColorWheel.ToRgbColor();
                        }
                    }

                    currentFrame = (int) (Storage.Frame / storage.Delay) % storage.Frames;
                }
                else if (chest.uses.Value > 0)
                {
                    if (storage.OpenNearby)
                    {
                        var farmerNearby = _reflection.GetField<bool>(chest, "_farmerNearby").GetValue();
                        var currentLidFrameReflected = _reflection.GetField<int>(chest, "_shippingBinFrameCounter");
                        var currentLidFrame = currentLidFrameReflected.GetValue();
                        currentFrame = currentLidFrame + (farmerNearby ? 1 : -1) * (int) (Storage.Frame - chest.uses.Value) / storage.Delay;
                        currentFrame = (int) MathHelper.Clamp(currentFrame, 0, storage.Frames - 1);
                        if (!farmerNearby && currentFrame == 0 || farmerNearby && currentFrame == storage.Frames - 1)
                        {
                            currentLidFrameReflected.SetValue(currentFrame);
                        }
                    }
                    else
                    {
                        currentFrame = Math.Max(0, (int) ((Storage.Frame - chest.uses.Value) / storage.Delay) % storage.Frames);
                        if (currentFrame == storage.Frames - 1)
                        {
                            chest.uses.Value = 0;
                            chest.frameCounter.Value = 5;
                            _reflection.GetField<int>(chest, "currentLidFrame").SetValue(chest.getLastLidFrame());
                        }
                    }
                }
                else if (chest.frameCounter.Value > -1)
                {
                    currentFrame = storage.Frames - 1;
                }

                var startLayer = drawColored && storage.PlayerColor ? 1 : 0;
                var endLayer = startLayer == 0 ? 1 : 3;
                for (var layer = startLayer; layer < endLayer; layer++)
                {
                    var color = layer % 2 == 0 || !drawColored
                        ? chest.Tint
                        : chest.playerChoiceColor.Value;
                    spriteBatch.Draw(texture,
                        pos + ShakeOffset(chest, -1, 2),
                        new Rectangle(spriteSheet.Width * currentFrame, spriteSheet.Height * layer, spriteSheet.Width, spriteSheet.Height),
                        color * alpha,
                        0f,
                        origin,
                        scaleSize,
                        SpriteEffects.None,
                        layerDepth + (1 + layer - startLayer) * 1E-05f);
                }

                return;
            }

            if (!drawColored) DrawVanillaDefault(chest, storage, spriteBatch, pos, origin, alpha, layerDepth, scaleSize);
            else DrawVanillaColored(chest, storage, spriteBatch, pos, origin, alpha, layerDepth, scaleSize);
        }

        private static void DrawVanillaDefault(Chest chest, Storage storage, SpriteBatch spriteBatch, Vector2 pos, Vector2 origin, float alpha, float layerDepth, float scaleSize)
        {
            var currentFrame = 0;
            if (storage.OpenNearby)
            {
                var farmerNearby = _reflection.GetField<bool>(chest, "_farmerNearby").GetValue();
                var currentLidFrame = _reflection.GetField<int>(chest, "_shippingBinFrameCounter").GetValue();
                currentFrame = currentLidFrame + (farmerNearby ? 1 : -1) * (int) (Storage.Frame - chest.uses.Value) / storage.Delay;
                currentFrame = (int) MathHelper.Clamp(currentFrame, 0, storage.Frames - 1);
            }
            else if (chest.uses.Value > 0)
            {
                currentFrame = Math.Max(0, (int) ((Storage.Frame - chest.uses.Value) / storage.Delay) % storage.Frames);
                if (currentFrame == storage.Frames - 1)
                {
                    chest.uses.Value = 0;
                    chest.frameCounter.Value = 5;
                    _reflection.GetField<int>(chest, "currentLidFrame").SetValue(chest.getLastLidFrame());
                }
            }
            else if (chest.frameCounter.Value > -1)
            {
                currentFrame = storage.Frames - 1;
            }

            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, chest.ParentSheetIndex, 16, 32),
                chest.Tint * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth);

            if (chest.uses.Value < 0 || storage.Frames == 1 || scaleSize < 4f) return;
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentFrame + chest.startingLidFrame.Value, 16, 32),
                chest.Tint * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 1E-05f);
        }

        private static int GetBaseOffset(Item item) => item.ParentSheetIndex switch {130 => 38, 232 => 0, _ => 6};
        private static int GetAboveOffset(Item item) => item.ParentSheetIndex switch {130 => 46, 232 => 8, _ => 11};

        private static void DrawVanillaColored(this Chest chest, Storage storage, SpriteBatch spriteBatch, Vector2 pos, Vector2 origin, float alpha, float layerDepth, float scaleSize)
        {
            var baseOffset = GetBaseOffset(chest);
            var aboveOffset = GetAboveOffset(chest);
            var currentFrame = 0;
            if (storage.OpenNearby)
            {
                var farmerNearby = _reflection.GetField<bool>(chest, "_farmerNearby").GetValue();
                var currentLidFrame = _reflection.GetField<int>(chest, "_shippingBinFrameCounter").GetValue();
                currentFrame = currentLidFrame + (farmerNearby ? 1 : -1) * (int) (Storage.Frame - chest.uses.Value) / storage.Delay;
                currentFrame = (int) MathHelper.Clamp(currentFrame, 0, storage.Frames - 1);
            }
            else if (chest.uses.Value > 0)
            {
                currentFrame = Math.Max(0, (int) ((Storage.Frame - chest.uses.Value) / storage.Delay) % storage.Frames);
                if (currentFrame == storage.Frames - 1)
                {
                    chest.uses.Value = 0;
                    chest.frameCounter.Value = 5;
                    _reflection.GetField<int>(chest, "currentLidFrame").SetValue(chest.getLastLidFrame());
                }
            }
            else if (chest.frameCounter.Value > -1)
            {
                currentFrame = storage.Frames - 1;
            }

            // Draw Storage Layer (Colorized)
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, chest.ParentSheetIndex + baseOffset, 16, 32),
                chest.playerChoiceColor.Value * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth);

            // Draw Lid Layer (Colorized)
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentFrame + baseOffset + chest.startingLidFrame.Value, 16, 32),
                chest.playerChoiceColor.Value * alpha * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 1E-05f);

            // Draw Brace Layer (Non-Colorized)
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentFrame + aboveOffset + chest.startingLidFrame.Value, 16, 32),
                Color.White * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 2E-05f);

            if (!ShowBottomBraceIds.Contains(chest.ParentSheetIndex)) return;

            // Draw Bottom Brace Layer (Non-Colorized)
            var rect = Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, chest.ParentSheetIndex + aboveOffset, 16, 32);
            rect.Y += 20;
            rect.Height -= 20;
            pos.Y += 20 * scaleSize;
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos,
                rect,
                Color.White * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 3E-05f);
        }

        private static Vector2 ShakeOffset(Object instance, int minValue, int maxValue)
        {
            return instance.shakeTimer > 0
                ? new Vector2(Game1.random.Next(minValue, maxValue), 0)
                : Vector2.Zero;
        }
    }
}