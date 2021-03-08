using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using ImJustMatt.ExpandedStorage.Framework.Models;
using ImJustMatt.ExpandedStorage.Framework.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

// ReSharper disable UnusedParameter.Global

// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class ChestPatch : Patch<ModConfig>
    {
        private static readonly HashSet<string> ExcludeModDataKeys = new();

        internal ChestPatch(IMonitor monitor, ModConfig config) : base(monitor, config)
        {
        }

        internal static void AddExclusion(string modDataKey)
        {
            ExcludeModDataKeys.Add(modDataKey);
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.checkForAction)),
                new HarmonyMethod(GetType(), nameof(CheckForActionPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.performToolAction)),
                new HarmonyMethod(GetType(), nameof(PerformToolActionPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.grabItemFromChest)),
                postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromChestPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.grabItemFromInventory)),
                postfix: new HarmonyMethod(GetType(), nameof(GrabItemFromInventoryPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.addItem), new[] {typeof(Item)}),
                new HarmonyMethod(GetType(), nameof(AddItemPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.GetActualCapacity)),
                new HarmonyMethod(GetType(), nameof(GetActualCapacityPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.draw), new[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
                new HarmonyMethod(GetType(), nameof(DrawPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.draw), new[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(bool)}),
                new HarmonyMethod(GetType(), nameof(DrawLocalPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.drawInMenu), new[] {typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)}),
                new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.updateWhenCurrentLocation)),
                new HarmonyMethod(GetType(), nameof(UpdateWhenCurrentLocationPrefix))
            );
        }

        /// <summary>Play custom sound when opening chest</summary>
        public static bool CheckForActionPrefix(Chest __instance, ref bool __result, Farmer who, bool justCheckingForActivity)
        {
            if (justCheckingForActivity
                || !__instance.playerChest.Value
                || !Game1.didPlayerJustRightClick(true))
                return true;
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage))
                return true;

            __result = true;
            if (storage.SpecialChestType == "MiniShippingBin"
                || Enum.TryParse(storage.Animation, out Storage.AnimationType animationType)
                && animationType != Storage.AnimationType.None)
            {
                Game1.playSound(storage.OpenSound);
                __instance.ShowMenu();
            }
            else
            {
                __instance.GetMutex().RequestLock(delegate
                {
                    __instance.frameCounter.Value = 5;
                    Game1.playSound(storage.OpenSound);
                    Game1.player.Halt();
                    Game1.player.freezePause = 1000;
                });
            }
            return false;
        }

        /// <summary>Prevent breaking indestructible chests</summary>
        private static bool PerformToolActionPrefix(Chest __instance, ref bool __result, Tool t, GameLocation location)
        {
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage) || storage.Option("Indestructible", true) != StorageConfig.Choice.Enable)
                return true;
            __result = false;
            return false;
        }

        /// <summary>Prevent adding item if filtered.</summary>
        public static bool AddItemPrefix(Chest __instance, ref Item __result, Item item)
        {
            if (!ReferenceEquals(__instance, item)
                && (!ExpandedStorage.TryGetStorage(__instance, out var storage) || storage.Filter(item)))
                return true;
            __result = item;
            return false;
        }

        /// <summary>Refresh inventory after item grabbed from chest.</summary>
        public static void GrabItemFromChestPostfix()
        {
            MenuViewModel.RefreshItems();
        }

        /// <summary>Refresh inventory after item grabbed from inventory.</summary>
        public static void GrabItemFromInventoryPostfix()
        {
            MenuViewModel.RefreshItems();
        }

        /// <summary>Returns modded capacity for storage.</summary>
        public static bool GetActualCapacityPrefix(Chest __instance, ref int __result)
        {
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage) || storage.ActualCapacity == 0)
                return true;
            __result = storage.ActualCapacity == -1 ? int.MaxValue : storage.ActualCapacity;
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation when placed.</summary>
        public static bool DrawPrefix(Chest __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage) || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;

            // Only draw origin sprite for bigger expanded storages
            if (storage.SpriteSheet is { } spriteSheet
                && (spriteSheet.TileWidth > 1 || spriteSheet.TileHeight > 1)
                && ((int) __instance.TileLocation.X != x || (int) __instance.TileLocation.Y != y))
                return false;

            var draw_x = (float) x;
            var draw_y = (float) y;
            if (__instance.localKickStartTile.HasValue)
            {
                draw_x = Utility.Lerp(__instance.localKickStartTile.Value.X, draw_x, __instance.kickProgress);
                draw_y = Utility.Lerp(__instance.localKickStartTile.Value.Y, draw_y, __instance.kickProgress);
            }

            var globalPosition = new Vector2(draw_x, (int) (draw_y - storage.Depth / 16f - 1f));
            var layerDepth = Math.Max(0.0f, ((draw_y + 1f) * 64f - 24f) / 10000f) + draw_x * 1E-05f;
            __instance.Draw(storage, spriteBatch, Game1.GlobalToLocal(Game1.viewport, globalPosition * 64), Vector2.Zero, alpha, layerDepth);
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation when held.</summary>
        public static bool DrawLocalPrefix(Chest __instance, SpriteBatch spriteBatch, int x, int y, float alpha, bool local)
        {
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage) || !local || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;
            __instance.Draw(storage, spriteBatch, new Vector2(x, y - 64), Vector2.Zero, alpha);
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation in menu.</summary>
        public static bool DrawInMenuPrefix(Chest __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (!ExpandedStorage.TryGetStorage(__instance, out var storage) || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;

            Vector2 origin;
            var drawScaleSize = scaleSize;
            if (storage.SpriteSheet is {Texture: { }} spriteSheet)
            {
                drawScaleSize *= spriteSheet.ScaleSize;
                origin = new Vector2(spriteSheet.Width / 2f, spriteSheet.Height / 2f);
            }
            else
            {
                drawScaleSize *= scaleSize < 0.2 ? 4f : 2f;
                origin = new Vector2(8, 16);
            }

            __instance.Draw(storage,
                spriteBatch,
                location + new Vector2(32, 32),
                origin,
                transparency,
                layerDepth,
                drawScaleSize);

            // Draw Stack
            if (__instance.Stack > 1)
                Utility.drawTinyDigits(__instance.Stack, spriteBatch, location + new Vector2(64 - Utility.getWidthOfTinyDigitString(__instance.Stack, 3f * scaleSize) - 3f * scaleSize, 64f - 18f * scaleSize + 2f), 3f * scaleSize, 1f, color);

            // Draw Held Items
            var items = __instance.GetItemsForPlayer(Game1.player.UniqueMultiplayerID).Count;
            if (items > 0)
                Utility.drawTinyDigits(items, spriteBatch, location + new Vector2(64 - Utility.getWidthOfTinyDigitString(items, 3f * scaleSize) - 3f * scaleSize, 2f * scaleSize), 3f * scaleSize, 1f, color);
            return false;
        }

        public static bool UpdateWhenCurrentLocationPrefix(Chest __instance, GameTime time, GameLocation environment)
        {
            return !ExpandedStorage.TryGetStorage(__instance, out var storage)
                   || !Enum.TryParse(storage.Animation, out Storage.AnimationType animationType)
                   || animationType == Storage.AnimationType.None;
        }
    }
}