using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class ChestPatches : BasePatch<ExpandedStorage>
    {
        private static readonly HashSet<string> ExcludeModDataKeys = new();

        public ChestPatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.addItem), new[] {typeof(Item)}),
                new HarmonyMethod(GetType(), nameof(AddItemPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.addItem)),
                transpiler: new HarmonyMethod(GetType(), nameof(AddItemTranspiler))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.checkForAction)),
                new HarmonyMethod(GetType(), nameof(CheckForActionPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(GetType(), nameof(GetActualCapacityPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.GetItemsForPlayer)),
                postfix: new HarmonyMethod(GetType(), nameof(GetItemsForPlayerPostfix))
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
                AccessTools.Method(typeof(Chest), nameof(Chest.performToolAction)),
                new HarmonyMethod(GetType(), nameof(PerformToolActionPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.updateWhenCurrentLocation)),
                new HarmonyMethod(GetType(), nameof(UpdateWhenCurrentLocationPrefix))
            );
        }

        internal static void AddExclusion(string modDataKey)
        {
            ExcludeModDataKeys.Add(modDataKey);
        }

        /// <summary>Prevent adding item if filtered.</summary>
        private static bool AddItemPrefix(Chest __instance, ref Item __result, Item item)
        {
            if (!ReferenceEquals(__instance, item) && (!Mod.AssetController.TryGetStorage(__instance, out var storage) || storage.Filter(item))) return true;
            __result = item;
            return false;
        }

        /// <summary>GetItemsForPlayer for all storages.</summary>
        private static IEnumerable<CodeInstruction> AddItemTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var patternPatches = new PatternPatches(instructions, Monitor);

            patternPatches
                .Find(
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Chest), nameof(Chest.items)))
                )
                .Log("Use GetItemsForPlayer for all storages.")
                .Patch(delegate(LinkedList<CodeInstruction> list)
                {
                    list.RemoveLast();
                    list.AddLast(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Game1), nameof(Game1.player)).GetGetMethod()));
                    list.AddLast(new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(Farmer), nameof(Farmer.UniqueMultiplayerID)).GetGetMethod()));
                    list.AddLast(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Chest), nameof(Chest.GetItemsForPlayer))));
                });

            foreach (var patternPatch in patternPatches)
                yield return patternPatch;

            if (!patternPatches.Done)
                Monitor.Log($"Failed to apply all patches in {nameof(AddItemTranspiler)}", LogLevel.Warn);
        }

        /// <summary>Play custom sound when opening chest</summary>
        private static bool CheckForActionPrefix(Chest __instance, ref bool __result, Farmer who, bool justCheckingForActivity)
        {
            if (justCheckingForActivity
                || !__instance.playerChest.Value
                || !Game1.didPlayerJustRightClick(true)
                || !Mod.AssetController.TryGetStorage(__instance, out var storage))
                return true;

            __result = __instance.CheckForAction(storage, who);
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation when placed.</summary>
        private static bool DrawPrefix(Chest __instance, ref int ____shippingBinFrameCounter, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            if (!Mod.AssetController.TryGetStorage(__instance, out var storage) || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;
            // Only draw origin sprite for bigger expanded storages
            if (storage.StorageSprite is { } spriteSheet
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
            __instance.Draw(
                ____shippingBinFrameCounter,
                storage,
                spriteBatch,
                Game1.GlobalToLocal(Game1.viewport, globalPosition * 64),
                Vector2.Zero,
                alpha,
                layerDepth
            );
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation when held.</summary>
        private static bool DrawLocalPrefix(Chest __instance, ref int ____shippingBinFrameCounter, SpriteBatch spriteBatch, int x, int y, float alpha, bool local)
        {
            if (!Mod.AssetController.TryGetStorage(__instance, out var storage) || !local || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;
            __instance.Draw(
                ____shippingBinFrameCounter,
                storage,
                spriteBatch,
                new Vector2(x, y - 64),
                Vector2.Zero,
                alpha
            );
            return false;
        }

        /// <summary>Draw chest with playerChoiceColor and lid animation in menu.</summary>
        private static bool DrawInMenuPrefix(Chest __instance, ref int ____shippingBinFrameCounter, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (!Mod.AssetController.TryGetStorage(__instance, out var storage) || __instance.modData.Keys.Any(ExcludeModDataKeys.Contains))
                return true;

            Vector2 origin;
            var drawScaleSize = scaleSize;
            if (storage.StorageSprite is {Texture: { }} spriteSheet)
            {
                drawScaleSize *= spriteSheet.ScaleSize;
                origin = new Vector2(spriteSheet.Width / 2f, spriteSheet.Height / 2f);
            }
            else
            {
                drawScaleSize *= scaleSize < 0.2 ? 4f : 2f;
                origin = new Vector2(8, 16);
            }

            __instance.Draw(
                ____shippingBinFrameCounter,
                storage,
                spriteBatch,
                location + new Vector2(32, 32),
                origin,
                transparency,
                layerDepth,
                drawScaleSize
            );

            // Draw Stack
            if (__instance.Stack > 1)
                Utility.drawTinyDigits(__instance.Stack, spriteBatch, location + new Vector2(64 - Utility.getWidthOfTinyDigitString(__instance.Stack, 3f * scaleSize) - 3f * scaleSize, 64f - 18f * scaleSize + 2f), 3f * scaleSize, 1f, color);

            // Draw Held Items
            var items = __instance.GetItemsForPlayer(Game1.player.UniqueMultiplayerID).Count;
            if (items > 0)
                Utility.drawTinyDigits(items, spriteBatch, location + new Vector2(64 - Utility.getWidthOfTinyDigitString(items, 3f * scaleSize) - 3f * scaleSize, 2f * scaleSize), 3f * scaleSize, 1f, color);
            return false;
        }

        /// <summary>Returns modded capacity for storage.</summary>
        private static void GetActualCapacityPostfix(Chest __instance, ref int __result)
        {
            if (Mod.AssetController.TryGetStorage(__instance, out var storage) && storage.Config.ActualCapacity != 0)
            {
                __result = storage.Config.ActualCapacity;
            }
        }

        /// <summary>Get heldItem Chest items.</summary>
        private static void GetItemsForPlayerPostfix(Chest __instance, ref NetObjectList<Item> __result, long id)
        {
            if (Mod.AssetController.TryGetStorage(__instance, out var storage) && storage.HeldStorage && __instance.heldObject.Value is Chest chest)
            {
                __result = chest.GetItemsForPlayer(id);
            }
        }

        /// <summary>Refresh inventory after item grabbed from chest.</summary>
        private static void GrabItemFromChestPostfix()
        {
            Mod.ActiveMenu.Value?.RefreshItems();
        }

        /// <summary>Refresh inventory after item grabbed from inventory.</summary>
        private static void GrabItemFromInventoryPostfix()
        {
            Mod.ActiveMenu.Value?.RefreshItems();
        }

        /// <summary>Prevent breaking indestructible chests</summary>
        private static bool PerformToolActionPrefix(Chest __instance, ref bool __result)
        {
            if (!Mod.AssetController.TryGetStorage(__instance, out var storage) || storage.Config.Option("Indestructible", true) != StorageConfigController.Choice.Enable)
                return true;
            __result = false;
            return false;
        }

        private static bool UpdateWhenCurrentLocationPrefix(Chest __instance, ref int ___health, ref int ____shippingBinFrameCounter, ref bool ____farmerNearby, ref int ___currentLidFrame, GameTime time, GameLocation environment)
        {
            if (!Mod.AssetController.TryGetStorage(__instance, out var storage)) return true;

            if (__instance.synchronized.Value)
            {
                __instance.openChestEvent.Poll();
            }

            if (__instance.localKickStartTile.HasValue)
            {
                if (ReferenceEquals(Game1.currentLocation, environment))
                {
                    if (__instance.kickProgress == 0f)
                    {
                        if (Utility.isOnScreen((__instance.localKickStartTile.Value + new Vector2(0.5f, 0.5f)) * 64f, 64))
                        {
                            environment.localSound("clubhit");
                        }

                        __instance.shakeTimer = 100;
                    }
                }
                else
                {
                    __instance.localKickStartTile = null;
                    __instance.kickProgress = -1f;
                }

                if (__instance.kickProgress >= 0f)
                {
                    __instance.kickProgress += (float) (time.ElapsedGameTime.TotalSeconds / 0.25f);
                    if (__instance.kickProgress >= 1f)
                    {
                        __instance.kickProgress = -1f;
                        __instance.localKickStartTile = null;
                    }
                }
            }
            else
            {
                __instance.kickProgress = -1f;
            }

            __instance.fixLidFrame();
            __instance.mutex.Update(environment);
            if (__instance.shakeTimer > 0)
            {
                __instance.shakeTimer -= time.ElapsedGameTime.Milliseconds;
                if (__instance.shakeTimer <= 0)
                {
                    ___health = 10;
                }
            }

            var currentFrame = 0;
            if (Enum.TryParse(storage.Animation, out StorageController.AnimationType animationType) && animationType != StorageController.AnimationType.None)
            {
                currentFrame = (int) (StorageController.Frame / storage.Delay) % storage.Frames;
                ____shippingBinFrameCounter = currentFrame;
            }
            else if (storage.OpenNearby > 0)
            {
                var farmerNearby = __instance.UpdateFarmerNearby(ref ____shippingBinFrameCounter, ref ____farmerNearby, storage, environment);
                if (StorageController.Frame > 0 && __instance.frameCounter.Value > -1)
                {
                    currentFrame = ____shippingBinFrameCounter + (farmerNearby ? 1 : -1) * (int) Math.Abs(StorageController.Frame - __instance.uses.Value) / storage.Delay;
                    currentFrame = (int) MathHelper.Clamp(currentFrame, 0, storage.Frames - 1);
                }

                ____shippingBinFrameCounter = currentFrame;
            }
            else if (__instance.uses.Value > 0)
            {
                currentFrame = Math.Max(0, (int) ((StorageController.Frame - __instance.uses.Value) / storage.Delay) % storage.Frames);
                ____shippingBinFrameCounter = currentFrame;
                if (currentFrame != storage.Frames - 1) return false;
                __instance.uses.Value = 0;
                __instance.frameCounter.Value = storage.Delay;
                ___currentLidFrame = __instance.getLastLidFrame();
            }
            else if (__instance.frameCounter.Value > -1)
            {
                __instance.frameCounter.Value--;
                if (__instance.frameCounter.Value >= 0 || !__instance.GetMutex().IsLockHeld() && __instance.owner.Value != Game1.player.UniqueMultiplayerID) return false;
                __instance.ShowMenu();
                ____shippingBinFrameCounter = 0;
            }
            else if (___currentLidFrame > __instance.startingLidFrame.Value && Game1.activeClickableMenu == null && __instance.GetMutex().IsLockHeld())
            {
                ____shippingBinFrameCounter = 0;
                __instance.uses.Value = 0;
                __instance.GetMutex().ReleaseLock();
            }

            return false;
        }
    }
}