using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewValley.Menus;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal abstract class MenuPatch : Patch<ModConfig>
    {
        private protected static readonly MethodInfo MenuCapacity =
            AccessTools.Method(typeof(MenuModel), nameof(MenuModel.GetMenuCapacity), new[] {typeof(object)});

        private protected static readonly MethodInfo MenuRows =
            AccessTools.Method(typeof(MenuModel), nameof(MenuModel.GetMenuRows), new[] {typeof(object)});

        private protected static readonly MethodInfo MenuOffset =
            AccessTools.Method(typeof(MenuModel), nameof(MenuModel.GetOffset), new[] {typeof(MenuWithInventory)});

        private protected static readonly MethodInfo MenuPadding =
            AccessTools.Method(typeof(MenuModel), nameof(MenuModel.GetPadding), new[] {typeof(MenuWithInventory)});

        private protected static readonly FieldInfo IClickableMenuHeight =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.height));

        private protected static readonly FieldInfo IClickableMenuBorderWidth =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.borderWidth));

        private protected static readonly FieldInfo IClickableMenuSpaceToClearTopBorder =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.spaceToClearTopBorder));

        private protected static readonly FieldInfo IClickableMenuYPositionOnScreen =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.yPositionOnScreen));

        internal MenuPatch(IMonitor monitor, ModConfig config) : base(monitor, config)
        {
        }

        protected internal abstract override void Apply(HarmonyInstance harmony);

        /// <summary>Adds a value to the end of the stack</summary>
        /// <param name="method">Method of the offset function</param>
        /// <param name="operation">Whether to add or subtract the value.</param>
        private protected static Action<LinkedList<CodeInstruction>> OffsetPatch(MethodInfo method, OpCode operation)
        {
            return instructions =>
            {
                instructions.AddLast(new CodeInstruction(OpCodes.Ldarg_0));
                instructions.AddLast(new CodeInstruction(OpCodes.Call, method));
                instructions.AddLast(new CodeInstruction(operation));
            };
        }
    }
}