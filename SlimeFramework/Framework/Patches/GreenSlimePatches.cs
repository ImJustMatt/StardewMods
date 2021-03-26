using System.Diagnostics.CodeAnalysis;
using Harmony;
using ImJustMatt.Common.Patches;
using ImJustMatt.SlimeFramework.Framework.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace ImJustMatt.SlimeFramework.Framework.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class GreenSlimePatches : BasePatch<SlimeFramework>
    {
        public GreenSlimePatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
            harmony.Patch(
                AccessTools.Constructor(typeof(GreenSlime), new []{typeof(Vector2)}),
                postfix: new HarmonyMethod(GetType(), nameof(ConstructorPostfix))
            );

            harmony.Patch(
                AccessTools.Constructor(typeof(GreenSlime), new []{typeof(Vector2), typeof(Color)}),
                postfix: new HarmonyMethod(GetType(), nameof(ConstructorPostfixColor))
            );

            harmony.Patch(
                AccessTools.Constructor(typeof(GreenSlime), new []{typeof(Vector2), typeof(int)}),
                postfix: new HarmonyMethod(GetType(), nameof(ConstructorPostfixMine))
            );
        }

        private static void ConstructorPostfix(GreenSlime __instance)
        {
            if (!SlimeFramework.TryGetSlime(out var slime)) return;
            __instance.MakeCustomSlime(slime);
        }

        private static void ConstructorPostfixColor(GreenSlime __instance)
        {
            if (!SlimeFramework.TryGetSlime(out var slime)) return;
            __instance.MakeCustomSlime(slime);
        }

        private static void ConstructorPostfixMine(GreenSlime __instance)
        {
            if (!SlimeFramework.TryGetSlime(out var slime)) return;
            __instance.MakeCustomSlime(slime);
        }
    }
}