using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.Framework.Views;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class DiscreteColorPickerPatch : BasePatch<ExpandedStorage>
    {
        public DiscreteColorPickerPatch(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            harmony.Patch(
                AccessTools.Constructor(typeof(DiscreteColorPicker), new[] {typeof(int), typeof(int), typeof(int), typeof(Item)}),
                postfix: new HarmonyMethod(GetType(), nameof(ConstructorPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getCurrentColor)),
                postfix: new HarmonyMethod(GetType(), nameof(GetCurrentColorPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getSelectionFromColor)),
                postfix: new HarmonyMethod(GetType(), nameof(GetSelectionFromColorPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getColorFromSelection)),
                postfix: new HarmonyMethod(GetType(), nameof(GetColorFromSelectionPostfix))
            );
        }

        private static void ConstructorPostfix(DiscreteColorPicker __instance)
        {
            __instance.visible = false;
        }

        private static void GetCurrentColorPostfix(DiscreteColorPicker __instance, ref Color __result)
        {
            if (__instance is HSLColorPicker colorPicker)
            {
                __result = colorPicker.getCurrentColor();
            }
        }

        private static void GetSelectionFromColorPostfix(DiscreteColorPicker __instance, ref int __result, Color c)
        {
            if (__instance is HSLColorPicker colorPicker)
            {
                __result = colorPicker.getSelectionFromColor(c);
            }
        }

        private static void GetColorFromSelectionPostfix(DiscreteColorPicker __instance, ref Color __result, int selection)
        {
            if (__instance is HSLColorPicker colorPicker)
            {
                __result = colorPicker.getColorFromSelection(selection);
            }
        }
    }
}