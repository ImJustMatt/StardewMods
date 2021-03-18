using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class InventoryMenuPatch : MenuPatch
    {
        internal InventoryMenuPatch(IMonitor monitor, ConfigController config) : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(InventoryMenu), nameof(InventoryMenu.draw), new[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)}),
                transpiler: new HarmonyMethod(GetType(), nameof(DrawTranspiler))
            );
        }

        private static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var patternPatches = new PatternPatches(instructions, Monitor);

            patternPatches
                .Find(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(InventoryMenu), nameof(InventoryMenu.actualInventory)))
                )
                .Log("Replace actualInventory with Filtered Inventory")
                .Patch(delegate(LinkedList<CodeInstruction> list) { list.AddLast(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MenuModel), nameof(MenuModel.GetItems)))); })
                .Repeat(-1);

            foreach (var patternPatch in patternPatches)
                yield return patternPatch;

            if (!patternPatches.Done)
                Monitor.Log($"Failed to apply all patches in {nameof(DrawTranspiler)}", LogLevel.Warn);
        }
    }
}