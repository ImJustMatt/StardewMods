using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Models;
using StardewModdingAPI;
using StardewValley.Menus;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal abstract class MenuPatches : BasePatch<ExpandedStorage>
    {
        private protected static readonly MethodInfo MenuCapacity =
            AccessTools.Method(typeof(MenuPatches), nameof(GetMenuCapacity));

        private protected static readonly MethodInfo MenuRows =
            AccessTools.Method(typeof(MenuPatches), nameof(GetMenuRows));

        private protected static readonly MethodInfo MenuOffset =
            AccessTools.Method(typeof(MenuPatches), nameof(GetMenuOffset));

        private protected static readonly MethodInfo MenuPadding =
            AccessTools.Method(typeof(MenuPatches), nameof(GetMenuPadding));

        private protected static readonly FieldInfo IClickableMenuHeight =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.height));

        private protected static readonly FieldInfo IClickableMenuBorderWidth =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.borderWidth));

        private protected static readonly FieldInfo IClickableMenuSpaceToClearTopBorder =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.spaceToClearTopBorder));

        private protected static readonly FieldInfo IClickableMenuYPositionOnScreen =
            AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.yPositionOnScreen));

        protected MenuPatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
        }

        private protected static ConfigModel Config => Mod.Config;
        private protected static AssetController AssetController => Mod.AssetController;

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

        /// <summary>Returns Offset to lower menu for expanded menus.</summary>
        private protected static int GetMenuOffset(MenuWithInventory menu)
        {
            return Config.ExpandInventoryMenu
                   && menu is ItemGrabMenu {shippingBin: false} igm
                   && AssetController.TryGetStorage(igm.context, out var storage)
                ? storage.Config.Menu.Offset
                : 0;
        }

        /// <summary>Returns Padding to top menu for search box.</summary>
        private protected static int GetMenuPadding(MenuWithInventory menu)
        {
            return menu is ItemGrabMenu {shippingBin: false} igm && AssetController.TryGetStorage(igm.context, out var storage) ? storage.Config.Menu.Padding : 0;
        }

        /// <summary>Returns Display Capacity of MenuWithInventory.</summary>
        private protected static int GetMenuCapacity(object context)
        {
            return Config.ExpandInventoryMenu && AssetController.TryGetStorage(context, out var storage) ? storage.Config.Menu.Capacity : 36;
        }

        /// <summary>Returns Displayed Rows of MenuWithInventory.</summary>
        private protected static int GetMenuRows(object context)
        {
            return Config.ExpandInventoryMenu && AssetController.TryGetStorage(context, out var storage) ? storage.Config.Menu.Rows : 3;
        }
    }
}