using Harmony;
using StardewModdingAPI;

namespace ImJustMatt.Common.PatternPatches
{
    internal abstract class BasePatch<T> where T : IMod
    {
        private protected static IMonitor Monitor;
        private protected static T Mod;

        internal BasePatch(IMod mod)
        {
            Mod = (T) mod;
            Monitor = mod.Monitor;
        }

        protected internal abstract void Apply(HarmonyInstance harmony);
    }
}