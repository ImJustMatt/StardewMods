using Harmony;
using ImJustMatt.Common.PatternPatches;
using StardewModdingAPI;
using StardewValley;

namespace ImJustMatt.UtilityChest.Framework.Patches
{
    internal class Game1Patch : BasePatch<UtilityChest>
    {
        private static IInputHelper InputHelper;

        public Game1Patch(IMod mod) : base(mod)
        {
            InputHelper = Mod.Helper.Input;
        }

        protected internal override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Game1), nameof(Game1.pressSwitchToolButton)),
                new HarmonyMethod(GetType(), nameof(PressSwitchToolButtonPrefix))
            );
        }

        private static bool PressSwitchToolButtonPrefix()
        {
            return !InputHelper.IsDown(SButton.LeftShift) && !InputHelper.IsDown(SButton.RightShift);
        }
    }
}