using Harmony;
using StardewModdingAPI;

namespace ImJustMatt.Common.PatternPatches
{
    internal abstract class BasePatch
    {
        private protected static IMonitor Monitor;

        internal BasePatch(IMod mod)
        {
            Monitor = mod.Monitor;
        }

        protected internal abstract void Apply(HarmonyInstance harmony);
    }
}