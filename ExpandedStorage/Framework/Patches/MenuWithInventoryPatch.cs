using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    internal class MenuWithInventoryPatch : MenuPatch
    {
        internal MenuWithInventoryPatch(IMonitor monitor, ConfigController config) : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            var drawMethod = AccessTools.Method(typeof(MenuWithInventory), nameof(MenuWithInventory.draw),
                new[]
                {
                    typeof(SpriteBatch),
                    typeof(bool),
                    typeof(bool),
                    typeof(int),
                    typeof(int),
                    typeof(int)
                });

            harmony.Patch(
                drawMethod,
                transpiler: new HarmonyMethod(GetType(), nameof(DrawTranspiler))
            );
        }

        static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var patternPatches = new PatternPatches(instructions, Monitor);

            var patch = patternPatches
                .Find(
                    new CodeInstruction(OpCodes.Ldfld, IClickableMenuYPositionOnScreen),
                    new CodeInstruction(OpCodes.Ldsfld, IClickableMenuBorderWidth),
                    new CodeInstruction(OpCodes.Add),
                    new CodeInstruction(OpCodes.Ldsfld, IClickableMenuSpaceToClearTopBorder),
                    new CodeInstruction(OpCodes.Add),
                    new CodeInstruction(OpCodes.Ldc_I4_S),
                    new CodeInstruction(OpCodes.Add)
                )
                .Log("Adding Offset to drawDialogueBox.y.");

            patch.Patch(OffsetPatch(MenuOffset, OpCodes.Add));
            patch.Patch(OffsetPatch(MenuPadding, OpCodes.Add));

            patch = patternPatches
                .Find(
                    new CodeInstruction(OpCodes.Ldfld, IClickableMenuHeight),
                    new CodeInstruction(OpCodes.Ldsfld, IClickableMenuBorderWidth),
                    new CodeInstruction(OpCodes.Ldsfld, IClickableMenuSpaceToClearTopBorder),
                    new CodeInstruction(OpCodes.Add),
                    new CodeInstruction(OpCodes.Ldc_I4, 192),
                    new CodeInstruction(OpCodes.Add)
                )
                .Log("Subtracting Y-Offset from drawDialogueBox.height");

            patch.Patch(OffsetPatch(MenuOffset, OpCodes.Add));
            patch.Patch(OffsetPatch(MenuPadding, OpCodes.Add));

            foreach (var patternPatch in patternPatches)
                yield return patternPatch;

            if (!patternPatches.Done)
                Monitor.Log($"Failed to apply all patches in {nameof(DrawTranspiler)}", LogLevel.Warn);
        }
    }
}