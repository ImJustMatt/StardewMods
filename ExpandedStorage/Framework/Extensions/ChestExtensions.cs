using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

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

        public static void Draw(this Chest chest, Storage storage, SpriteBatch spriteBatch, Vector2 pos, Vector2 origin, float alpha = 1f, float layerDepth = 0.89f, float scaleSize = 4f)
        {
            var currentLidFrameReflected = _reflection.GetField<int>(chest, "currentLidFrame");
            var currentLidFrame = currentLidFrameReflected.GetValue();
            if (currentLidFrame == 0 || currentLidFrame - chest.startingLidFrame.Value >= storage.Frames)
                currentLidFrame = chest.startingLidFrame.Value;

            var drawColored = storage.PlayerColor
                              && !chest.playerChoiceColor.Value.Equals(Color.Black)
                              && !HideColorPickerIds.Contains(chest.ParentSheetIndex);

            if (storage.SpriteSheet is {Texture: { } texture} spriteSheet)
            {
                currentLidFrame -= chest.startingLidFrame.Value;
                var startLayer = drawColored && storage.PlayerColor ? 1 : 0;
                var endLayer = startLayer == 0 ? 1 : 3;

                for (var layer = startLayer; layer < endLayer; layer++)
                {
                    var color = layer % 2 == 0 || !drawColored
                        ? chest.Tint
                        : chest.playerChoiceColor.Value;
                    spriteBatch.Draw(texture,
                        pos + ShakeOffset(chest, -1, 2),
                        new Rectangle(spriteSheet.Width * currentLidFrame, spriteSheet.Height * layer, spriteSheet.Width, spriteSheet.Height),
                        color * alpha,
                        0f,
                        origin,
                        scaleSize,
                        SpriteEffects.None,
                        layerDepth + (1 + layer - startLayer) * 1E-05f);
                }

                return;
            }

            if (!drawColored)
            {
                spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                    pos + ShakeOffset(chest, -1, 2),
                    Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, chest.ParentSheetIndex, 16, 32),
                    chest.Tint * alpha,
                    0f,
                    origin,
                    scaleSize,
                    SpriteEffects.None,
                    layerDepth);

                if (storage.Frames == 1 || scaleSize < 4f)
                    return;

                spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                    pos + ShakeOffset(chest, -1, 2),
                    Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentLidFrame, 16, 32),
                    chest.Tint * alpha,
                    0f,
                    origin,
                    scaleSize,
                    SpriteEffects.None,
                    layerDepth + 1E-05f);

                return;
            }

            var baseOffset = chest.ParentSheetIndex switch {130 => 38, 232 => 0, _ => 6};
            var aboveOffset = chest.ParentSheetIndex switch {130 => 46, 232 => 8, _ => 11};

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
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentLidFrame + baseOffset, 16, 32),
                chest.playerChoiceColor.Value * alpha * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 1E-05f);

            // Draw Brace Layer (Non-Colorized)
            spriteBatch.Draw(Game1.bigCraftableSpriteSheet,
                pos + ShakeOffset(chest, -1, 2),
                Game1.getSourceRectForStandardTileSheet(Game1.bigCraftableSpriteSheet, currentLidFrame + aboveOffset, 16, 32),
                Color.White * alpha,
                0f,
                origin,
                scaleSize,
                SpriteEffects.None,
                layerDepth + 2E-05f);

            if (!ShowBottomBraceIds.Contains(chest.ParentSheetIndex))
                return;

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