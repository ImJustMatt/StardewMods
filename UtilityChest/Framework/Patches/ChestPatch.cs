﻿using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.UtilityChest.Framework.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.UtilityChest.Framework.Patches
{
    internal class ChestPatch : BasePatch
    {
        private static PerScreen<Chest> CurrentChest;

        public ChestPatch(IMod mod) : base(mod)
        {
            CurrentChest = ((UtilityChest) mod).CurrentChest;
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Chest), nameof(Chest.drawInMenu), new[] {typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)}),
                postfix: new HarmonyMethod(GetType(), nameof(DrawInMenuPostfix))
            );
        }

        public static void DrawInMenuPostfix(Chest __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (CurrentChest.Value?.CurrentItem() is not { } item) return;
            item.drawInMenu(spriteBatch, location, scaleSize * 0.8f, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }
}