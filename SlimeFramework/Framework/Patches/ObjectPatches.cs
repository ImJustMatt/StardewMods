using Harmony;
using ImJustMatt.Common.Patches;
using StardewModdingAPI;

namespace ImJustMatt.SlimeFramework.Framework.Patches
{
    internal class ObjectPatches : BasePatch<SlimeFramework>
    {
        public ObjectPatches(IMod mod, HarmonyInstance harmony) : base(mod, harmony)
        {
        }

        private static void DayUpdate()
        {
            
        }
    }
}