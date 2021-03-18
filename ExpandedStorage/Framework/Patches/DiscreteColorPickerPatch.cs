using Harmony;
using ImJustMatt.Common.PatternPatches;
using ImJustMatt.ExpandedStorage.Framework.Controllers;
using ImJustMatt.ExpandedStorage.Framework.Views;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

// ReSharper disable InconsistentNaming

namespace ImJustMatt.ExpandedStorage.Framework.Patches
{
    internal class DiscreteColorPickerPatch : Patch<ConfigController>
    {
        public DiscreteColorPickerPatch(IMonitor monitor, ConfigController config)
            : base(monitor, config)
        {
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Constructor(typeof(DiscreteColorPicker), new[] {typeof(int), typeof(int), typeof(int), typeof(Item)}),
                postfix: new HarmonyMethod(GetType(), nameof(ConstructorPostfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getCurrentColor)),
                new HarmonyMethod(GetType(), nameof(GetCurrentColorPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getSelectionFromColor)),
                new HarmonyMethod(GetType(), nameof(GetSelectionFromColorPrefix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(DiscreteColorPicker), nameof(DiscreteColorPicker.getColorFromSelection)),
                new HarmonyMethod(GetType(), nameof(GetColorFromSelectionPrefix))
            );
        }

        private static void ConstructorPostfix(DiscreteColorPicker __instance)
        {
            __instance.visible = false;
        }

        private static bool GetCurrentColorPrefix(DiscreteColorPicker __instance, ref Color __result)
        {
            if (__instance is not HSLColorPicker colorPicker)
                return true;
            __result = colorPicker.getCurrentColor();
            return false;
        }

        private static bool GetSelectionFromColorPrefix(DiscreteColorPicker __instance, ref int __result, Color c)
        {
            if (__instance is not HSLColorPicker colorPicker)
                return true;
            __result = colorPicker.getSelectionFromColor(c);
            return false;
        }

        private static bool GetColorFromSelectionPrefix(DiscreteColorPicker __instance, ref Color __result, int selection)
        {
            if (__instance is not HSLColorPicker colorPicker)
                return true;
            __result = colorPicker.getColorFromSelection(selection);
            return false;
        }
    }
}